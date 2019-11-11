using System;
using System.Collections.Generic;
using System.Text;

namespace StreamableDynamicTiles.Messages
{
    public enum DTCMessageOpcode
    {
        SetServer = 0, //Inbound
        DTReady = 1, //Outbound
        AddTileSubscription = 2, //Inbound
        RemoveTileSubscription = 3, //Inbound
        TileLoad = 4 //Outbound
    }
}
