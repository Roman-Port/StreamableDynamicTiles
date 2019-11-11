using LibDeltaSystem.Db.System.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace StreamableDynamicTiles.Messages.Outbound
{
    public class DTCPayloadTileLoad
    {
        public int i;
        public DynamicTileType t;
        public string url;
    }
}
