using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace SPM_AgentService
{
    //https://docs.microsoft.com/ru-ru/dotnet/framework/windows-services/walkthrough-creating-a-windows-service-application-in-the-component-designer
    //https://developingsoftware.com/wix-toolset-install-windows-service/
    //https://wixtoolset.org/documentation/manual/v3/xsd/wix/serviceinstall.html
    //https://wixtoolset.org//documentation/manual/v3/wixui/wixui_dialog_library.html
    //https://helgeklein.com/blog/2014/09/real-world-example-wix-msi-application-installer/
    //https://wixtoolset.org//documentation/manual/v3/wixui/wixui_customizations.html
    //http://www.peshkov.biz/2015/03/replacing-standard-welcomedlg-with.html
    public partial class SPM_AgentService : ServiceBase
    {
        internal static System.Diagnostics.EventLog eventLog;

        internal static DateTime CapturedDateTime { get; set; }
        internal static double CPULoad { get; set; }
        internal static double AllMem { get; set; }
        internal static double FreeMem { get; set; }

        internal static List<KeyValuePair<string, double>> DisksTotalSpaces { get; set; }
        internal static List<KeyValuePair<string, double>> DisksFreeSpaces { get; set; }

        internal static List<KeyValuePair<string, double>> DisksLoad { get; set; }

        internal static List<KeyValuePair<string, double>> NetworkInterfacesLoad { get; set; }


        internal static DateTime Last_Restarted_Time { get; set; }
        internal static DateTime Last_SystemShutingDown_Time { get; set; }
        internal static DateTime Last_ShutdownByUser_Time { get; set; }
        internal static DateTime Last_ResetByUser_Time { get; set; }

        internal static List<EventLogEvent> LastSystemCriticalEvents { get; set; }
        internal static List<EventLogEvent> LastSystemErrorEvents { get; set; }


        internal static int Listen_Port { get; private set; }
        internal readonly string Encryption_Key;
        
        internal static List<KeyValuePair<int, string>> CustomEventsForSend;
        internal static object _customEventsForSend_locker;

        internal static SPM_AgentService Instance { get; private set; } // тут будет инстанс для доступа 

        public SPM_AgentService(string[] args)
        {
            InitializeComponent();
            Instance = this; // инициализируем статическую переменную Instance

            CapturedDateTime = DateTime.MinValue;
            CPULoad = -1;
            AllMem = -1;
            FreeMem = 9999999;
            DisksTotalSpaces = new List<KeyValuePair<string, double>>();
            DisksFreeSpaces = new List<KeyValuePair<string, double>>();
            DisksLoad = new List<KeyValuePair<string, double>>();
            NetworkInterfacesLoad = new List<KeyValuePair<string, double>>();

            Last_Restarted_Time = DateTime.MinValue;
            Last_SystemShutingDown_Time = DateTime.MinValue;
            Last_ShutdownByUser_Time = DateTime.MinValue;
            Last_ResetByUser_Time = DateTime.MinValue;

            LastSystemCriticalEvents = new List<EventLogEvent>();
            LastSystemErrorEvents = new List<EventLogEvent>();

            Listen_Port = 4538;
            Encryption_Key = "default_string";

            CustomEventsForSend = new List<KeyValuePair<int, string>>();
            _customEventsForSend_locker = new object();

            if (args.Length > 0)
            {
                int parsed_port = 0;
                int.TryParse(args[0], out parsed_port);
                if (parsed_port != 0) { Listen_Port = parsed_port; }
            }            

            if (args.Length > 1)
            {
                Encryption_Key = args[1];                
            }
            


            eventLog = new System.Diagnostics.EventLog();
            if (!EventLog.SourceExists("SPM Monitoring system Agent"))
            {
                EventLog.CreateEventSource(
                    "SPM Monitoring system Agent", "Application");
            }
            eventLog.Source = "SPM Monitoring system Agent";
            eventLog.Log = "Application";

        }




        Task ListenerTask = new Task(() =>
        {
            TCP_Listener MyTcpListener = new TCP_Listener(IPAddress.Any, Listen_Port);
        });
        
        protected override void OnStart(string[] args)
        {
            eventLog.WriteEntry("SPM Monitoring system Agent Service is Starting...", EventLogEntryType.Information, 1001);
            // Set up a timer that triggers every minute.
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 15000; // 15 seconds
            timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
            timer.Start();

            DataRefresh();

            ListenerTask.Start();
        }

        protected override void OnStop()
        {
            eventLog.WriteEntry("SPM Monitoring system Agent Service is Stopping...", EventLogEntryType.Information, 1001);
            try
            {
                ListenerTask.Dispose();
            }
            catch { }
        }

        protected override void OnPause()
        {
            eventLog.WriteEntry("SPM Monitoring system Agent Service is Paused...", EventLogEntryType.Information, 1002);
            try
            {
                ListenerTask.Dispose();
            }
            catch { }
        }

        protected override void OnContinue()
        {
            eventLog.WriteEntry("SPM Monitoring system Agent Service is Continue...", EventLogEntryType.Information, 1003);
            try
            {
                ListenerTask.Start();
            }
            catch { }
        }

        
        protected override void OnShutdown()
        {
            eventLog.WriteEntry("SPM Monitoring system Agent Service is Shutdown...", EventLogEntryType.Information, 1004);
            try
            {
                ListenerTask.Dispose();
            }
            catch { }
        }

        

        public void OnTimer(object sender, ElapsedEventArgs args)
        {
            DataRefresh();                    
        }

        
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
                    catch(Exception ex)
                    {
                        getCPULoad_errors_count++;
                        if (getCPULoad_errors_count >= maximumerrors)
                        {
                            eventLog.WriteEntry("Last " + maximumerrors + " attempts to get CPU Load data got an error" + "\n\nException:\n" + ex.Message, EventLogEntryType.Warning, 3001);
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
                            eventLog.WriteEntry("Last " + maximumerrors + " attempts to get Total memory data got an error" + "\n\nException:\n" + ex.Message, EventLogEntryType.Warning, 3002);
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
                            eventLog.WriteEntry("Last " + maximumerrors + " attempts to get Free memory data got an error" + "\n\nException:\n" + ex.Message, EventLogEntryType.Warning, 3003);
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
                            eventLog.WriteEntry("Last " + maximumerrors + " attempts to get Total disk spaces data got an error" + "\n\nException:\n" + ex.Message, EventLogEntryType.Warning, 3004);
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
                            eventLog.WriteEntry("Last " + maximumerrors + " attempts to get Free disk spaces data got an error" + "\n\nException:\n" + ex.Message, EventLogEntryType.Warning, 3005);
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
                            eventLog.WriteEntry("Last " + maximumerrors + " attempts to get Disks Load data got an error" + "\n\nException:\n" + ex.Message, EventLogEntryType.Warning, 3006);
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
                            eventLog.WriteEntry("Last " + maximumerrors + " attempts to get Network interfaces Load got an error" + "\n\nException:\n" + ex.Message, EventLogEntryType.Warning, 3007);
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
                        Thread.Sleep(rnd.Next(10, 50));
                        try { Last_SystemShutingDown_Time = commandProcessor.Get_Last_SystemShutingDown_Time(); } catch (Exception ex) { getEventLogData_exceptions.Add(ex); }                        
                        Thread.Sleep(rnd.Next(10, 50));
                        try { Last_ShutdownByUser_Time = commandProcessor.Get_Last_ShutdownByUser_Time(); } catch (Exception ex) { getEventLogData_exceptions.Add(ex); }                        
                        Thread.Sleep(rnd.Next(10, 50));
                        try { Last_ResetByUser_Time = commandProcessor.Get_Last_ResetByUser_Time(); } catch (Exception ex) { getEventLogData_exceptions.Add(ex); }                        

                        Thread.Sleep(rnd.Next(10, 50));
                        try { LastSystemCriticalEvents = commandProcessor.Get_LastSystemCriticalEvents(); } catch (Exception ex) { getEventLogData_exceptions.Add(ex); }                        
                        Thread.Sleep(rnd.Next(10, 50));
                        try { LastSystemErrorEvents = commandProcessor.Get_LastSystemErrorEvents(); } catch (Exception ex) { getEventLogData_exceptions.Add(ex); }                        


                        
                    if(getEventLogData_exceptions.Count > 0)
                    {
                        getEventLogData_errors_count++;
                        if (getEventLogData_errors_count >= maximumerrors)
                        {
                            string logtext = "Last " + maximumerrors + " attempts to get System Eventlog data got an error" + "\n\nException:\n";

                            foreach (Exception ex in getEventLogData_exceptions)
                            {
                                logtext += ex.Message + "\n\n";
                            }
                            eventLog.WriteEntry(logtext, EventLogEntryType.Warning, 3008);
                            getEventLogData_errors_count = 0;
                        }
                    }
                    else
                    {getEventLogData_errors_count = 0;}
                }
            });

            Task.Run(() =>
            {
                 lock (_customEventsForSend_locker)
                 {
                        if (CustomEventsForSend.Count > 0)
                        {
                            int max_event_count = 500;
                            string first_text = "We have collected " + max_event_count + " data retrieving errors:\n\n";  

                            var query = CustomEventsForSend.Where(x => x.Key == 3006).ToList();
                            if (query.Count() >= max_event_count)
                            {
                                eventLog.WriteEntry(first_text + query[0].Value, EventLogEntryType.Warning, 3006);

                                CustomEventsForSend.RemoveAll(x => x.Key == 3006);

                            }

                            query = CustomEventsForSend.Where(x => x.Key == 3007).ToList();
                            if (query.Count() >= max_event_count)
                            {
                                eventLog.WriteEntry(first_text + query[0].Value, EventLogEntryType.Warning, 3007);

                                CustomEventsForSend.RemoveAll(x => x.Key == 3007);

                            }

                            //eventLog.WriteEntry(CustomEventsForSend.Count.ToString(), EventLogEntryType.Warning, 3110);
                        }
                 }
                                     
                
            });
        }
    }
}
