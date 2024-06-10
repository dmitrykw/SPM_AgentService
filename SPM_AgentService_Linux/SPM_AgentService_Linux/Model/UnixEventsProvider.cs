using System;
using System.Collections.Generic;
using System.Text;

namespace SPM_AgentService_Linux
{
    class UnixEventsProvider
    {
        private int Catchlog_LastPeriod;

        public UnixEventsProvider(int catchlog_lastperiod_minutes)
        {
            Catchlog_LastPeriod = catchlog_lastperiod_minutes;
        }

        #region Public Methods
        public DateTime GetLastRestartedTime()
        {
            try
            {
                DateTime result = DateTime.MinValue;

                DateTime systemBootDT = Get_SystemBoot_DateTime();
                if (systemBootDT.AddMinutes(Catchlog_LastPeriod) > DateTime.Now)
                {
                    result = systemBootDT;
                }
                return result;
            }
            catch (Exception ex) { throw new Exception("GetLastRestartedTime() Exception: " + ex.Message); }
        }
        #endregion


        #region Private Methods

        private DateTime Get_SystemBoot_DateTime()
        {
            DateTime capturedDT = DateTime.Now.AddMilliseconds(-Environment.TickCount64);
            int roundedSecond = (int)((Math.Floor(((double)capturedDT.Second) /10)) * 10);
            return new DateTime(capturedDT.Year,capturedDT.Month, capturedDT.Day, capturedDT.Hour, capturedDT.Minute, roundedSecond);
        }
        #endregion
    }
}
