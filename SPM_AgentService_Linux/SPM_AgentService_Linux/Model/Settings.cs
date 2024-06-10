using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace SPM_AgentService_Linux
{
    class Settings
    {
        private int listen_port = 4538;
        private string encryption_key = "default_value";
        public Settings()
        {
            ConfigParser configParser = new ConfigParser("/etc/spm-agent.conf");
            List<KeyValuePair<string,object>> optionsList = configParser.GetOptionsList();

            int listen_port = 0;
            if (optionsList.Where(x => x.Key.ToLower() == "listen_port").Count() > 0)
            {
                int.TryParse((string)optionsList.Where(x => x.Key.ToLower() == "listen_port").FirstOrDefault().Value, out listen_port);
            }
            else { Console.WriteLine("Listen Port Option has not been found in /etc/spm-agent.conf. Using default value: " + Listen_Port); }
            

            string encryption_key = "";
            if (optionsList.Where(x => x.Key.ToLower() == "encryption_key").Count() > 0)
            {
                encryption_key = (string)optionsList.Where(x => x.Key.ToLower() == "encryption_key").FirstOrDefault().Value;
            }
            else { Console.WriteLine("Encryption Key Option has not been found in /etc/spm-agent.conf. Using default value: " + Encryption_Key); }

            if (listen_port != 0) { Listen_Port = listen_port; }
            if (encryption_key != "") { Encryption_Key = encryption_key; }
        }

        public int Listen_Port { get { return listen_port; } private set { listen_port = value; } }
        public string Encryption_Key { get { return encryption_key; } private set { encryption_key = value; } }
    }
}
