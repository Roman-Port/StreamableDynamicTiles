using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using LibDeltaSystem;
using LibDeltaSystem.Tools.InternalComms;
using System.Threading.Tasks;
using LibDeltaSystem.Db.System.Entities;
using LibDeltaSystem.Entities.DynamicTiles;
using LibDeltaSystem.Db.System;

namespace StreamableDynamicTiles.TileBuilders
{
    public class BuilderNetworking : InternalCommsServer
    {
        /// <summary>
        /// List of connected servers.
        /// </summary>
        public List<BuilderServer> servers;

        public BuilderNetworking(DeltaConnection conn, byte[] key, int port) : base(conn, key, port)
        {
            servers = new List<BuilderServer>();
        }

        public override InternalCommsServerClient GetClient(DeltaConnection conn, byte[] key, Socket sock)
        {
            return new BuilderServer(conn, key, sock, this);
        }

        public override void OnClientAuthorized(InternalCommsServerClient client)
        {
            
        }

        public override void OnClientDisconnected(InternalCommsServerClient client)
        {
            
        }

        /// <summary>
        /// Called when an updated tile is needed
        /// </summary>
        public bool RequestImage(DynamicTileTarget target, bool highPriority, DbServer sdata)
        {
            //Check if any servers exist
            if (servers.Count == 0)
                return false;

            //Loop through servers and find server with the least number of requests pending
            int min = int.MaxValue;
            BuilderServer server = null;
            lock(servers)
            {
                foreach(var s in servers)
                {
                    if(s.pending.Count < min)
                    {
                        min = s.pending.Count;
                        server = s;
                    }
                }
            }

            //If no servers were found, return false
            if (server == null)
                return false;

            //Add this tile to the list
            server.ProcessTile(new DynamicTileBuilderRequest
            {
                highPriority = highPriority,
                target = target,
                structure_revision_id = sdata.revision_id_structures
            });
            return true;
        }
    }
}
