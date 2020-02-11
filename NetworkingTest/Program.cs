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


            /*  new Thread(() =>
              {
                  new NetworkServer(1337, 10000).Start();
              }).Start();
             
            new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        NetworkClient client = new NetworkClient("192.168.28.203", 10001);
                        client.Connect();


                        while (true)
                        {
                            client.Read();


                            Thread.Sleep(500);

                            if (!client.Send(Encoding.ASCII.GetBytes(@"{
                              ""type"": ""Auth"",
                              ""data"": {
                                ""nickname"": ""lmao"",
                                ""uuid"": ""abcd""
                                     }
                                }
                        ".Replace("lmao", new Random().Next().ToString() + "ALKDSAWSd").Replace("abcd", Guid.NewGuid().ToString()))))
                            {
                                client.Disconnect();
                                break;
                            }
                            else
                            {
                                Thread.Sleep(1000);


                                Console.WriteLine("package sent");
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
        }*/
    }
}
