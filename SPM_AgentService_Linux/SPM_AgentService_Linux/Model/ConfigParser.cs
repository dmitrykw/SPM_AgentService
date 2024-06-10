using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SPM_AgentService_Linux
{
    class ConfigParser
    {
        private string configfilepath;
        public ConfigParser(string ConfigFilePath)
        {
            configfilepath = ConfigFilePath;
        }

        public List<KeyValuePair<string, object>> GetOptionsList()
        {
            List<KeyValuePair<string, object>> result = new List<KeyValuePair<string, object>>();


            if (File.Exists(configfilepath))
            {
                using (StreamReader sr = new StreamReader(configfilepath, System.Text.Encoding.UTF8))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        line = line.Trim();
                        if (line.Length > 0 && line[0] != '#' && line.Contains("="))
                        {
                            string[] splitted = line.Split("=");
                            if (splitted.Length == 2)
                            {
                                result.Add(new KeyValuePair<string, object>(splitted[0].Trim().ToLower(), splitted[1].Trim()));
                            }
                        }                        
                    }
                }
            }

            return result;
        }

        
    }
}
