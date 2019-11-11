using LibDeltaSystem;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using StreamableDynamicTiles.TileBuilders;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StreamableDynamicTiles
{
    class Program
    {
        public static DeltaConnection conn;
        public static ServiceConfig config;
        public static BuilderNetworking builders;

        static void Main(string[] args)
        {
            //Load config
            config = new ServiceConfig();
            
            //Launch server
            MainAsync().GetAwaiter().GetResult();
        }

        public static async Task MainAsync()
        {
            //Connect to database
            conn = new DeltaConnection(config.database_config, "dynamic-tiles-streamable", 0, 0);
            await conn.Connect();

            //Set up builder servers
            builders = new BuilderNetworking(conn, new byte[32], config.builder_port);
            builders.StartServer();

            //Set up web server
            var host = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    IPAddress addr = IPAddress.Any;
                    options.Listen(addr, config.port);

                })
                .UseStartup<Program>()
                .Build();

            await host.RunAsync();
        }

        public void Configure(IApplicationBuilder app)
        {
            var webSocketOptions = new WebSocketOptions()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(config.timeout_seconds),
                ReceiveBufferSize = config.buffer_size
            };

            app.UseWebSockets(webSocketOptions);

            app.Run(OnHttpRequest);
        }

        public static async Task WriteStringToStreamAsync(Stream s, string content)
        {
            byte[] data = Encoding.UTF8.GetBytes(content);
            await s.WriteAsync(data);
        }

        public static async Task OnHttpRequest(Microsoft.AspNetCore.Http.HttpContext e)
        {
            try
            {
                if (e.Request.Path == "/v1")
                    await AcceptV1Request(e);
                else if (e.Request.Path == "/")
                    await AcceptRootRequest(e);
                else
                {
                    e.Response.StatusCode = 404;
                    await WriteStringToStreamAsync(e.Response.Body, "Not Found");
                }
            }
            catch (Exception ex)
            {
                //Log and display error
                var error = await conn.LogHttpError(ex, new System.Collections.Generic.Dictionary<string, string>());
                e.Response.StatusCode = 500;
                await WriteStringToStreamAsync(e.Response.Body, JsonConvert.SerializeObject(error, Newtonsoft.Json.Formatting.Indented));
            }
        }

        public static async Task AcceptV1Request(Microsoft.AspNetCore.Http.HttpContext e)
        {
            //First, authenticate this user
            DTSession session = await DTSession.AuthenticateSession(e.Request.Query["access_token"]);
            if (session == null)
            {
                //Failed to authenticate
                e.Response.StatusCode = 401;
                return;
            }

            //Accept the socket
            var socket = await e.WebSockets.AcceptWebSocketAsync();

            //Set up the client
            await session.Run(socket);
        }

        public static async Task AcceptRootRequest(Microsoft.AspNetCore.Http.HttpContext e)
        {
            await WriteStringToStreamAsync(e.Response.Body, "DeltaWebMap Dynamic Tiles Streamable\n\n(C) DeltaWebMap 2019, RomanPort 2019");
        }
    }
}
