using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace Networking.Client
{
    public class NetworkClient : IDisposable
    {
        public bool Connected { get; private set; }
        public int Port { get; private set; }
        private TcpClient tcpClient;
        private IPAddress iPAddress;


        public NetworkClient(string ip, int port)
        {
            this.tcpClient = new TcpClient();
            this.iPAddress = IPAddress.Parse(ip);
            this.Port = port;
        }

        public void Read()
        {
            byte[] bytes = new byte[tcpClient.ReceiveBufferSize];

            tcpClient.GetStream().Read(bytes, 0, (int)tcpClient.ReceiveBufferSize);

            string returndata = Encoding.UTF8.GetString(bytes);

            Console.WriteLine("[READ] Response: " + returndata);
        }

        public void Connect()
        {
            if (!Connected)
            {
                try
                {
                    this.tcpClient.Connect(this.iPAddress, this.Port);
                    this.Connected = true;
                }
                catch (Exception e)
                {
                    this.Connected = false;
                    Console.WriteLine("[Client] Connection failed.");
                    Console.WriteLine("[Client] Error: " + e.Message);
                }
            }
            else
            {
                Console.WriteLine("[Client] Already connected to: " + this.iPAddress.ToString() + ":" + this.Port);
            }
        }

        public bool Send(byte[] buffer)
        {
            if (this.tcpClient.Connected)
            {
                try
                {
                    BinaryWriter writer = new BinaryWriter(tcpClient.GetStream());
                    writer.Write(Encoding.Default.GetString(buffer));
                    writer.Flush();
                    //this.tcpClient.GetStream().Write(buffer, 0, buffer.Length);
                    //this.tcpClient.GetStream().Flush();
                    return true;
                }
                catch (Exception)
                {
                    Console.WriteLine("[Client] Failed to send packet: " + Encoding.ASCII.GetString(buffer));
                    return false;
                }
            }
            else
            {
                return false;
            }
        }


        public void Disconnect()
        {
            this.Connected = false;
            this.tcpClient.Close();
        }

        public void Dispose()
        {
            this.Connected = false;
            this.tcpClient.Close();
        }
    }
}