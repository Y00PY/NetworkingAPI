using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Networking.Server
{
    public class ClientInfo
    {
        public Socket Client = null;
        public const int BufferSize = 1024;
        public byte[] buffer = new byte[BufferSize];
        public StringBuilder sb = new StringBuilder();
        public long LastKeepAlive = 0;
    }
}
