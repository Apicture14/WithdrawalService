using System.Timers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Windows;
using System;
using System.Runtime.CompilerServices;

namespace TestService
{
    public partial class Service1 : ServiceBase
    {
        public Timer timer = new Timer();
        public Timer btimer = new Timer();
        public Random rand = new Random();
        public string FolderPath = "C:\\yService\\logs";
        public string ChromeFilePath = "C:\\Users\\ANYUSER\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Preferences";
        public FileStream logFile = new FileStream($"C:\\yService\\logs\\log {DateTime.Now.ToString("yyyyMMddHHmmss")}.txt", FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);
        public List<string> targets = new List<string>() {"chrome"};
        public string cpf = Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
        public bool radintv = false;
        public int maxInterval = 60000;
        public int minInterval = 5000;
        public List<Tuple<int, int>> timeSpans = new List<Tuple<int, int>>();
        public int times = 0;
        public Dictionary<string, int> result = new Dictionary<string, int>();
        private byte ControlKey = 0x6A;
        private string ConntrolName = "WITHDRAWALER";
        
        public enum States
        {
            RUNNING,
            STOPPING,
            RESTARTING
        }

        public States state = States.RUNNING; 
        
        public Service1()
        {   
            InitializeComponent();
            
            this.timeSpans.Add(new Tuple<int, int>(000000,235959));

            this.CanShutdown = true;
            this.CanHandlePowerEvent = true;
            this.CanStop = true;
            
            //Log(ChromeFilePath);
            Log(cpf + "\\cfg.txt","C");
            
            LoadConfig();
            
            
            //logFile = new FileStream($"C:\\yService\\logs\\log {DateTime.Now.ToString("yyyyMMddHHmmss")}.txt", FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);
            timer.Interval = minInterval;
            timer.AutoReset = true; 
            timer.Elapsed += new ElapsedEventHandler(run);

            btimer.Interval = 1000;
            btimer.AutoReset = true;
            btimer.Elapsed += new ElapsedEventHandler(listen);
        }

        private void Log(string text,string level="I")
        {
            string msg = $"[{DateTime.Now.ToString()}] <{level}> {text}\r\n";
            logFile.Write(Encoding.UTF8.GetBytes(msg),0, Encoding.UTF8.GetBytes(msg).Length);
            logFile.Flush();
        }

        public void LoadConfig()
        {
            if (File.Exists(cpf + "\\cfg.txt"))
            {
                Log("Config Found!","C");
                FileStream fs = new FileStream(cpf + "\\cfg.txt",FileMode.Open,FileAccess.Read,FileShare.Read);
                Utils.ConfigObj o = Utils.Extract(new StreamReader(fs).ReadToEnd());
                fs.Close();
                if (o.Vaild)
                {
                    this.FolderPath = o.LogPath;
                    this.minInterval = o.minInterval;
                    this.timer.Interval = this.minInterval;
                    if (o.maxInterval != -1)
                    {
                        this.maxInterval = o.maxInterval;
                        radintv = true;
                    }
                    this.targets = o.ProcessNames;
                    this.timeSpans = o.timeSpans;
                    this.ChromeFilePath = this.ChromeFilePath.Replace("ANYUSER", o.UserName);
                    Log($"Vaild Config Loaded Against {o.ProcessNames.Count} In {o.timeSpans.Count} timeSpans For {o.UserName}","C");
                    StringBuilder sb = new StringBuilder();
                    sb.Append("Showing TimeSpans:\r\n");
                    for (int j=0;j<o.timeSpans.Count;j++)
                    {
                        Tuple<int, int> time = o.timeSpans[j];
                        sb.Append($"TimeSpan({j+1}) From {time.Item1} To {time.Item2};\r\n");
                    }
                    Log(sb.ToString(),"C");
                    Log($"Anticipated Chrome File Path {ChromeFilePath}","C");
                    if (radintv)
                    {
                        Log($"Random Interval Enabled From {this.minInterval} to {this.maxInterval}","C");
                    }
                    else
                    {
                        Log($"Random Interval Disabled, now Value is {this.minInterval}","C");
                    }
                }
                else
                {
                    Log("Config invalid,abandoned","C");
                }
            }
            else
            {
                Log("No Config","C");
            }
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);
            timer.Start();
            btimer.Start();
        }

        protected override void OnStop()
        {
            if (!CanStop)
            {
                Log("Stop Requested,Denying");
            }
            else
            {
                Log("No Flag,Stopping.....");
                logFile.Close(); 
                btimer.Stop();
                timer.Stop(); 
                base.OnStop();
            }
        }

        protected override void OnShutdown()
        {
            Log("System Shutting down!","W");
            timer.Stop();
            btimer.Stop();
            logFile.Close();
            base.OnShutdown();
        }

        public void listen(object s, ElapsedEventArgs e)
        {
            //Log("","C");
            if (File.Exists("C:\\yService\\controls\\RELOAD.ctr"))
            {
                Log("Signal found, checking","C");
                if (Utils.decode("C:\\yService\\Controls\\RELOAD.ctr", ControlKey) != this.ConntrolName)
                {
                    //File.Copy("C:\\yService\\Controls\\RELOAD.ctr","C:\\yService\\Controls\\RELOAD.ctrx");
                    File.Delete("C:\\yService\\Controls\\RELOAD.ctr");
                    Log("Wrong signal,Deleted","C");
                }
                else
                {
                    timer.Stop();
                    timer.Interval = minInterval;
                    Log("Reload Signal Verified, Reloading","C");
                    LoadConfig();
                    File.Delete("C:\\yService\\controls\\RELOAD.ctr");
                    timer.Start();
                }
                
            }
        }

        public void run(object s,ElapsedEventArgs e)
        {
            timer.Stop();
            bool Intime = false;
            bool Found = false;
            bool Killed = false;
            int routine = -1;
            int now = Convert.ToInt32(DateTime.Now.ToString("HHmmss"));
            for (int i = 0;i < timeSpans.Count;i++)
            {
                Tuple<int, int> timeSpan = timeSpans[i];
                if (now > timeSpan.Item1 && now < timeSpan.Item2)
                {
                    Intime = true;
                    routine = i;
                }
                else
                {
                    Intime = false;
                }
            }

            if (Intime)
            {
                times++;
                List<Process> a = Process.GetProcesses().Where(i => targets.Contains(i.ProcessName)).ToList();
                if (a.Count != 0)
                {
                    Found = true;
                }

                foreach (var v in a)
                {
                    try
                    {
                        Log($"Killing {v.ProcessName} pid:{v.Id}");
                        string n = v.ProcessName;
                        v.Kill();
                        v.WaitForExit(1000);
                        if (result.Keys.Contains(v.ProcessName))
                        {
                            result[v.ProcessName]++;
                        }
                        else
                        {
                            result[v.ProcessName] = 1;
                        }

                        Log($"Killed {v.ProcessName} pid:{v.Id} time:{result[v.ProcessName]}");
                        Killed = true;

                        if (n == "chrome")
                        {
                            if (File.Exists(ChromeFilePath))
                            {
                                using (FileStream fs = new FileStream(ChromeFilePath, FileMode.Open,
                                           FileAccess.ReadWrite))
                                {
                                    StreamReader sr = new StreamReader(fs);
                                    string f = sr.ReadToEnd();
                                    fs.SetLength(0);
                                    fs.Seek(0, SeekOrigin.Begin);
                                    fs.Write(Encoding.UTF8.GetBytes(f.Replace("Crashed", "Normal")), 0,
                                        Encoding.UTF8.GetBytes(f.Replace("Crashed", "Normal")).Length);
                                    fs.Close();
                                    Log("Chrome Anti-Recov Finished");
                                }
                            }
                            else
                            {
                                Log("Chrome File Not Found!");
                            }
                        }
                    }
                    catch (Win32Exception ex)
                    {
                        Log("WinError: " + ex.Message,"E");
                    }
                    catch (InvalidOperationException ex)
                    {
                        Log("OperationError: " + ex.Message,"E");
                    }

                }

                prepareNext:
                if (radintv)
                {
                    timer.Interval = rand.Next(minInterval, maxInterval);
                }
                Log($"Working Loop:{times} F:{Found} K:{Killed} {timer.Interval}ms Before Next");
            }
            else
            {
                Log($"Idling Waiting for next schedule From {timeSpans[routine+1].Item1} To {timeSpans[routine+1].Item2}");
            }
            timer.Start();
        }
    }
}
