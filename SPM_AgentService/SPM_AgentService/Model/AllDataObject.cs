﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPM_AgentService
{[Serializable]
    class AllDataObject
    {
        public string AgentVersion { get; set; }
        public DateTime CapturedDateTime { get; set; }
        public double CPULoad { get; set; }
        public double AllMem { get; set; }
        public double FreeMem { get; set; }

        public List<KeyValuePair<string, double>> DisksTotalSpaces { get; set; }
        public List<KeyValuePair<string, double>> DisksFreeSpaces { get; set; }
        public List<KeyValuePair<string, double>> DisksLoad { get; set; }

        public List<KeyValuePair<string, double>> NetworkInterfacesLoad { get; set; }

        public DateTime Last_Restarted_Time { get; set; }
        public DateTime Last_SystemShutingDown_Time { get; set; }
        public DateTime Last_ShutdownByUser_Time { get; set; }
        public DateTime Last_ResetByUser_Time { get; set; }

        public List<EventLogEvent> LastSystemCriticalEvents { get; set; }
        public List<EventLogEvent> LastSystemErrorsEvents { get; set; }



        public AllDataObject()
        {
            AgentVersion = "nodata";

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
            LastSystemErrorsEvents = new List<EventLogEvent>();

        }
    }
}
