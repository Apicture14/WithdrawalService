using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YamlDotNet.Core.Events;

namespace TestService
{
    public class Utils
    {
        public string LogPath = @"C:/yService/logs";

        public struct ConfigObj
        {
            public List<string> ProcessNames;
            public string LogPath;
            public bool Vaild;
            public int startTime;
            public int endTime;
            public int minInterval;
            public int maxInterval;
            public string UserName;
            public List<Tuple<int,int>> timeSpans;
        }

        //public ConfigObj defaultCfg = new ConfigObj();
        public static ConfigObj Extract(string Content)
        {
            try
            {
                ConfigObj obj = new ConfigObj();
                string[] a = Content.Replace("\r\n", "\n").Split('\n');
                if (a[0] == "YSERVICE CONFIG")
                {
                    obj.ProcessNames = a[1].Split(',').ToList();
                    obj.UserName = a[2];
                    obj.minInterval = Convert.ToInt32(a[3]);
                    obj.maxInterval = Convert.ToInt32(a[4]);
                    List<Tuple<int, int>> timespans = new List<Tuple<int, int>>();

                    foreach (var timespan in a[5].Split('|'))
                    {
                        string[] stimespan = timespan.Split(',');
                        timespans.Add(new Tuple<int, int>(Convert.ToInt32(stimespan[0]),Convert.ToInt32(stimespan[1])));
                    }

                    obj.timeSpans = timespans;




                    obj.Vaild = true;
                }
                else
                {
                    obj.Vaild = false;
                    return obj;
                }
                return obj;
            }
            catch (Exception ex)
            {
                return new ConfigObj() { Vaild = false };
            }
        }

        public static string decode(string path, byte key)
        {
            if (File.Exists(path))
            using (FileStream fs = new FileStream(path,FileMode.Open,FileAccess.Read))
            {
                byte[] bs = new byte[fs.Length];
                
                fs.Read(bs, 0, (int)fs.Length);
                for (int i = 0; i < fs.Length; i++)
                {
                    bs[i] = (byte)(bs[i] ^ key);
                }

                return Encoding.UTF8.GetString(bs);
            }

            return "";
        }
    }
}