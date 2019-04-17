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
        public long KEEPALIVE_TIMEOUT { get; private set; } = 10000;
        public List<ClientInfo> Clients { get; private set; }
        public int Port { get; private set; }
        private Socket listener;

        private ManualResetEvent allDone = new ManualResetEvent(false);

        public NetworkServer(int port, long timeoutInMilliseconds)
        {
            this.KEEPALIVE_TIMEOUT = timeoutInMilliseconds;
            this.Port = port;
            this.Clients = new List<ClientInfo>();
        }

        #region StartUp

        public void Start()
        {
            try
            {
                IPAddress ip = Dns.Resolve(Dns.GetHostName()).AddressList[0];
                IPEndPoint localEndPoint = new IPEndPoint(ip, this.Port);
                OutputQueue.Enqueue($"\n[{DateTime.Now.ToShortTimeString()}]" + "Starting server on: " + ip.ToString() + ":" + this.Port);
                this.listener = new Socket(ip.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);
                this.listener.Bind(localEndPoint);
                this.listener.Listen(100);
                OutputQueue.Enqueue($"\n[{DateTime.Now.ToShortTimeString()}]" + "Server started.\nWaiting for clients to connect...");
                StartInputThread();
                StartTimeOutCheckerThread();
                while (true)
                {
                    allDone.Reset();
                    listener.BeginAccept(new AsyncCallback(AcceptCallback), this.listener);
                    allDone.WaitOne();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occured: \n" + e.Message);
                Console.ReadKey(true);
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey(true);
                Environment.Exit(-1);
            }
        }

        #endregion

        #region ConsoleOutput
        private Queue<string> OutputQueue = new Queue<string>();

        private int outCol, outRow, outHeight = Console.WindowHeight / 2;

        private void PrintMessage(string msg, bool appendNewLine)
        {
            int inCol, inRow;
            inCol = Console.CursorLeft;
            inRow = Console.CursorTop;

            int outLines = GetRowCount(outCol, msg) + (appendNewLine ? 1 : 0);
            int outBottom = outRow + outLines;
            if (outBottom > outHeight)
                outBottom = outHeight;
            if (inRow <= outBottom)
            {
                int scrollCount = outBottom - inRow + 1;
                Console.MoveBufferArea(0, inRow, Console.BufferWidth, 1, 0, inRow + scrollCount);
                inRow += scrollCount;
            }
            if (outRow + outLines > outHeight)
            {
                int scrollCount = outRow + outLines - outHeight;
                Console.MoveBufferArea(0, scrollCount, Console.BufferWidth, outHeight - scrollCount, 0, 0);
                outRow -= scrollCount;
                Console.SetCursorPosition(outCol, outRow);
            }
            Console.SetCursorPosition(outCol, outRow);
            if (appendNewLine)
                Console.WriteLine(msg);
            else
                Console.Write(msg);
            outCol = Console.CursorLeft;
            outRow = Console.CursorTop;
            Console.SetCursorPosition(inCol, inRow);
        }

        private int GetRowCount(int startCol, string msg)
        {
            string[] lines = msg.Split('\n');
            int result = 0;
            foreach (string line in lines)
            {
                result += (startCol + line.Length) / Console.BufferWidth;
                startCol = 0;
            }
            return result + lines.Length - 1;
        }

        private void HandleConsoleCommand(string input)
        {
            if (input.ToString().Equals("list"))
            {
                Console.Clear();
                lock (OutputQueue)
                {
                    OutputQueue.Enqueue("--------Clients--------");
                    for (int i = 0; i < this.Clients.Count; i++)
                    {
                        IPEndPoint endpoint = this.Clients[i].Client.RemoteEndPoint as IPEndPoint;

                        OutputQueue.Enqueue($"{endpoint.Address.ToString()}:{endpoint.Port.ToString()}");
                    }
                    OutputQueue.Enqueue("-----------------------");
                    OutputQueue.Enqueue("Count: " + this.Clients.Count);
                    OutputQueue.Enqueue("-----------------------");
                }
            }
            else if (input.ToString().Equals("dlist"))
            {
                Console.Clear();
                lock (OutputQueue)
                {
                    OutputQueue.Enqueue("--------DLIST--------");
                    for (int i = 0; i < this.Clients.Count; i++)
                    {
                        IPEndPoint endpoint = this.Clients[i].Client.RemoteEndPoint as IPEndPoint;
                        OutputQueue.Enqueue($"{endpoint.Address.ToString()}:{endpoint.Port.ToString()}\tSeconds until timeout: {(KEEPALIVE_TIMEOUT - (Environment.TickCount - this.Clients[i].LastKeepAlive)) / 1000 }");
                    }
                    OutputQueue.Enqueue("-----------------------");
                    OutputQueue.Enqueue("Client-Count: " + this.Clients.Count);
                    OutputQueue.Enqueue("-----------------------");
                }
            }
            else if (input.ToString().Equals("help"))
            {
                lock (OutputQueue)
                {
                    OutputQueue.Enqueue("-----HELP-----");
                    OutputQueue.Enqueue("list - lists all the clients connected.");
                    OutputQueue.Enqueue("dlist - detailed list of all the clients connected.");
                    OutputQueue.Enqueue("clear - clears the screen");
                    OutputQueue.Enqueue("exit - shutsdown the server");
                    OutputQueue.Enqueue("--------------");
                }
            }
            else if (input.ToString().Equals("clear"))
            {
                Console.Clear();
            }
            else if (input.ToString().Equals("exit"))
            {
                Environment.Exit(0);
            }
        }

        private void StartInputThread()
        {
            new Thread(() =>
            {
                Console.CursorTop = Console.WindowHeight / 2;
                StringBuilder input = new StringBuilder();
                bool quit = false;

                while (true)
                {
                    do
                    {
                        if (Console.KeyAvailable)
                        {
                            char key = Console.ReadKey(false).KeyChar;

                            if (key == 13)
                            {
                                Console.Write("\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t");
                                Console.CursorLeft = 0;

                                HandleConsoleCommand(input.ToString());
                                input.Clear();

                                Console.CursorTop = Console.WindowHeight / 2;
                            }
                            else if (key == 8)
                            {
                                Console.Write(" ");

                                if (input.Length > 0)
                                    input.Length--;

                                if (Console.CursorLeft > 0)
                                    Console.CursorLeft--;
                            }
                            else
                            {
                                input.Append(key.ToString());
                            }
                        }


                        System.Threading.Thread.Sleep(10);
                        if (OutputQueue.Count > 0)
                        {
                            for (int i = 0; i < OutputQueue.Count; i++)
                            {
                                PrintMessage("\n" + OutputQueue.Dequeue(), true);
                            }
                        }
                    } while (!quit);

                }
            }).Start();
        }
        #endregion

        #region TimeOutHandling
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
        #endregion

        #region Stop
        public void Stop()
        {
            this.listener.Shutdown(SocketShutdown.Both);
        }

        public void Dispose()
        {
            this.Stop();
        }
        #endregion

        #region Communication

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
                        OutputQueue.Enqueue($"\n[{DateTime.Now.ToShortTimeString()}]" + "[Server] Client disconnected: " + ((IPEndPoint)clientSocket.RemoteEndPoint).Address + ":" + ((IPEndPoint)clientSocket.RemoteEndPoint).Port);
                        clientSocket.Close();
                    }
                }
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

            OutputQueue.Enqueue($"\n[{DateTime.Now.ToShortTimeString()}]" + "[Server] Client connected: " + ((IPEndPoint)clientSocket.RemoteEndPoint).Address);
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
                // OutputQueue.Enqueue($"\n[{DateTime.Now.ToShortTimeString()}]"+"\n[Server] Sent {0} bytes to client."+ bytesSent, true);

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
            catch (Exception e)
            {
                OutputQueue.Enqueue($"\n[{DateTime.Now.ToShortTimeString()}]" + "[Server]" + " " + e.ToString());
            }
        }

        #endregion

        #region PacketHandling

        private void HandlePacket(string packet, ClientInfo clientInfo)
        {
            if (packet.Equals("keep-alive"))
            {
                clientInfo.LastKeepAlive = Environment.TickCount;
            }
            else
            {
                OutputQueue.Enqueue($"\n[{DateTime.Now.ToShortTimeString()}]" + $"[{((IPEndPoint)clientInfo.Client.RemoteEndPoint).Address}:{((IPEndPoint)clientInfo.Client.RemoteEndPoint).Port}] {packet}");
            }
        }

        #endregion
    }
}