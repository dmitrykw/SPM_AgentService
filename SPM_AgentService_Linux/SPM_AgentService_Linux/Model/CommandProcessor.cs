using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SPM_AgentService_Linux
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
                    result = Worker.CPULoad.ToString();
                    break;
                case "gettotalmemory":
                    result = Worker.AllMem.ToString();
                    break;
                case "getavailablememory":
                    result = Worker.FreeMem.ToString();
                    break;
                case "getalldrives":

                    foreach (var disk in Worker.DisksTotalSpaces)
                    {
                        result += disk.Key + ";";
                    }
                    break;
                case "getdrivetotalspace":

                    if (argument != "")
                    {
                        var FoundDrives = Worker.DisksTotalSpaces.Where(drv => drv.Key.ToLower().Equals(argument.ToLower())).ToList();

                        if (FoundDrives.Count == 1)
                        {
                            result = FoundDrives[0].Value.ToString();
                        }
                    }
                    break;
                case "getdrivefreespace":

                    if (argument != "")
                    {
                        var FoundDrives = Worker.DisksFreeSpaces.Where(drv => drv.Key.ToLower().Equals(argument.ToLower())).ToList();

                        if (FoundDrives.Count == 1)
                        {
                            result = FoundDrives[0].Value.ToString();
                        }
                    }
                    break;
                case "getdriveload":

                    if (argument != "")
                    {
                        var FoundDisks = Worker.DisksLoad.Where(drv => drv.Key.ToLower().Equals(argument.ToLower())).ToList();

                        if (FoundDisks.Count == 1)
                        {
                            result = FoundDisks[0].Value.ToString();
                        }
                    }
                    break;
                case "getallnetworkadapters":

                    foreach (var adapter in Worker.NetworkInterfacesLoad)
                    {
                        result += adapter.Key + ";";
                    }
                    break;
                case "getnetworkadapterload":
                    if (argument != "")
                    {
                        var FoundAdapters = Worker.NetworkInterfacesLoad.Where(adapter => adapter.Key.ToLower().Equals(argument.ToLower())).ToList();

                        if (FoundAdapters.Count == 1)
                        {
                            result = FoundAdapters[0].Value.ToString();
                        }
                    }
                    break;
                case "getlastrestartedtime":
                    result = Worker.Last_Restarted_Time.ToString();
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
                returnobj.AgentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + "*nix";
                returnobj.CapturedDateTime = Worker.CapturedDateTime;
                returnobj.CPULoad = Worker.CPULoad;
                returnobj.AllMem = Worker.AllMem;
                returnobj.FreeMem = Worker.FreeMem;
                returnobj.DisksTotalSpaces = Worker.DisksTotalSpaces;
                returnobj.DisksFreeSpaces = Worker.DisksFreeSpaces;
                returnobj.DisksLoad = Worker.DisksLoad;
                returnobj.NetworkInterfacesLoad = Worker.NetworkInterfacesLoad;

                returnobj.Last_Restarted_Time = Worker.Last_Restarted_Time;

                return JsonConvert.SerializeObject(returnobj);
            }
            catch
            {
                return "error_when_serialize_AllDataObject";
            }
        }

        private double GetCPULoadMetric()
        {
            try
            {
                double result = 0;

                var output = "";

                //top - bn1 | grep "Cpu(s)" | sed "s/.*, *\([0-9.]*\)%* id.*/\1/" | awk '{print 100 - $1"%"}'
                //string command = @"top -bn1|grep 'Cpu(s)'|sed 's/.*, *\([0-9.]*\)%* id.*/\1/'|awk '{print 100 - $1}'";
                //string command = "iostat -c 1 2 | sed '/^\s*$/d' | tail -n 1 | awk '{usage=100-$NF} END {printf("%5.2f", usage)}' | sed 's/,/./'";
                string command = @"iostat -c 1 2 | sed '/^\s*$/d' | tail -n 1 | awk '{print $NF}'";
                var info = new ProcessStartInfo();
                info.FileName = "/bin/bash";
                info.Arguments = "-c \"" + command + "\"";
                info.RedirectStandardOutput = true;

                using (var process = Process.Start(info))
                {
                    output = process.StandardOutput.ReadToEnd();
                    //Console.WriteLine(output);
                }
                //output = output.Replace(",", ".");
                double IdleTime = double.Parse(output.Trim());
                result = 100 - IdleTime;
                result = Math.Round(result, 2);
                if (result < 0) { result = 0; }
                else if (result > 100) { result = 100; }
                return result;
            }
            catch (Exception ex) { throw new Exception("GetCPULoadMetric() Exception: " + ex.Message); }
        }

        internal double GetCPULoad()
        {
            try
            {
                double result = 0;
                
                DateTime startMeasure = DateTime.Now;
                int ValuesCounter = 0;
                double value = 0;

                int fails_counter = 0;
                while (DateTime.Now < startMeasure.AddSeconds(10))
                {
                    try
                    {
                        value = GetCPULoadMetric();
                        result += value;
                        ValuesCounter++;
                    }
                    catch (Exception ex)
                    {
                        value = 0; fails_counter++;
                        if (fails_counter >= 40) { throw new Exception("40+ fails while measure CPU Load Process Exception: " + ex.Message); }
                    }

                    Thread.Sleep(200);
                }
                result = Math.Round(result / ValuesCounter, 2);

                return result;                
            }
            catch (Exception ex) { throw new Exception("GetCPULoad() Exception: " + ex.Message); }
        }

        private double[] GetMemoryMetrics()
        {
            try
            {
                double[] result = new double[3];

                var output = "";

                var info = new ProcessStartInfo();
                string command = "free -m";
                info.FileName = "/bin/bash";
                info.Arguments = "-c \"" + command + "\"";
                info.RedirectStandardOutput = true;

                using (var process = Process.Start(info))
                {
                    output = process.StandardOutput.ReadToEnd();
                    //Console.WriteLine(output);
                }

                var lines = output.Split("\n");
                var memory = lines[1].Split(" ", StringSplitOptions.RemoveEmptyEntries);

                result[0] = double.Parse(memory[1]); //.Replace(",", ".")); //Total
                result[1] = double.Parse(memory[2]); //.Replace(",", ".")); //Used
                result[2] = double.Parse(memory[6]); //.Replace(",", ".")); //Free

                return result;
            }
            catch (Exception ex) { throw new Exception("GetMemoryMetrics() Exception: " + ex.Message); }
        }

        internal double GetAllMemorySize()
        {
            try
            {
                double result = 0;                                
                double value = GetMemoryMetrics()[0]; //Total


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
                double value = GetMemoryMetrics()[2]; //Free
              
                result = Math.Round(value, 0);
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
            catch { }
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
            catch { }
            return result;
        }

        private List<string[]> GetAllDrivesMetrics()
        {
            try
            {
                List<string[]> result = new List<string[]>();
                var output = "";

                //var info = new ProcessStartInfo("df -k | grep 'dev' | awk '{print $1,$2,$3,$4,$6}'");
                var info = new ProcessStartInfo();
                string command = "df -k | grep -vE '^Filesystem|tmpfs|cdrom|mmcblk0p1|loop|udev' | awk '{print $1,$2,$3,$4,$6}'";
                info.FileName = "/bin/bash";                
                info.Arguments = "-c \"" + command + "\"";
                info.RedirectStandardOutput = true;

                using (var process = Process.Start(info))
                {
                    output = process.StandardOutput.ReadToEnd();
                }

                var lines = output.Split("\n");
                if (lines.Count() > 0)
                {
                    foreach (var line in lines)
                    {
                        string[] lineElements = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                        if (lineElements.Count() > 4)
                        {
                            string driveName = lineElements[0];                            
                            if (driveName.Contains("dev/"))
                            {
                                string driveTotalSpace = lineElements[1]; //.Replace(",", ".");
                                string driveUsedSpace = lineElements[2]; //.Replace(",", ".");
                                string driveFreeSpace = lineElements[3]; //.Replace(",", ".");
                                string driveMountPoint = lineElements[4]; //.Replace(",", ".");

                                result.Add(new string[5] { driveName, driveTotalSpace, driveUsedSpace, driveFreeSpace, driveMountPoint });
                            }
                        }
                    }
                }
                return result;
            }
            catch (Exception ex) { throw new Exception("GetAllDrivesMetrics() Exception: " + ex.Message); }
        }

        internal List<KeyValuePair<string, double>> GetAllDrivesTotalSpace()
        {

            try
            {

                List<KeyValuePair<string, double>> result = new List<KeyValuePair<string, double>>();

                List<string[]> DrivesData = GetAllDrivesMetrics();
                if (DrivesData.Count > 0)
                {   foreach(string[] drive in DrivesData)
                    {
                        if (drive.Length > 4)
                        {
                            string driveName = drive[0];
                            string driveTotalSpace = drive[1];
                            //string driveUsedSpace = drive[2];
                            //string driveFreeSpace = drive[3];
                            string driveMountPoint = drive[4];
                            
                            result.Add(new KeyValuePair<string, double>(driveName + " ( Mounted on: " + driveMountPoint + " )", Math.Round((Convert.ToDouble(driveTotalSpace) / 1024), 2)));                            
                        }
                    }
                            
                }

                return result;
            }
            catch (Exception ex) { throw new Exception("GetAllDrivesTotalSpace() Exception: " + ex.Message); }
        }

        internal List<KeyValuePair<string, double>> GetAllDrivesFreeSpace()
        {
            try
            {                
                List<KeyValuePair<string, double>> result = new List<KeyValuePair<string, double>>();

                List<string[]> DrivesData = GetAllDrivesMetrics();
                if (DrivesData.Count > 0)
                {
                    foreach (string[] drive in DrivesData)
                    {
                        if (drive.Length > 4)
                        {
                            string driveName = drive[0];
                            //string driveTotalSpace = drive[1];
                            //string driveUsedSpace = drive[2];
                            string driveFreeSpace = drive[3];
                            string driveMountPoint = drive[4];
                            
                            result.Add(new KeyValuePair<string, double>(driveName + " ( Mounted on: " + driveMountPoint + " )", Math.Round((Convert.ToDouble(driveFreeSpace) / 1024), 2)));
                        }
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

                List<string[]> DrivesData = GetAllDrivesMetrics();
                if (DrivesData.Count > 0)
                {
                    foreach (string[] drive in DrivesData)
                    {
                        if (drive.Length > 4)
                        {
                            string driveName = drive[0];
                            //string driveTotalSpace = drive[1];
                            //string driveUsedSpace = drive[2];
                            //string driveFreeSpace = drive[3];
                            string driveMountPoint = drive[4];

                            myTasks.Add((Task.Factory.StartNew(() =>
                            {                                
                                double value = GetDrive_NowLoad_Percent(driveName);
                                lock (locker)
                                {
                                    result.Add(new KeyValuePair<string, double>(driveName + " ( Mounted on: " + driveMountPoint + " )", value));
                                }
                            })));
                            
                        }
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
                            lock (Worker._customEventsForSend_locker)
                            {
                                Worker.CustomEventsForSend.Add(new KeyValuePair<int, string>(3006, "Error Getting Disk Load: GetAllDrivesLoad() Exception: " + exception_text));
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

                var output = "";

                string command = "iostat -d -x -N -y 10 1 " + input_DiskName + " | sed '1d' | grep -v '^$' | awk '{print $NF}' | grep -v '%util'";
                var info = new ProcessStartInfo();
                info.FileName = "/bin/bash";
                info.Arguments = "-c \"" + command + "\"";
                info.RedirectStandardOutput = true;

                using (var process = Process.Start(info))
                {
                    output = process.StandardOutput.ReadToEnd();
                }
                //output = output.Replace(",", ".");
                result = Math.Round(double.Parse(output), 0);
                if (result < 0) { result = 0; }
                else if (result > 100) { result = 100; }
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
                                lock (Worker._customEventsForSend_locker)
                                {
                                    Worker.CustomEventsForSend.Add(new KeyValuePair<int, string>(3007, "Error Getting Network Load: GetAllNetworkInterfacesLoad() Exception: " + exception_text));
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
                    catch (Exception ex)
                    {
                        fails_counter++;
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

                if (result < 0) { result = 0; }
                else if (result > 100) { result = 100; }

                return result;
            }
            catch (Exception ex) { throw new Exception("GetInterface_NowLoad_Percent(NetworkInterface input_interface) - Network adapter " + input_interface.Name + " Exception: " + ex.Message); }
        }

        UnixEventsProvider events_provider = new UnixEventsProvider(5);
        internal DateTime Get_Last_Restarted_Time()
        {
            try
            {
                return events_provider.GetLastRestartedTime();
            }
            catch (Exception ex) { throw new Exception("Get_Last_Restarted_Time() Exception: " + ex.Message); }
        }
    }
}
