using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Timers;
using Microsoft.Extensions.Logging;

namespace SPM_AgentService_Linux
{
    class JunkClientIP
    {
        public IPAddress IP { get; set; }
        public DateTime RemoveTime { get; set; }
        public JunkClientIP(IPAddress input_ip, DateTime input_removetime)
        {
            this.IP = input_ip;
            this.RemoveTime = input_removetime;
        }
    }



    class TCP_Listener
    {   
        private string Encryption_Key { get; set; }

        private const int floodBanning_junk_Packet_count = 300;
        private const int floodBanning_Minutes = 5;
        private int Port { get; set; }
        private IPAddress localAddr { get; set; }
        private TcpListener Server { get; set; }


        private List<JunkClientIP> junkRequestIPs;
        System.Timers.Timer junkRequestIPs_Reset_Timer;

        public TCP_Listener(IPAddress input_localaddr, int input_port, string encryption_key)
        {
            Encryption_Key = encryption_key;
            Init(input_localaddr, input_port);

        }



        private void Init(IPAddress input_localaddr, int input_port)
        {
            junkRequestIPs = new List<JunkClientIP>();
            junkRequestIPs_Reset_Timer = new System.Timers.Timer();
            junkRequestIPs_Reset_Timer.Interval = 60000; // 1 min
            junkRequestIPs_Reset_Timer.Elapsed += new ElapsedEventHandler(this.junkRequestIPs_Reset_Timer_Elapsed);
            junkRequestIPs_Reset_Timer.Start();


            this.Port = input_port;
            this.localAddr = input_localaddr;
            Server = new TcpListener(localAddr, Port);
            Worker.Instance._logger.LogInformation("SPM Monitoring system Agent Server Starting on Port: " + Port.ToString());            

            Server.Start();
            Listen();

        }

        private async void Listen()
        {
            while (true)
            {
                try
                {
                    // Подключение клиента
                    TcpClient client = await Server.AcceptTcpClientAsync();
                    IPAddress ClientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address;
                    var queryList = junkRequestIPs.Select(x => x.IP.Equals(ClientIP)).ToList();

                    if (queryList.Count() == floodBanning_junk_Packet_count)
                    {
                        Worker.Instance._logger.LogWarning("TCP Flood Attack detected from IP: " + ClientIP.ToString() + " - banned for " + floodBanning_Minutes + " min" + ". If it is your own Simple Ping Monitor ip address, check the Encryption Key which is set on this host Agent service and Simple Ping Monitor Options. To change Encryption Key on the Agent on this host you must reinstall the Agent.");                        
                        junkRequestIPs.Add(new JunkClientIP(ClientIP, DateTime.Now.AddMinutes(floodBanning_Minutes))); //Добавляем еще один, чтобы общее количество стало больше чем (a==b)
                    }

                    if (queryList.Count() < floodBanning_junk_Packet_count)
                    {


                        NetworkStream stream = client.GetStream();
                        // Обмен данными
                        try
                        {
                            if (stream.CanRead)
                            {
                                byte[] myReadBuffer = new byte[256];
                                StringBuilder myCompleteMessage = new StringBuilder();
                                int numberOfBytesRead = 0;
                                do
                                {
                                    numberOfBytesRead = stream.Read(myReadBuffer, 0, myReadBuffer.Length);
                                    myCompleteMessage.AppendFormat("{0}", Encoding.UTF8.GetString(myReadBuffer, 0, numberOfBytesRead));
                                }
                                while (stream.DataAvailable);

                                Byte[] responseData = Encoding.UTF8.GetBytes("NoCommand");
                                CommandProcessor commandProcessor = new CommandProcessor();
                                try
                                {
                                    string inputcommand = MyEncryption.DecryptText(myCompleteMessage.ToString(), Encryption_Key);
                                    string response = MyEncryption.EncryptText(commandProcessor.GetCachedAnswer(inputcommand), Encryption_Key);
                                    responseData = Encoding.UTF8.GetBytes(response);


                                    junkRequestIPs.RemoveAll(s => s.IP.Equals(ClientIP));

                                }
                                catch
                                {
                                    junkRequestIPs.Add(new JunkClientIP(ClientIP, DateTime.Now.AddMinutes(floodBanning_Minutes)));
                                }


                                stream.Write(responseData, 0, responseData.Length);

                            }
                        }
                        finally
                        {
                            stream.Close();
                            client.Close();
                        }
                    }
                    else { client.Close(); }
                }
                catch (Exception ex)               
                {
                    Server.Stop();
                    Worker.Instance._logger.LogError("SPM Monitoring system Agent server is broken and now will restart... Error:" + ex.Message);                    
                    Init(this.localAddr, this.Port);
                    break;
                }

            }

        }

        public void junkRequestIPs_Reset_Timer_Elapsed(object sender, ElapsedEventArgs args)
        {
            if (junkRequestIPs.Count() > 0)
            {
                List<IPAddress> RestoreIPsList = new List<IPAddress>();
                foreach (var junkClient in junkRequestIPs)
                {
                    if (DateTime.Now >= junkClient.RemoveTime)
                    {
                        if (!RestoreIPsList.Contains(junkClient.IP))
                        {
                            RestoreIPsList.Add(junkClient.IP);
                        }
                    }
                }

                foreach (IPAddress ip in RestoreIPsList)

                {
                    int countOfIPs = junkRequestIPs.Select(x => x.IP.Equals(ip)).Count();
                    junkRequestIPs.RemoveAll(s => s.IP.Equals(ip));
                    if (countOfIPs >= floodBanning_junk_Packet_count)
                    {
                        Worker.Instance._logger.LogInformation("Early banned Client IP: " + ip.ToString() + " - now is restored");                        
                    }
                }


            }
        }

    }
}
