using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SPM_AgentService
{
    class CommandProcessor
    {


        public string GetCachedAnswer(string inputCommand)
        {
            string command = inputCommand;
            string argument = "";
            string result = "";
            if (inputCommand.Contains('?'))
            {
                string[] cmdAndArg = inputCommand.ToLower().Split('?');
                command = cmdAndArg[0];
                argument = cmdAndArg[1];
            }
            switch (command)
            {
                case "getalldata":

                    result = GetAllDataJSON();

                    break;
                case "getcpuload":
                    result = SPM_AgentService.CPULoad.ToString();                    
                    break;
                case "gettotalmemory":
                    result = SPM_AgentService.AllMem.ToString();
                    break;
                case "getavailablememory":
                    result = SPM_AgentService.FreeMem.ToString();                    
                    break;
                case "getalldrives":

                    foreach (var disk in SPM_AgentService.DisksTotalSpaces)
                    {
                        result += disk.Key + ";";
                    }                    
                    break;
                case "getdrivetotalspace":

                    if (argument != "")
                    {
                        var FoundDrives = SPM_AgentService.DisksTotalSpaces.Where(drv => drv.Key.ToLower().Equals(argument.ToLower())).ToList();

                        if (FoundDrives.Count == 1)
                        {
                            result = FoundDrives[0].Value.ToString();
                        }
                    }
                    break;
                case "getdrivefreespace":

                    if (argument != "")
                    {
                        var FoundDrives = SPM_AgentService.DisksFreeSpaces.Where(drv => drv.Key.ToLower().Equals(argument.ToLower())).ToList();

                        if (FoundDrives.Count == 1)
                        {
                            result = FoundDrives[0].Value.ToString();
                        }
                    }
                    break;
                case "getdriveload":

                    if (argument != "")
                    {
                        var FoundDisks = SPM_AgentService.DisksLoad.Where(drv => drv.Key.ToLower().Equals(argument.ToLower())).ToList();

                        if (FoundDisks.Count == 1)
                        {
                            result = FoundDisks[0].Value.ToString();
                        }
                    }
                    break;
                case "getallnetworkadapters":

                    foreach (var adapter in SPM_AgentService.NetworkInterfacesLoad)
                    {
                        result += adapter.Key + ";";
                    }
                    break;
                case "getnetworkadapterload":
                    if (argument != "")
                    {
                        var FoundAdapters = SPM_AgentService.NetworkInterfacesLoad.Where(adapter => adapter.Key.ToLower().Equals(argument.ToLower())).ToList();

                        if (FoundAdapters.Count == 1)
                        {
                            result = FoundAdapters[0].Value.ToString();
                        }
                    }
                    break;
                case "getlastrestartedtime":
                    result = SPM_AgentService.Last_Restarted_Time.ToString();
                    break;
                case "getlastsystemshutingdowntime":
                    result = SPM_AgentService.Last_SystemShutingDown_Time.ToString();
                    break;
                case "getlastresetbyusertime":
                    result = SPM_AgentService.Last_ResetByUser_Time.ToString();
                    break;
                case "getlastshutdownbyusertime":
                    result = SPM_AgentService.Last_ShutdownByUser_Time.ToString();
                    break;

                case "getlastsystemcriticalevents":
                    foreach (var myevent in SPM_AgentService.LastSystemCriticalEvents)
                    {
                        result += "TimeCreated: " + myevent.TimeCreated.ToString() + "\nDescription: " + myevent.Description + "\nLogName: " + myevent.LogName + "\nLevel: " + myevent.Level + "\nTask: " + myevent.Task + "\nProviderName: " + myevent.ProviderName + "\nOpCode: " + myevent.OpCode + "\nEventID: " + myevent.EventID + ";\n";
                    }

                    break;

                case "getlastsystemerrorevents":
                    foreach (var myevent in SPM_AgentService.LastSystemErrorEvents)
                    {
                        result += "TimeCreated: " + myevent.TimeCreated.ToString() + "\nDescription: " + myevent.Description + "\nLogName: " + myevent.LogName + "\nLevel: " + myevent.Level + "\nTask: " + myevent.Task + "\nProviderName: " + myevent.ProviderName + "\nOpCode: " + myevent.OpCode + "\nEventID: " + myevent.EventID + ";\n";
                    }
                    break;
                default:
                    result = "command_not_found";
                    break;
            }
            if (result == "") { result = "no_data"; }
            return result;
        }

        private string GetAllDataJSON()
        {
            try
            {
                AllDataObject returnobj = new AllDataObject();
                returnobj.AgentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                returnobj.CapturedDateTime = SPM_AgentService.CapturedDateTime;
                returnobj.CPULoad = SPM_AgentService.CPULoad;
                returnobj.AllMem = SPM_AgentService.AllMem;
                returnobj.FreeMem = SPM_AgentService.FreeMem;
                returnobj.DisksTotalSpaces = SPM_AgentService.DisksTotalSpaces;
                returnobj.DisksFreeSpaces = SPM_AgentService.DisksFreeSpaces;
                returnobj.DisksLoad = SPM_AgentService.DisksLoad;
                returnobj.NetworkInterfacesLoad = SPM_AgentService.NetworkInterfacesLoad;

                returnobj.Last_Restarted_Time = SPM_AgentService.Last_Restarted_Time;
                returnobj.Last_SystemShutingDown_Time = SPM_AgentService.Last_SystemShutingDown_Time;
                returnobj.Last_ShutdownByUser_Time = SPM_AgentService.Last_ShutdownByUser_Time;
                returnobj.Last_ResetByUser_Time = SPM_AgentService.Last_ResetByUser_Time;

                returnobj.LastSystemCriticalEvents = SPM_AgentService.LastSystemCriticalEvents;
                returnobj.LastSystemErrorsEvents = SPM_AgentService.LastSystemErrorEvents;

                return JsonConvert.SerializeObject(returnobj);
            }
            catch {                
                return "error_when_serialize_AllDataObject";
            }
        }



        internal double GetCPULoad()
        {
            try
            {
                double result = 0;


                using (PerformanceCounter theCPUCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"))
                {
                    DateTime startMeasure = DateTime.Now;
                    int ValuesCounter = 0;
                    float value = 0;

                    int fails_counter = 0;
                    while (DateTime.Now < startMeasure.AddSeconds(10))
                    {                        
                        try { value = theCPUCounter.NextValue();
                            result += value;
                            ValuesCounter++;

                        } catch(Exception ex) { value = 0; fails_counter++;
                            if (fails_counter >= 40) { throw new Exception("40+ fails while measure CPU Load Process Exception: " + ex.Message); }
                        }
                       
                        Thread.Sleep(200);
                    }
                    result = Math.Round(Convert.ToDouble(result / ValuesCounter), 2);
                }  
                
                return result;
            }
            catch(Exception ex) { throw new Exception("GetCPULoad() Exception: " + ex.Message); }
        }


        
        internal double GetAllMemorySize()
        {
            try
            {
                double result = 0;
                double value = 0;
               
                ManagementClass mc = new ManagementClass("Win32_ComputerSystem");
                ManagementObjectCollection moc = mc.GetInstances();
                foreach (ManagementObject item in moc)
                {
                   value = Convert.ToDouble(item.Properties["TotalPhysicalMemory"].Value) / 1048576; 
                }
                              

                result = Math.Round(value, 0);

                return result;
            }
            catch (Exception ex) { throw new Exception("GetAllMemorySize() Exception: " + ex.Message); }
        }

        internal double GetAvailableMemory()
        {
            try
            {
                double result = 0;
                float value = 0;                
                using (PerformanceCounter theMemCounter = new PerformanceCounter("Memory", "Available MBytes"))
                {
                   value = theMemCounter.NextValue(); 
                }
                
                result = Math.Round(Convert.ToDouble(value), 0);

                return result;
            }
            catch (Exception ex) { throw new Exception("GetAvailableMemory() Exception: " + ex.Message); }
        }


        private string GetAllDrives()
        {
            string result = "";
            try
            {                
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if ((drive.DriveType == DriveType.CDRom) || (drive.DriveType == DriveType.Removable)) { continue; } //Если это cdrom скипаем итерацию
                    if (drive.IsReady == true)
                    {
                        result += drive.Name + ";";

                    }
                }
                
            }
            catch{}
            return result;
        }

        private double GetDriveFreeSpace(string diskname)
        {
            double result = 0;
            try
            {                
                List<DriveInfo> drives = DriveInfo.GetDrives().ToList();

                drives = drives.Where(drv => drv.DriveType != DriveType.CDRom).ToList();
                drives = drives.Where(drv => drv.DriveType != DriveType.Removable).ToList();
                drives = drives.Where(drv => drv.IsReady).ToList();
                drives = drives.Where(drv => drv.Name.ToLower().Equals(diskname.ToLower())).ToList();

                if (drives.Count == 1)
                {
                    result = Math.Round((Convert.ToDouble(drives[0].AvailableFreeSpace) / 1024 / 1024), 2);
                }
                
            }
            catch{}
            return result;
        }

        internal List<KeyValuePair<string, double>> GetAllDrivesTotalSpace()
        {

            try
            {
                List<KeyValuePair<string, double>> result = new List<KeyValuePair<string, double>>();

                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if ((drive.DriveType == DriveType.CDRom) || (drive.DriveType == DriveType.Removable)) { continue; } //Если это cdrom скипаем итерацию
                    if (drive.IsReady)
                    {
                        result.Add(new KeyValuePair<string, double>(drive.Name, Math.Round((Convert.ToDouble(drive.TotalSize) / 1024 / 1024), 2)));
                    }
                }
               
                return result;
            }
            catch (Exception ex) { throw new Exception("GetAllDrivesTotalSpace() Exception: " + ex.Message); }
        }

        internal List<KeyValuePair<string,double>> GetAllDrivesFreeSpace()
        {

            try
            {
                List<KeyValuePair<string, double>> result = new List<KeyValuePair<string, double>>();

                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if ((drive.DriveType == DriveType.CDRom) || (drive.DriveType == DriveType.Removable)) { continue; } //Если это cdrom скипаем итерацию
                    if (drive.IsReady)
                    {
                        result.Add(new KeyValuePair<string, double>(drive.Name, Math.Round((Convert.ToDouble(drive.AvailableFreeSpace) / 1024 / 1024), 2)));
                    }
                }               

                return result;
            }
            catch (Exception ex) { throw new Exception("GetAllDrivesFreeSpace() Exception: " + ex.Message); }
        }

        internal List<KeyValuePair<string, double>> GetAllDrivesLoad()
        {
            try
            {
                object locker = new object();

                List<KeyValuePair<string, double>> result = new List<KeyValuePair<string, double>>();

                List<Task> myTasks = new List<Task>();

                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if ((drive.DriveType == DriveType.CDRom) || (drive.DriveType == DriveType.Removable)) { continue; } //Если это cdrom скипаем итерацию
                    if (drive.IsReady == true)
                    {
                        myTasks.Add((Task.Factory.StartNew(() =>
                        {

                            string PerfInstanceName = drive.Name.Substring(0, 2);
                            double value = GetDrive_NowLoad_Percent(PerfInstanceName);
                            lock (locker)
                            {
                                result.Add(new KeyValuePair<string, double>(drive.Name, value));
                            }                        
                        })));

                    }

                }
                try
                {
                    Task.WaitAll(myTasks.ToArray());
                }
                catch (AggregateException agr_ex)
                {
                    if (agr_ex.InnerExceptions.Count > 0)
                    {
                        string exception_text = "";
                        foreach (Exception inner_ex in agr_ex.InnerExceptions)
                        {
                            exception_text += "\n" + inner_ex.Message;
                        }                    
                    
                        if (agr_ex.InnerExceptions.Count >= myTasks.Count)
                        { throw new Exception(exception_text); }
                        else 
                        {
                            // Если измерения DiskLoad пришли с ошибкой не у всех дисков, а только у одного (или нескольких) то ничего не делаем. Не будем спамить в лог тоже. Ну его нахер.
                            //SPM_AgentService.eventLog.WriteEntry("Error Getting Disk Load:\n\nGetAllDrivesLoad() Exception: " + exception_text, EventLogEntryType.Warning, 3006); 
                            // Если измерения DiskLoad пришли с ошибкой не у всех дисков, а только у одного (или нескольких) отправляем ошибку в ксатомный лог
                            lock (SPM_AgentService._customEventsForSend_locker)
                            {
                                SPM_AgentService.CustomEventsForSend.Add(new KeyValuePair<int, string>(3006, "Error Getting Disk Load:\n\nGetAllDrivesLoad() Exception: " + exception_text));
                            }
                        }
                    }                                        
                    
                }

                foreach (var mytask in myTasks)
                { mytask.Dispose(); }

                return result;
            }
            catch (Exception ex) { throw new Exception("GetAllDrivesLoad() Exception: " + ex.Message); }
        }

        private double GetDrive_NowLoad_Percent(string input_DiskName)
        {
            try
            {
                double result = 0;

                using (PerformanceCounter theHDDCounter = new PerformanceCounter("LogicalDisk", "% Disk Time", input_DiskName))
                {
                    DateTime startMeasure = DateTime.Now;
                    int ValuesCounter = 0;
                    float value = 0;
                    int fails_counter = 0;
                    while (DateTime.Now < startMeasure.AddSeconds(10))
                    {
                        try { value = theHDDCounter.NextValue();
                            result += Convert.ToDouble(value);
                            ValuesCounter++;

                        } catch (Exception ex) { value = 0; fails_counter++;
                            if (fails_counter >= 40) { throw new Exception("40+ fails while measure disk Load Percent Exception: " + ex.Message); }
                        }
                        
                        Thread.Sleep(200);
                        
                    }
                    result = result / ValuesCounter;
                }

                result = Math.Round(result, 0);
                if (result > 100) { result = 100; }
                return result;
            }
            catch (Exception ex) { throw new Exception("GetDrive_NowLoad_Percent(string input_DiskName) - Disk " + input_DiskName + " Exception: " + ex.Message); }
        }


        internal List<KeyValuePair<string, double>> GetAllNetworkInterfacesLoad()
        {
            try
            {
                List<KeyValuePair<string, double>> result = new List<KeyValuePair<string, double>>();

                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    object locker = new object();
                    List<Task> myTasks = new List<Task>();

                    NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                    foreach (NetworkInterface ni in interfaces)
                    {
                        if (ni.NetworkInterfaceType != NetworkInterfaceType.Loopback && ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                        {
                            myTasks.Add((Task.Factory.StartNew(() =>
                            {
                                double value = 0;
                                value = GetInterface_NowLoad_Percent(ni);
                                lock (locker)
                                {
                                    result.Add(new KeyValuePair<string, double>(ni.Name, value));
                                }                           
                        })));
                        }

                    }

                    try
                    {
                        Task.WaitAll(myTasks.ToArray());
                    }
                    catch (AggregateException agr_ex)
                    {
                        if (agr_ex.InnerExceptions.Count > 0)
                        {
                            string exception_text = "";
                            foreach (Exception inner_ex in agr_ex.InnerExceptions)
                            {
                                exception_text += "\n" + inner_ex.Message;
                            }

                            if (agr_ex.InnerExceptions.Count >= myTasks.Count)
                            { throw new Exception(exception_text); }
                            else 
                            {
                                // Если измерения NetworkLoad пришли с ошибкой не у всех адаптеров, а только у одного (или нескольких) то ничего не делаем. Не будем спамить в лог тоже. Ну его нахер.
                                //SPM_AgentService.eventLog.WriteEntry("Error Getting Network Load:\n\GetAllNetworkInterfacesLoad() Exception: " + exception_text, EventLogEntryType.Warning, 3007); 

                                // Если измерения NetworkLoad пришли с ошибкой не у всех адаптеров, а только у одного (или нескольких) отправляем ошибку в ксатомный лог
                                lock (SPM_AgentService._customEventsForSend_locker)
                                {
                                    SPM_AgentService.CustomEventsForSend.Add(new KeyValuePair<int, string>(3007, "Error Getting Network Load:\n\nGetAllNetworkInterfacesLoad() Exception: " + exception_text));
                                }
                            }
                        }

                    }


                    foreach (var mytask in myTasks)
                    { mytask.Dispose(); }
                }

                return result;
            }
            catch (Exception ex) { throw new Exception("GetAllNetworkInterfacesLoad() Exception: " + ex.Message); }
        }



        private int GetInterface_NowLoad_Percent(NetworkInterface input_interface)
        {
            try
            {
                int result = 0;

                long[,] SentAndRecieve_Measurements = new long[2, 2];

                DateTime startMeasure = DateTime.Now;
                int ValuesCounter = 0;
                double SentDiff = 0;
                double RecieveDiff = 0;
                int fails_counter = 0;
                while (DateTime.Now < startMeasure.AddSeconds(10))
                {
                    try
                    {
                        SentAndRecieve_Measurements[0, 0] = input_interface.GetIPv4Statistics().BytesSent;
                        SentAndRecieve_Measurements[0, 1] = input_interface.GetIPv4Statistics().BytesReceived;

                        Thread.Sleep(1000);

                        SentAndRecieve_Measurements[1, 0] = input_interface.GetIPv4Statistics().BytesSent;
                        SentAndRecieve_Measurements[1, 1] = input_interface.GetIPv4Statistics().BytesReceived;



                        SentDiff += SentAndRecieve_Measurements[1, 0] - SentAndRecieve_Measurements[0, 0];
                        RecieveDiff += SentAndRecieve_Measurements[1, 1] - SentAndRecieve_Measurements[0, 1];

                        ValuesCounter++;                        
                    }
                    catch(Exception ex) { fails_counter++;                        
                        SentAndRecieve_Measurements[0, 0] = 0;
                        SentAndRecieve_Measurements[0, 1] = 0;
                        SentAndRecieve_Measurements[1, 0] = 0;
                        SentAndRecieve_Measurements[1, 1] = 0;

                        if (fails_counter >= 7) { throw new Exception("7+ fails while measure Network adapter Load Percent Exception: " + ex.Message); }
                    }
                }
               
                SentDiff = SentDiff / ValuesCounter;
                RecieveDiff = RecieveDiff / ValuesCounter;

                double DataCount = 0;
                if (RecieveDiff >= SentDiff) { DataCount = RecieveDiff; } else { DataCount = SentDiff; }

                long MAXsped = input_interface.Speed / 8;

                double percent = Math.Round((DataCount / MAXsped * 100), 0);

                result = Convert.ToInt32(percent);

                if (result > 100) { result = 100; }

                return result;
            }
            catch (Exception ex) { throw new Exception("GetInterface_NowLoad_Percent(NetworkInterface input_interface) - Network adapter " + input_interface.Name + " Exception: " + ex.Message); }
        }


        EventLog_Provider eventlog_provider = new EventLog_Provider(5);
        internal DateTime Get_Last_Restarted_Time()
        {
            try
            {
                return eventlog_provider.GetLastRestartedTime();
            }
            catch (Exception ex) { throw new Exception("Get_Last_Restarted_Time() Exception: " + ex.Message); }
        }

        internal DateTime Get_Last_SystemShutingDown_Time()
        {
            try
            {
                return eventlog_provider.GetLastShutingdownTime();
            }
            catch (Exception ex) { throw new Exception("Get_Last_SystemShutingDown_Time() Exception: " + ex.Message); }
        }

        internal DateTime Get_Last_ShutdownByUser_Time()
        {
            try
            {
                return eventlog_provider.GetLastShutdownByUserTime();
            }
            catch (Exception ex) { throw new Exception("Get_Last_ShutdownByUser_Time() Exception: " + ex.Message); }
        }

        internal DateTime Get_Last_ResetByUser_Time()
        {
            try
            {
                return eventlog_provider.GetLastResetByUserTime();
            }
            catch (Exception ex) { throw new Exception("Get_Last_ResetByUser_Time() Exception: " + ex.Message); }
        }


        internal List<EventLogEvent> Get_LastSystemCriticalEvents()
        {
            try
            {
                return eventlog_provider.GetLastSystemCriticalEvents();
            }
            catch (Exception ex) { throw new Exception("Get_LastSystemCriticalEvents() Exception: " + ex.Message); }
        }

        internal List<EventLogEvent> Get_LastSystemErrorEvents()
        {
            try
            {
                return eventlog_provider.GetLastSystemErrorEvents();
            }
            catch (Exception ex) { throw new Exception("Get_LastSystemErrorEvents() Exception: " + ex.Message); }
        }
    }
}
