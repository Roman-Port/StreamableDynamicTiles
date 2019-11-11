using System;
using System.Collections.Generic;
using System.Text;

namespace StreamableDynamicTiles.Messages.Outbound
{
    public class DTCPayloadReady : DTCMessagePayload
    {
        public bool ok;
        public string server_id;
        public int tribe_id;
    }
}
