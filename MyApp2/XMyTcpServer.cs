using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MyApp2
{   
    class XMyTcpServer
    {
        public Dictionary<string, Socket> ClientList = new Dictionary<string, Socket>();
        public Socket Socket_Server;
        public int newClient = 0;
    }
}
