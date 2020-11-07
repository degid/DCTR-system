using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.ServiceProcess;
using System.Runtime.InteropServices;
using System.Timers;

namespace DCTR_Cliest
{
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
    public partial class Cliest : ServiceBase
    {
        static public int IntervalSystem = Int32.Parse(ConfigurationSettings.AppSettings["IntervalSystem"].ToString());
        static public int IntervalSend = Int32.Parse(ConfigurationSettings.AppSettings["IntervalSend"].ToString());
        static public string Company = ConfigurationSettings.AppSettings["Company"].ToString();
        static public string CompanyPrefix = ConfigurationSettings.AppSettings["CompanyPrefix"].ToString();
        static public string TaskPrefix = ConfigurationSettings.AppSettings["TaskPrefix"].ToString();
        static public string strVersion = ConfigurationSettings.AppSettings["Version"].ToString();
        static public string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        static public EventLog eventLog = new System.Diagnostics.EventLog();
        static public System.Timers.Timer timerGetDataSys = new System.Timers.Timer();
        static public System.Timers.Timer timerSendData = new System.Timers.Timer();

        public Cliest()
        {
            InitializeComponent();
            if (!System.Diagnostics.EventLog.SourceExists("DCTR"))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                    "DCTR_Client", "DCTR");
            }
            eventLog.Source = "DCTR_Client";
            eventLog.Log = "DCTR";
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);

        protected override void OnStart(string[] args)
        {
            // Update the service state to Start Pending.
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            eventLog.WriteEntry("DCTR service started\n\n" + strVersion);

            // Set up a timers
            timerGetDataSys.Interval = IntervalSystem;
            timerGetDataSys.Elapsed += new ElapsedEventHandler(this.GetDataSysOnTimer);
            timerGetDataSys.Start();

            timerSendData.Interval = IntervalSend;
            timerSendData.Elapsed += new ElapsedEventHandler(this.SendDataOnTimer);
            timerSendData.Start();

            FirstPack();

            // Update the service state to Running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
            PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            Logger.InitLogger();
            Logger.Log.Info("Start");
        }

        protected override void OnStop()
        {
            try
            {
                timerGetDataSys.Stop();
                timerSendData.Stop();
            }
            catch (Exception ex)
            {
                eventLog.WriteEntry(ex.Message, EventLogEntryType.Error, 600);
            }

            new Cliest().RunSendData().Wait();

            eventLog.WriteEntry("DCTR service stopped");
        }

        private void FirstPack()
        {
            SavePackage pack = new SavePackage();
            pack.SaveAll();
            new Cliest().RunSendData().Wait();
        }

        public void GetDataSysOnTimer(object sender, ElapsedEventArgs args)
        {
            SavePackage pack = new SavePackage();
            pack.SaveAll();
        }

        public void SendDataOnTimer(object sender, ElapsedEventArgs args)
        {
            new Cliest().RunSendData().Wait();
        }

        private async System.Threading.Tasks.Task RunSendData()
        {
            //**************
            // Загрузка данных на диска
            try
            {
                if (ListIsoStorageFile.ListFile.Count == 0)
                {
                    throw new Exception("No files to send");
                }

                var service = GoogleDrive.CreateService(GoogleDrive.GetUserCredential());
                List<string> GListId = await GoogleDrive.UploadFilesAsync(service);

                eventLog.WriteEntry($"Files saved {GListId.Count-1} to Google.Drive\n\n" + String.Join(", ", GListId),
                    EventLogEntryType.Information, 1);
            }
            catch (Exception ex)
            {
                if (ex.Message.EndsWith("baseUri"))
                {
                    eventLog.WriteEntry($"{ex.Message}\n\nFailed to send package. The package will be sent on next shipment.", EventLogEntryType.Warning, 801);
                }
                else
                {
                    eventLog.WriteEntry(ex.Message, EventLogEntryType.Error, 800);
                }
            }
        }
    }
}
