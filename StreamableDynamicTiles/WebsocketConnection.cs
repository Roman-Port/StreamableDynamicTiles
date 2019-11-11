using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StreamableDynamicTiles
{
    public abstract class WebsocketConnection
    {
        /// <summary>
        /// The socket used.
        /// </summary>
        private WebSocket sock;

        /// <summary>
        /// Task to send data on the wire
        /// </summary>
        private Task sendTask;

        /// <summary>
        /// Output queue
        /// </summary>
        private ConcurrentQueue<byte[]> queue;

        public WebsocketConnection()
        {
            sendTask = Task.CompletedTask;
            queue = new ConcurrentQueue<byte[]>();
        }

        /// <summary>
        /// Accepts an incoming connection.
        /// </summary>
        /// <param name="sock">Websocket used.</param>
        /// <returns></returns>
        public async Task Run(WebSocket sock)
        {
            this.sock = sock;

            //Flush queue
            Flush();

            //Start loops
            await ReadLoop();

            //Shut down the connection
            await Close();
        }

        /// <summary>
        /// Used when data is sent.
        /// </summary>
        /// <param name="data"></param>
        public abstract void OnMessageReceived(byte[] data, int length);

        /// <summary>
        /// Sends data to the client.
        /// </summary>
        /// <param name="data"></param>
        public void SendMessage(byte[] data)
        {
            //Add to queue
            queue.Enqueue(data);

            //Flush
            Flush();
        }

        /// <summary>
        /// Flushes new buffer messages. Should be used every time we add to the queue
        /// </summary>
        private void Flush()
        {
            lock (sendTask)
            {
                //Ignore if we're already flushing
                if (!sendTask.IsCompleted)
                    return;

                //Trigger
                sendTask = SendQueuedMessages();
            }
        }

        /// <summary>
        /// Loop for reading from the websocket
        /// </summary>
        /// <returns></returns>
        private async Task ReadLoop()
        {
            try
            {
                var buffer = new byte[Program.config.buffer_size];
                WebSocketReceiveResult result = await sock.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                while (!result.CloseStatus.HasValue)
                {
                    OnMessageReceived(buffer, result.Count);
                    result = await sock.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                //We might log this in the future.
            }
        }

        /// <summary>
        /// Sends any messages queued. Should be used every time we add to the queue
        /// </summary>
        /// <returns></returns>
        private async Task SendQueuedMessages()
        {
            //Make sure that we're still connected
            if (sock.CloseStatus.HasValue)
                return;

            //Lock queue and send all
            byte[] data;
            while (queue.TryDequeue(out data))
            {
                //Send on the network
                try
                {
                    await sock.SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch
                {
                    //Add back, trigger fault
                    queue.Enqueue(data);
                    await Close();
                    return;
                }
            }
        }

        /// <summary>
        /// Ends the connection.
        /// </summary>
        /// <returns></returns>
        public async Task Close()
        {
            //Close
            if (!sock.CloseStatus.HasValue)
            {
                //Signal that this has closed
                try
                {
                    await sock.CloseAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None);
                }
                catch { }
            }
        }
    }
}
