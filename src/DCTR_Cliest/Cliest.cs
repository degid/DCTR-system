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
        static public string TaskPrefix = ConfigurationSettings.AppSettings["TaskPrefix"].ToString();
        static public string strVersion = ConfigurationSettings.AppSettings["Version"].ToString();
        static public EventLog eventLog = new System.Diagnostics.EventLog();

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
            // Set up a timer that triggers every minute.
            Timer timerGetData = new Timer();
            timerGetData.Interval = IntervalSystem;
            timerGetData.Elapsed += new ElapsedEventHandler(this.GetDataOnTimer);
            timerGetData.Start();

            Timer timerSendData = new Timer();
            timerSendData.Interval = IntervalSend;
            timerSendData.Elapsed += new ElapsedEventHandler(this.SendDataOnTimer);
            timerSendData.Start();

            // Update the service state to Running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
            PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available MBytes");
        }

        protected override void OnStop()
        {
            eventLog.WriteEntry("DCTR service stopped");
        }

        public void GetDataOnTimer(object sender, ElapsedEventArgs args)
        {
            // TODO: Insert monitoring activities here.
            SavePackage n = new SavePackage();
            n.SaveAll();

            eventLog.WriteEntry("Save status data", EventLogEntryType.Information, 10);
        }

        public void SendDataOnTimer(object sender, ElapsedEventArgs args)
        {
            eventLog.WriteEntry("trying to send...", EventLogEntryType.Information, 20);
            try
            {
                new Cliest().RunSendData().Wait();
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Console.WriteLine("ERROR: " + e.Message);
                    eventLog.WriteEntry(e.Message, EventLogEntryType.Error, 2000);
                }
            }
        }

        private async System.Threading.Tasks.Task RunSendData()
        {
            //**************
            // Загрузка данных на диска
            try
            {
                var Credential = GoogleDrive.GetUserCredential();
                var service = GoogleDrive.CreateService(Credential);
                List<string> GListId = await GoogleDrive.UploadFilesAsync(service, ListIsoStorageFile.ListFile);
            }
            catch (Exception ex)
            {
                eventLog.WriteEntry(ex.Message, EventLogEntryType.Error, 200);
            }
        }
    }
}
