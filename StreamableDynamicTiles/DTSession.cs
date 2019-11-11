using LibDeltaSystem.Db.System;
using LibDeltaSystem.Db.System.Entities;
using MongoDB.Bson;
using Newtonsoft.Json;
using StreamableDynamicTiles.Messages;
using StreamableDynamicTiles.Messages.Inbound;
using StreamableDynamicTiles.Messages.Outbound;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace StreamableDynamicTiles
{
    public class DTSession : WebsocketConnection
    {
        /// <summary>
        /// ID of this user
        /// </summary>
        public string user_id;

        /// <summary>
        /// Steam ID of this user
        /// </summary>
        public string steam_id;

        /// <summary>
        /// Target server ID
        /// </summary>
        public ObjectId target_server;

        /// <summary>
        /// Target map name
        /// </summary>
        public string target_map;

        /// <summary>
        /// Target tribe ID of the server
        /// </summary>
        public int target_tribe;

        /// <summary>
        /// The revision ID of the target server
        /// </summary>
        public DbServer target_server_data;

        /// <summary>
        /// The last time the target_server_data was refreshed
        /// </summary>
        public DateTime last_server_refresh;

        /// <summary>
        /// Creates a session.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task<DTSession> AuthenticateSession(string token)
        {
            //Auth user
            DbUser u = await Program.conn.AuthenticateUserToken(token);

            //Create a new session
            return new DTSession
            {
                user_id = u.id,
                steam_id = u.steam_id
            };
        }

        /// <summary>
        /// Sends a DT message to the client
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="opcode"></param>
        /// <param name="payload"></param>
        public void SendDTMessage<T>(DTCMessageOpcode opcode, T payload)
        {
            //Create the message and send it
            DTCMessage<T> msg = new DTCMessage<T>
            {
                opcode = opcode,
                payload = payload
            };

            //Send
            SendMessage(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(msg)));
        }

        /// <summary>
        /// Sends a message to the client that a tile has loaded
        /// </summary>
        /// <param name="type"></param>
        /// <param name="index"></param>
        /// <param name="url"></param>
        public void SendTileLoadMessage(DynamicTileType type, int index, string url)
        {
            SendDTMessage<DTCPayloadTileLoad>(DTCMessageOpcode.TileLoad, new DTCPayloadTileLoad
            {
                i = index,
                t = type,
                url = url
            });
        }

        /// <summary>
        /// Called when we download a message. We'll need to decode it.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="length"></param>
        public override void OnMessageReceived(byte[] data, int length)
        {
            try
            {
                //Get this as a string
                string content = Encoding.UTF8.GetString(data, 0, length);

                //First, decode as a basic message so that we can get the opcode
                DTCMessageOpcode opcode = JsonConvert.DeserializeObject<DTCMessage<DTCMessagePayload>>(content).opcode;

                //Choose outcome of this
                switch (opcode)
                {
                    case DTCMessageOpcode.SetServer: HandleSetServerMsg(GetMessagePayload<DTCPayloadSetServer>(content)); break;
                    case DTCMessageOpcode.AddTileSubscription: HandleAddTileSubscription(GetMessagePayload<DTCPayloadAddTileSubscription>(content)); break;
                    case DTCMessageOpcode.RemoveTileSubscription: HandleRemoveTileSubscription(GetMessagePayload<DTCPayloadRemoveTileSubscription>(content)); break;
                }
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
            }
        }

        /// <summary>
        /// Helper for reading messages.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="content"></param>
        /// <returns></returns>
        private T GetMessagePayload<T>(string content)
        {
            return JsonConvert.DeserializeObject<DTCMessage<T>>(content).payload;
        }

        private void HandleSetServerMsg(DTCPayloadSetServer msg)
        {
            //Get server first
            DbServer server = Program.conn.GetServerByIdAsync(msg.server_id).GetAwaiter().GetResult();
            if (server == null)
                return;

            //Now, get the tribe ID
            int? tribe = server.TryGetTribeIdAsync(steam_id).GetAwaiter().GetResult();
            if (!tribe.HasValue)
                return;

            //Set needed vars
            target_server = server._id;
            target_tribe = tribe.Value;
            target_map = server.latest_server_map;
            target_server_data = server;
            last_server_refresh = DateTime.UtcNow;

            //Tell the client that we are ready
            SendDTMessage<DTCPayloadReady>(DTCMessageOpcode.DTReady, new DTCPayloadReady
            {
                ok = true,
                server_id = target_server.ToString(),
                tribe_id = target_tribe
            });
        }

        private void HandleAddTileSubscription(DTCPayloadAddTileSubscription msg)
        {
            //Create target
            var target = new LibDeltaSystem.Db.System.Entities.DynamicTileTarget
            {
                map_id = msg.t,
                server_id = target_server.ToString(),
                tribe_id = target_tribe,
                x = msg.x,
                y = msg.y,
                z = msg.z,
                map_name = target_map
            };

            //Add to subs
            SubscriptionManager.SubscribeTile(this, target, msg.i);

            //Check if we already have this file in the cache
            var cached = Program.conn.GetCachedDynamicTile(target).GetAwaiter().GetResult();
            if(cached != null)
            {
                //Send this now
                SendTileLoadMessage(target.map_id, msg.i, cached.url);

                //If the revision matches, don't bother making a new one
                if (cached.revision_id == GetServerData().GetAwaiter().GetResult().revision_id_structures)
                    return;
            }

            //Request tile
            //TODO: This is a hanging operation! Change that!
            Program.builders.RequestImage(target, false, GetServerData().GetAwaiter().GetResult());
        }

        private void HandleRemoveTileSubscription(DTCPayloadRemoveTileSubscription msg)
        {
            //Remove from subs
            SubscriptionManager.UnsubscribeTile(this, msg.i);
        }

        /// <summary>
        /// Gets the data
        /// </summary>
        /// <returns></returns>
        public async Task<DbServer> GetServerData()
        {
            //Check if it is new enough (5 minutes)
            if (DateTime.UtcNow < last_server_refresh.AddMinutes(5))
                return target_server_data;

            //Refresh
            target_server_data = await Program.conn.GetServerByIdAsync(target_server.ToString());
            last_server_refresh = DateTime.UtcNow;
            return target_server_data;
        }
    }
}
