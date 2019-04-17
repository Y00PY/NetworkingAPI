using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Networking.Server
{
    public class NetworkServer : IDisposable
    {
        public const long KEEPALIVE_TIMEOUT = 10000;
        public List<ClientInfo> Clients { get; private set; }
        public int Port { get; private set; }
        private Socket listener;

        private ManualResetEvent allDone = new ManualResetEvent(false);

        public NetworkServer(int port)
        {
            this.Port = port;
            this.Clients = new List<ClientInfo>();
        }

        private void StartInputThread()
        {
            new Thread(() =>
            {
                while (true)
                {
                    Console.Write("> ");
                    string input = Console.ReadLine();

                    if (input.Equals("list"))
                    {
                        Console.WriteLine("--------Clients--------");
                        for (int i = 0; i < this.Clients.Count; i++)
                        {
                            IPEndPoint endpoint = this.Clients[i].Client.RemoteEndPoint as IPEndPoint;

                            Console.WriteLine($"{endpoint.Address.ToString()}:{endpoint.Port.ToString()}");
                        }
                        Console.WriteLine("-----------------------");
                        Console.WriteLine("Count: " + this.Clients.Count);
                        Console.WriteLine("-----------------------");
                    }
                    else if (input.Equals("dlist"))
                    {
                        Console.WriteLine("--------DLIST--------");
                        for (int i = 0; i < this.Clients.Count; i++)
                        {
                            IPEndPoint endpoint = this.Clients[i].Client.RemoteEndPoint as IPEndPoint;

                            Console.WriteLine($"{endpoint.Address.ToString()}:{endpoint.Port.ToString()}\tSeconds until timeout: {(KEEPALIVE_TIMEOUT - (Environment.TickCount - this.Clients[i].LastKeepAlive)) / 1000 }");
                        }
                        Console.WriteLine("-----------------------");
                        Console.WriteLine("Client-Count: " + this.Clients.Count);
                        Console.WriteLine("-----------------------");
                    }
                    else if (input.Equals("help"))
                    {
                        Console.WriteLine("-----HELP-----");
                        Console.WriteLine("list - lists all the clients connected.");
                        Console.WriteLine("dlist - detailed list of all the clients connected.");
                        Console.WriteLine("clear - clears the screen");
                        Console.WriteLine("exit - shutsdown the server");
                    }
                    else if (input.Equals("clear"))
                    {
                        Console.Clear();
                    }
                    else if (input.Equals("exit"))
                    {
                        Environment.Exit(0);
                    }
                }
            }).Start();
        }

        private void StartTimeOutCheckerThread()
        {
            new Thread(() =>
            {
                while (true)
                {
                    lock (Clients)
                    {
                        for (int i = 0; i < this.Clients.Count; i++)
                        {
                            if (Environment.TickCount - Clients[i].LastKeepAlive > KEEPALIVE_TIMEOUT)
                            {
                                DisconnectClientFromList(Clients[i].Client);
                            }
                        }
                    }

                    Thread.Sleep(250);
                }

            }).Start();
        }

        public void Start()
        {
            IPAddress ip = Dns.Resolve(Dns.GetHostName()).AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ip, this.Port);
            Console.WriteLine("Starting server on: " + ip.ToString() + ":" + this.Port);
            this.listener = new Socket(ip.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);
            this.listener.Bind(localEndPoint);
            this.listener.Listen(100);
            Console.WriteLine("Waiting for clients to connect...");
            StartInputThread();
            StartTimeOutCheckerThread();
            while (true)
            {
                allDone.Reset();
                this.listener.BeginAccept(new AsyncCallback(AcceptCallback), this.listener);
                allDone.WaitOne();
            }
        }

        private void AcceptCallback(IAsyncResult res)
        {
            Socket clientSocket = listener.EndAccept(res) as Socket;
            ClientInfo clientInfo = new ClientInfo();
            clientInfo.Client = clientSocket;

            if (this.Clients.Count > 0)
            {
                //Deny connection
                bool found = false;
                for (int i = 0; i < this.Clients.Count; i++)
                {
                    IPEndPoint remote = this.Clients[i].Client.RemoteEndPoint as IPEndPoint;

                    if ((remote?.Address.ToString() == (clientSocket.RemoteEndPoint as IPEndPoint)?.Address.ToString()))
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    clientSocket.Close();
                    allDone.Set();
                    return;
                }
            }

            lock (this.Clients)
            {
                clientInfo.LastKeepAlive = Environment.TickCount;
                this.Clients.Add(clientInfo);
            }

            Console.WriteLine("\n[Server] Client connected: " + ((IPEndPoint)clientSocket.RemoteEndPoint).Address);
            clientSocket.BeginReceive(clientInfo.buffer, 0, ClientInfo.BufferSize, 0,
               new AsyncCallback(ReadCallBack), clientInfo);

            allDone.Set();
        }

        private void ReadCallBack(IAsyncResult res)
        {
            string content = string.Empty;

            ClientInfo clientInfo = (ClientInfo)res.AsyncState;
            Socket clientSocket = clientInfo.Client;

            try
            {
                int bytesRead = clientSocket.EndReceive(res);
                if (bytesRead > 0)
                {
                    clientInfo.sb.Append(Encoding.ASCII.GetString(
                        clientInfo.buffer, 0, bytesRead));

                    content = clientInfo.sb.ToString();
                    if (!string.IsNullOrEmpty(content))
                    {
                        HandlePacket(content, clientInfo);

                        Console.WriteLine($"[{((IPEndPoint)clientInfo.Client.RemoteEndPoint).Address}:{((IPEndPoint)clientInfo.Client.RemoteEndPoint).Port}] {content}");
                    }

                    clientInfo.buffer = new byte[ClientInfo.BufferSize];
                    clientInfo.sb.Clear();

                    clientSocket.BeginReceive(clientInfo.buffer, 0, ClientInfo.BufferSize, 0,
                   new AsyncCallback(ReadCallBack), clientInfo);
                }
                else
                {
                    DisconnectClientFromList(clientSocket);
                }
            }
            catch (Exception)
            {
                DisconnectClientFromList(clientSocket);
            }
            Console.WriteLine("\n[Server] Current clients connected: " + this.Clients.Count);
        }

        private void HandlePacket(string packet, ClientInfo clientInfo)
        {
            if (packet.Equals("keep-alive"))
            {
                clientInfo.LastKeepAlive = Environment.TickCount;
            }
        }

        private void DisconnectClientFromList(Socket clientSocket)
        {
            try
            {
                IPEndPoint ep = clientSocket.RemoteEndPoint as IPEndPoint;
            }
            catch (Exception)
            {
                return;
            }

            lock (Clients)
            {
                for (int i = 0; i < this.Clients.Count; i++)
                {
                    if ((Clients[i].Client.RemoteEndPoint as IPEndPoint)?.Address.ToString() == (clientSocket.RemoteEndPoint as IPEndPoint)?.Address.ToString())
                    {
                        this.Clients.RemoveAt(i);
                        Console.WriteLine("\n[Server] Client disconnected: " + ((IPEndPoint)clientSocket.RemoteEndPoint).Address + ":" + ((IPEndPoint)clientSocket.RemoteEndPoint).Port);
                        clientSocket.Close();
                    }
                }
            }
        }

        private void Send(Socket handler, String data)
        {
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            handler.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), handler);
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket handler = (Socket)ar.AsyncState;

                int bytesSent = handler.EndSend(ar);
                Console.WriteLine("\n[Server] Sent {0} bytes to client.", bytesSent);

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("\n[Server]" + " " + e.ToString());
            }
        }


        public void Stop()
        {
            this.listener.Shutdown(SocketShutdown.Both);
        }

        public void Dispose()
        {
            this.Stop();
        }
    }
}