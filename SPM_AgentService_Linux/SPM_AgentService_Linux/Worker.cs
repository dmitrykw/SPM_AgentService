using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace SPM_AgentService_Linux
{
    public class Worker : BackgroundService
    {
        public readonly ILogger<Worker> _logger;
        public static Worker Instance { get; private set; }

        public Worker(ILogger<Worker> logger)
        {
            Instance = this;
            _logger = logger;
            _logger.LogInformation("SPM Monitoring system Agent Service is Starting...");
        }

 
        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SPM Monitoring system Agent Service is Shutdown...");
            await Task.Delay(1, stoppingToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SPM Monitoring TCP_Listener init...");

            Settings settings = new Settings();
            Listen_Port = settings.Listen_Port;
            Encryption_Key = settings.Encryption_Key;

            ListenerTask.Start();           

            while (!stoppingToken.IsCancellationRequested)
            {                
                DataRefresh();
                await Task.Delay(15000, stoppingToken);
            }
        }

        


        internal static DateTime CapturedDateTime = DateTime.MinValue;
        internal static double CPULoad = -1;
        internal static double AllMem = -1;
        internal static double FreeMem = 9999999;

        internal static List<KeyValuePair<string, double>> DisksTotalSpaces = new List<KeyValuePair<string, double>>();
        internal static List<KeyValuePair<string, double>> DisksFreeSpaces = new List<KeyValuePair<string, double>>();

        internal static List<KeyValuePair<string, double>> DisksLoad = new List<KeyValuePair<string, double>>();

        internal static List<KeyValuePair<string, double>> NetworkInterfacesLoad = new List<KeyValuePair<string, double>>();

        internal static DateTime Last_Restarted_Time = DateTime.MinValue;



        internal static int Listen_Port { get; private set; }
        internal static string Encryption_Key { get; private set; }

        internal static List<KeyValuePair<int, string>> CustomEventsForSend = new List<KeyValuePair<int, string>>();
        internal static object _customEventsForSend_locker = new object();
        

        public Worker()
        {          
            //InitializeComponent();            
        }




        Task ListenerTask = new Task(() =>
        {
            TCP_Listener MyTcpListener = new TCP_Listener(IPAddress.Any, Listen_Port, Encryption_Key);
        });

       


        private int maximumerrors = 15;

        private int getCPULoad_errors_count = 0;
        private object _getCPULoad_locker = new object();

        private int getAllMemorySize_errors_count = 0;
        private object _getAllMemorySize_locker = new object();

        private int getAvailableMemory_errors_count = 0;
        private object _getAvailableMemory_locker = new object();

        private int getAllDrivesTotalSpace_errors_count = 0;
        private object _getAllDrivesTotalSpace_locker = new object();

        private int getAllDrivesFreeSpace_errors_count = 0;
        private object _getAllDrivesFreeSpace_locker = new object();

        private int getAllDrivesLoad_errors_count = 0;
        private object _getAllDrivesLoad_locker = new object();

        private int getAllNetworkInterfacesLoad_errors_count = 0;
        private object _getAllNetworkInterfacesLoad_locker = new object();

        private int getEventLogData_errors_count = 0;
        private object _getEventLogData_locker = new object();


        private void DataRefresh()
        {
            CapturedDateTime = DateTime.Now;
            CommandProcessor commandProcessor = new CommandProcessor();
            Random rnd = new Random();
            Task.Run(() =>
            {
                lock (_getCPULoad_locker)
                {
                    try
                    {
                        CPULoad = commandProcessor.GetCPULoad();
                        getCPULoad_errors_count = 0;
                    }
                    catch (Exception ex)
                    {
                        getCPULoad_errors_count++;
                        if (getCPULoad_errors_count >= maximumerrors)
                        {
                            _logger.LogWarning("Last " + maximumerrors + " attempts to get CPU Load data got an error" + " Exception: " + ex.Message);                            
                            getCPULoad_errors_count = 0;
                        }
                    }
                }
            });
            Task.Run(() =>
            {
                lock (_getAllMemorySize_locker)
                {
                    try
                    {
                        AllMem = commandProcessor.GetAllMemorySize();
                        getAllMemorySize_errors_count = 0;
                    }
                    catch (Exception ex)
                    {
                        getAllMemorySize_errors_count++;
                        if (getAllMemorySize_errors_count >= maximumerrors)
                        {
                            _logger.LogWarning("Last " + maximumerrors + " attempts to get Total memory data got an error" + " Exception: " + ex.Message);                            
                            getAllMemorySize_errors_count = 0;
                        }
                    }
                }
            });
            Task.Run(() =>
            {
                lock (_getAvailableMemory_locker)
                {
                    try
                    {
                        FreeMem = commandProcessor.GetAvailableMemory();
                        getAvailableMemory_errors_count = 0;
                    }
                    catch (Exception ex)
                    {
                        getAvailableMemory_errors_count++;
                        if (getAvailableMemory_errors_count >= maximumerrors)
                        {
                            _logger.LogWarning("Last " + maximumerrors + " attempts to get Free memory data got an error" + " Exception: " + ex.Message);                            
                            getAvailableMemory_errors_count = 0;
                        }
                    }
                }
            });
            Task.Run(() =>
            {
                lock (_getAllDrivesTotalSpace_locker)
                {
                    try
                    {
                        DisksTotalSpaces = commandProcessor.GetAllDrivesTotalSpace();
                        getAllDrivesTotalSpace_errors_count = 0;
                    }
                    catch (Exception ex)
                    {
                        getAllDrivesTotalSpace_errors_count++;
                        if (getAllDrivesTotalSpace_errors_count >= maximumerrors)
                        {
                            _logger.LogWarning("Last " + maximumerrors + " attempts to get Total disk spaces data got an error" + " Exception: " + ex.Message);                            
                            getAllDrivesTotalSpace_errors_count = 0;
                        }
                    }
                }

            });
            Task.Run(() =>
            {
                lock (_getAllDrivesFreeSpace_locker)
                {
                    try
                    {
                        DisksFreeSpaces = commandProcessor.GetAllDrivesFreeSpace();
                        getAllDrivesFreeSpace_errors_count = 0;
                    }
                    catch (Exception ex)
                    {
                        getAllDrivesFreeSpace_errors_count++;
                        if (getAllDrivesFreeSpace_errors_count >= maximumerrors)
                        {
                            _logger.LogWarning("Last " + maximumerrors + " attempts to get Free disk spaces data got an error" + " Exception: " + ex.Message);                            
                            getAllDrivesFreeSpace_errors_count = 0;
                        }
                    }
                }
            });
            Task.Run(() =>
            {
                lock (_getAllDrivesLoad_locker)
                {
                    try
                    {
                        DisksLoad = commandProcessor.GetAllDrivesLoad();
                        getAllDrivesLoad_errors_count = 0;
                    }
                    catch (Exception ex)
                    {
                        getAllDrivesLoad_errors_count++;
                        if (getAllDrivesLoad_errors_count >= maximumerrors)
                        {
                            _logger.LogWarning("Last " + maximumerrors + " attempts to get Disks Load data got an error" + " Exception: " + ex.Message);                            
                            getAllDrivesLoad_errors_count = 0;
                        }
                    }
                }
            });
            Task.Run(() =>
            {
                lock (_getAllNetworkInterfacesLoad_locker)
                {
                    try
                    {
                        NetworkInterfacesLoad = commandProcessor.GetAllNetworkInterfacesLoad();
                        getAllNetworkInterfacesLoad_errors_count = 0;
                    }
                    catch (Exception ex)
                    {
                        getAllNetworkInterfacesLoad_errors_count++;
                        if (getAllNetworkInterfacesLoad_errors_count >= maximumerrors)
                        {
                            _logger.LogWarning("Last " + maximumerrors + " attempts to get Network interfaces Load got an error" + " Exception: " + ex.Message);                            
                            getAllNetworkInterfacesLoad_errors_count = 0;
                        }
                    }
                }
            });

            Task.Run(() =>
            {
                lock (_getEventLogData_locker)
                {

                    List<Exception> getEventLogData_exceptions = new List<Exception>();

                    try { Last_Restarted_Time = commandProcessor.Get_Last_Restarted_Time(); } catch (Exception ex) { getEventLogData_exceptions.Add(ex); }
                    //Thread.Sleep(rnd.Next(10, 50));
                    

                    if (getEventLogData_exceptions.Count > 0)
                    {
                        getEventLogData_errors_count++;
                        if (getEventLogData_errors_count >= maximumerrors)
                        {
                            string logtext = "Last " + maximumerrors + " attempts to get System Eventlog data got an error" + "\n\nException:\n";

                            foreach (Exception ex in getEventLogData_exceptions)
                            {
                                logtext += ex.Message + "\n\n";
                            }
                            _logger.LogWarning(logtext);
                            getEventLogData_errors_count = 0;
                        }
                    }
                    else
                    { getEventLogData_errors_count = 0; }
                }
            });

            Task.Run(() =>
            {
                lock (_customEventsForSend_locker)
                {
                    if (CustomEventsForSend.Count > 0)
                    {
                        int max_event_count = 500;
                        string first_text = "We have collected " + max_event_count + " data retrieving errors: ";

                        var query = CustomEventsForSend.Where(x => x.Key == 3006).ToList();
                        if (query.Count() >= max_event_count)
                        {
                            _logger.LogWarning(first_text + query[0].Value);                            

                            CustomEventsForSend.RemoveAll(x => x.Key == 3006);

                        }

                        query = CustomEventsForSend.Where(x => x.Key == 3007).ToList();
                        if (query.Count() >= max_event_count)
                        {
                            _logger.LogWarning(first_text + query[0].Value);                            

                            CustomEventsForSend.RemoveAll(x => x.Key == 3007);

                        }
                        
                    }
                }


            });
        }
    }
}
