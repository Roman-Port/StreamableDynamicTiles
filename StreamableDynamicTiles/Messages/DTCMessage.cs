using System;
using System.Collections.Generic;
using System.Text;

namespace StreamableDynamicTiles.Messages
{
    public class DTCMessage<T>
    {
        public T payload;
        public DTCMessageOpcode opcode;
    }
}
