using System;
using System.ComponentModel;
using System.Linq;
using System.ServiceProcess;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;

namespace XShortService
{
    public partial class Indexing : ServiceBase
    {
        public int command;
        public bool exit = false;
        public string dataPath = String.Empty;
        public string targetPath = String.Empty;
        public Indexing(string[] args)
        {
            InitializeComponent();
            if (args.Length > 0)
            {
                OnStart(args);
            }
            CanShutdown = true;
            CanPauseAndContinue = true;
        }

        protected override void OnPause()
        {
            base.OnPause();
            command = 1;
        }

        protected override void OnContinue()
        {
            base.OnContinue();
            command = 0;
        }

        protected override void OnShutdown()
        {
            base.OnShutdown();
            command = -1;
            exit = true;
        }

        protected override void OnStart(string[] args)
        {
            dataPath = args[0];
            targetPath = args[1];
            Directory.SetCurrentDirectory(targetPath);
            BackgroundWorker backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += BackgroundWorker_DoWork;
            backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
            backgroundWorker.RunWorkerAsync();
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if ((int)e.Result != 0)
            {
                File.Copy(dataPath + "\\temp1", dataPath + "\\folders", true);
                File.Copy(dataPath + "\\temp2", dataPath + "\\files", true);
                File.WriteAllText(dataPath + "\\index", String.Empty);
            }
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            e.Result = 1;
            if (File.Exists(dataPath + "\\index"))
            {
                if (File.GetLastWriteTime(dataPath + "\\index").Date == DateTime.Now.Date)
                {
                    e.Result = 0;
                    return;
                }
            }
            while (command != 0)
            {
                Thread.Sleep(1000);
                if (exit)
                    return;
            }
            File.WriteAllText(dataPath + "\\temp1", String.Empty);
            File.WriteAllText(dataPath + "\\temp2", String.Empty);

            SearchFileAndFolder(targetPath);//new smart search
        }

        private void SearchFileAndFolder(string dir)
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(dir);
                DirectoryInfo[] dir1 = di.GetDirectories("*" + "*.*");
                string alldir = String.Empty;
                for (int i = 0; i < dir1.Count(); i++)
                {
                    if (!dir1[i].Attributes.HasFlag(FileAttributes.Hidden) && !dir1[i].Name.StartsWith("."))
                        alldir += dir1[i].FullName + Environment.NewLine;
                }
                File.AppendAllText(dataPath + "\\temp1", alldir);

                FileInfo[] files = di.GetFiles("*" + "*.*");
                string allfiles = String.Empty;
                for (int i = 0; i < files.Count(); i++)
                {
                    if (!files[i].Attributes.HasFlag(FileAttributes.Hidden) && !files[i].Name.StartsWith("."))
                        allfiles += files[i].FullName + Environment.NewLine;
                }
                File.AppendAllText(dataPath + "\\temp2", allfiles);

                DirectoryInfo[] dirs = di.GetDirectories();
                if (dirs == null || dirs.Length < 1)
                    return;
                foreach (DirectoryInfo sdir in dirs)
                {
                    while (command != 0)
                    {
                        Thread.Sleep(1000);
                        if (exit || command == -1)
                        {
                            return;
                        }
                    }
                    if (!sdir.Attributes.HasFlag(FileAttributes.Hidden) && !sdir.Name.StartsWith("."))
                        SearchFileAndFolder(sdir.FullName);
                    else
                        continue;
                    Thread.Sleep(1500);
                }
            }
            catch
            {
                return;
            }
        }


        protected override void OnStop()
        {
            command = -1;
            exit = true;
        }

       

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);

        public enum ServiceState
        {
            SERVICE_STOPPED = 0x00000001,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDING = 0x00000003,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ServiceStatus
        {
            public int dwServiceType;
            public ServiceState dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
        };
    }
}
