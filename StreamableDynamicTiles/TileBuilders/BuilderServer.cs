using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using LibDeltaSystem;
using LibDeltaSystem.Tools.InternalComms;
using static LibDeltaSystem.Tools.InternalComms.InternalCommsServer;
using LibDeltaSystem.Entities.DynamicTiles;
using Newtonsoft.Json;
using LibDeltaSystem.Db.System;

namespace StreamableDynamicTiles.TileBuilders
{
    public class BuilderServer : InternalCommsServerClient
    {
        /// <summary>
        /// Queue for the number of pending tile requests
        /// </summary>
        public ConcurrentQueue<DynamicTileBuilderRequest> pending;

        /// <summary>
        /// Set to true when this is processing a tile
        /// </summary>
        public bool busy;

        /// <summary>
        /// Gets the server parent
        /// </summary>
        /// <returns></returns>
        public BuilderNetworking GetServer()
        {
            return (BuilderNetworking)server;
        }

        public BuilderServer(DeltaConnection conn, byte[] key, Socket sock, InternalCommsServer server) : base(conn, key, sock, server)
        {
            busy = false;
            pending = new ConcurrentQueue<DynamicTileBuilderRequest>();
        }

        public override async Task HandleMessage(int opcode, Dictionary<string, byte[]> payloads)
        {
            if (opcode == 1)
                OnTileProcessResponse(GetJsonFromPayload<DynamicTileBuilderResponse>(payloads, "RESPONSE"));
        }

        public override void OnAuthorized()
        {
            GetServer().servers.Add(this);
        }

        public override void OnDisconnect(string reason = null)
        {
            if(GetServer().servers.Contains(this))
                GetServer().servers.Remove(this);
        }

        /// <summary>
        /// Adds a tile to the queue. Public API
        /// </summary>
        /// <param name="request"></param>
        public void ProcessTile(DynamicTileBuilderRequest request)
        {
            //If this is not busy, run now. If it is, queue it
            if (!busy)
                StartTileProcessing(request);
            else
                pending.Enqueue(request);
        }

        /// <summary>
        /// Starts processing a tile now
        /// </summary>
        private void StartTileProcessing(DynamicTileBuilderRequest request)
        {
            busy = true;
            RawSendMessage(0, new Dictionary<string, byte[]>
            {
                {"REQUEST", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request)) }
            });
        }

        private void OnTileProcessResponse(DynamicTileBuilderResponse response)
        {
            //Send events to clients
            SubscriptionManager.SendEvent(response.target, response.url);

            //Write to database
            DbDynamicTileCache cache = new DbDynamicTileCache
            {
                create_time = DateTime.UtcNow,
                revision_id = response.revision_id,
                server = response.server,
                target = response.target,
                tiles = response.count,
                url = response.url
            };
            Program.conn.system_dynamic_tile_cache.FindOneAndReplace(response.target.CreateFilter(), cache, new MongoDB.Driver.FindOneAndReplaceOptions<DbDynamicTileCache, DbDynamicTileCache>
            {
                IsUpsert = true
            });

            //If there is a tile pending, process it
            busy = false;
            if (pending.TryDequeue(out DynamicTileBuilderRequest request))
                ProcessTile(request);
        }
    }
}
