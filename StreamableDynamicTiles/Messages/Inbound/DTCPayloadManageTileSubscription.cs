using LibDeltaSystem.Db.System.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace StreamableDynamicTiles.Messages.Inbound
{
    public class DTCPayloadAddTileSubscription : DTCMessagePayload
    {
        public int x;
        public int y;
        public int z;
        public DynamicTileType t;
        public int i; //Index
    }
}
