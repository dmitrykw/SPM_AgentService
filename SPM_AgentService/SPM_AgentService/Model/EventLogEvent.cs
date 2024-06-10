using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPM_AgentService
{
    [Serializable]
    class EventLogEvent
    {
        public readonly DateTime TimeCreated;
        public readonly string Description;
        public readonly string LogName;
        public readonly string Level;
        public readonly string Task;
        public readonly string ProviderName;
        public readonly string OpCode;
        public readonly int EventID;



        public EventLogEvent(DateTime timecreated, string description, string logname, string level, string task, string providername, string opcode, int eventid)
        {
            this.TimeCreated = timecreated;
            this.Description = description;
            this.LogName = logname;
            this.Level = level;
            this.Task = task;
            this.ProviderName = providername;
            this.OpCode = opcode;
            this.EventID = eventid;
        }
    }
}
