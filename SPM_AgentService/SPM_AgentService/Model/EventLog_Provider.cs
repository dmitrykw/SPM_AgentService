using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPM_AgentService
{
    class EventLog_Provider
    {
        private int Catchlog_LastPeriod;

        public EventLog_Provider(int catchlog_lastperiod_minutes)
        {
            Catchlog_LastPeriod = catchlog_lastperiod_minutes;
        }


        #region Public Methods
        public DateTime GetLastRestartedTime()
        {
            try
            {
                DateTime result = DateTime.MinValue;
                List<EventRecord> restarted_events = GetLastSysLogEvents(12).Where(x => x.ProviderName.Contains("Kernel-General")).OrderBy(x => x.TimeCreated).ToList();
                if (restarted_events.Count > 0)
                {
                    result = restarted_events.LastOrDefault().TimeCreated.GetValueOrDefault();
                }
                return result;
            }
            catch (Exception ex) { throw new Exception("GetLastRestartedTime() Exception: " + ex.Message); }
        }

        public DateTime GetLastShutingdownTime()
        {
            try
            {
                DateTime result = DateTime.MinValue;
                List<EventRecord> restarted_events = GetLastSysLogEvents(13).Where(x => x.ProviderName.Contains("Kernel-General")).OrderBy(x => x.TimeCreated).ToList();
                if (restarted_events.Count > 0)
                {
                    result = restarted_events.LastOrDefault().TimeCreated.GetValueOrDefault();
                }
                return result;
            }
            catch (Exception ex) { throw new Exception("GetLastShutingdownTime() Exception: " + ex.Message); }
        }

        public DateTime GetLastShutdownByUserTime()
        {
            try
            {
                DateTime result = DateTime.MinValue;
                List<EventRecord> restarted_events = GetLastSysLogEvents(1074).Where(x => x.ProviderName.Contains("User32")).Where(x => x.ToXml().Contains("power off")).OrderBy(x => x.TimeCreated).ToList();
                if (restarted_events.Count > 0)
                {
                    result = restarted_events.LastOrDefault().TimeCreated.GetValueOrDefault();
                }
                return result;
            }
            catch (Exception ex) { throw new Exception("GetLastShutdownByUserTime() Exception: " + ex.Message); }
        }

        public DateTime GetLastResetByUserTime()
        {
            try
            {
                DateTime result = DateTime.MinValue;
                List<EventRecord> restarted_events = GetLastSysLogEvents(1074).Where(x => x.ProviderName.Contains("User32")).Where(x => x.ToXml().Contains("restart")).OrderBy(x => x.TimeCreated).ToList();
                if (restarted_events.Count > 0)
                {
                    result = restarted_events.LastOrDefault().TimeCreated.GetValueOrDefault();
                }
                return result;
            }
            catch (Exception ex) { throw new Exception("GetLastResetByUserTime() Exception: " + ex.Message); }
        }

        public List<EventLogEvent> GetLastSystemCriticalEvents() //Get Critical-Level Events
        {
            try
            {
                List<EventLogEvent> result = new List<EventLogEvent>();
                foreach (EventRecord eventrecord in GetLastSystemEvents(1))
                {
                    result.Add(new EventLogEvent(eventrecord.TimeCreated.HasValue ? eventrecord.TimeCreated.Value : DateTime.MinValue, eventrecord.FormatDescription(), eventrecord.LogName, eventrecord.LevelDisplayName, eventrecord.TaskDisplayName, eventrecord.ProviderName, eventrecord.OpcodeDisplayName, eventrecord.Id));
                }                
                return result;
                
            }
            catch (Exception ex) { throw new Exception("GetLastSystemCriticalEvents() Exception: " + ex.Message); }
        }

        public List<EventLogEvent> GetLastSystemErrorEvents()  //Get Error-Level Events
        {
            try
            {
                List<EventLogEvent> result = new List<EventLogEvent>();
                foreach (EventRecord eventrecord in GetLastSystemEvents(2))
                {
                    result.Add(new EventLogEvent(eventrecord.TimeCreated.HasValue ? eventrecord.TimeCreated.Value : DateTime.MinValue, eventrecord.FormatDescription(), eventrecord.LogName, eventrecord.LevelDisplayName, eventrecord.TaskDisplayName, eventrecord.ProviderName, eventrecord.OpcodeDisplayName, eventrecord.Id));
                }                
                return result;                
            }
            catch (Exception ex) { throw new Exception("GetLastSystemErrorEvents() Exception: " + ex.Message); }
        }
        #endregion





        #region Private Methods
        private List<EventRecord> GetLastSystemEvents(int level)
        {
            try
            {
                List<EventRecord> result = new List<EventRecord>();
                string query = "*[System/Level=" + level + "]";
                EventLogQuery elq = new EventLogQuery("System", PathType.LogName, query);
                EventLogReader elr = new EventLogReader(elq);
                EventRecord entry;

                while ((entry = elr.ReadEvent()) != null)
                {
                    if (entry.TimeCreated.HasValue && entry.TimeCreated.Value.AddMinutes(Catchlog_LastPeriod) > DateTime.Now)
                    {
                        result.Add(entry);
                    }
                    if (result.Count > 50)
                    {
                        break;
                    }
                }                
                return result;
            }
            catch (Exception ex) { throw new Exception("GetLastSystemEvents(int level) Exception: " + ex.Message); }
        }






        private List<EventRecord> GetLastSysLogEvents(int Event_Category_ID)
        {
            try
            {
                List<EventRecord> result = new List<EventRecord>();
                string query = "*[System/EventID=" + Event_Category_ID.ToString() + "]";
                EventLogQuery elq = new EventLogQuery("System", PathType.LogName, query);
                EventLogReader elr = new EventLogReader(elq);
                EventRecord entry;

                while ((entry = elr.ReadEvent()) != null)
                {
                    if (entry.TimeCreated.HasValue && entry.TimeCreated.Value.AddMinutes(Catchlog_LastPeriod) > DateTime.Now)
                    {
                        result.Add(entry);
                    }
                    if (result.Count > 30)
                    {
                        break;
                    }
                }                
                return result;
            }
            catch (Exception ex) { throw new Exception("GetLastSysLogEvents(int Event_Category_ID) Exception: " + ex.Message); }
        }

        #endregion
    }
}
