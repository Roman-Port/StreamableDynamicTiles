using LibDeltaSystem.Db.System.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace StreamableDynamicTiles.Messages.Inbound
{
    public class DTCPayloadRemoveTileSubscription : DTCMessagePayload
    {
        public int i;
        public DynamicTileType t;
    }
}
