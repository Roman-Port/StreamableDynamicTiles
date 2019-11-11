using LibDeltaSystem.Db.System.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace StreamableDynamicTiles.Entities
{
    /// <summary>
    /// Represents a subscription to a tile by a user
    /// </summary>
    public class TileSubscription
    {
        public DynamicTileTarget target;
        public DTSession session;
        public int token;
    }
}
