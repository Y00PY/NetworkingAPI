﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Networking.Server
{
    public class NetworkServer : IDisposable
    {
        public List<Socket> Clients { get; private set; }
        public int Port { get; private set; }
        private Socket listener;

        private ManualResetEvent allDone = new ManualResetEvent(false);

        public NetworkServer(int port)
        {
            this.Port = port;
            this.Clients = new List<Socket>();
        }

        public void Start()
        {
            Console.WriteLine("Starting server on: " + this.Port);
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, this.Port);
            this.listener = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);
            //this.listener.ReceiveTimeout = 10000;
            this.listener.Bind(localEndPoint);
            this.listener.Listen(100);
            Console.WriteLine("Waiting for clients to connect...");

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

            if (this.Clients.Count == 0)
            {
                this.Clients.Add(clientSocket);
            }
            else
            {
                bool wasClosed = false;
                for (int i = 0; i < this.Clients.Count; i++)
                {
                    IPEndPoint remote = this.Clients[i].RemoteEndPoint as IPEndPoint;

                    if ((remote?.Address.ToString() == (clientSocket.RemoteEndPoint as IPEndPoint)?.Address.ToString()))
                    {
                        clientSocket.Close();
                        wasClosed = true;
                        break;
                    }
                }

                if (wasClosed)
                {
                    allDone.Set();
                    return;
                }
            }

            clientSocket.BeginReceive(clientInfo.buffer, 0, ClientInfo.BufferSize, 0,
               new AsyncCallback(ReadCallBack), clientInfo);

            allDone.Set();
        }

        private void ReadCallBack(IAsyncResult res)
        {
            string content = string.Empty;

            ClientInfo clientInfo = (ClientInfo)res.AsyncState;
            Socket clientSocket = clientInfo.Client;

            int bytesRead = clientSocket.EndReceive(res);

            if (bytesRead > 0)
            {
                Console.WriteLine("[Server] Client connected: " + ((IPEndPoint)clientSocket.RemoteEndPoint).Address);
                clientInfo.sb.Append(Encoding.ASCII.GetString(
                    clientInfo.buffer, 0, bytesRead));

                content = clientInfo.sb.ToString();
                if (!string.IsNullOrEmpty(content))
                {
                    Console.WriteLine($"[{((IPEndPoint)clientInfo.Client.RemoteEndPoint).Address}:{((IPEndPoint)clientInfo.Client.RemoteEndPoint).Port}] {content}");
                }

                clientSocket.BeginReceive(clientInfo.buffer, 0, ClientInfo.BufferSize, 0,
               new AsyncCallback(ReadCallBack), clientInfo);
            }
            else
            {
                for (int i = 0; i < this.Clients.Count; i++)
                {
                    IPEndPoint remote = this.Clients[i].RemoteEndPoint as IPEndPoint;

                    if (remote?.Address == (clientSocket.RemoteEndPoint as IPEndPoint)?.Address && remote?.Port == (clientSocket.RemoteEndPoint as IPEndPoint)?.Port)
                    {
                        this.Clients.RemoveAt(i);
                    }
                    else
                    {
                        Console.WriteLine("An error occured");
                    }
                }

                Console.WriteLine("[Server] Client disconnected: " + ((IPEndPoint)clientSocket.RemoteEndPoint).Address + ":" + ((IPEndPoint)clientInfo.Client.RemoteEndPoint).Port);
            }


            Console.WriteLine("[Server] Current clients connected: " + this.Clients.Count);
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
                Console.WriteLine("[Server] Sent {0} bytes to client.", bytesSent);

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("[Server]" + " " + e.ToString());
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