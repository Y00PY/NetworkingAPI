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
                new NetworkServer(1337, 10000).Start();
            }).Start();

            new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        NetworkClient client = new NetworkClient("10.9.242.100", 1337);
                        client.Connect();

                        while (true)
                        {
                            Thread.Sleep(5000);

                            if (!client.Send(Encoding.ASCII.GetBytes("keep-alive")))
                            {
                                client.Disconnect();
                                break;
                            }
                        }
                    }
                    catch (Exception) { }
                }
            }).Start();

            while (true)
            {
                Thread.Sleep(500);
            }
        }
    }
}
