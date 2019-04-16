using Networking.Client;
using Networking.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace NetworkingTest
{
    class Program
    {
        static void Main(string[] args)
        {

            new Thread(() =>
            {
                new NetworkServer(1337).Start();
            }).Start();

            NetworkClient client = new NetworkClient("127.0.0.1", 1337);
            Thread.Sleep(1000);
            client.Connect();
            Thread.Sleep(1000);
            client.Send(Encoding.ASCII.GetBytes("test-msg"));
            Thread.Sleep(1000);
            client.Disconnect();
            while (true) ;
        }
    }
}
