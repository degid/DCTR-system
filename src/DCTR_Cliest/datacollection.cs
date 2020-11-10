using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.InteropServices;
using System.Security;
using System.Diagnostics;
using System.Management;
using Newtonsoft.Json;
using Microsoft.Win32.TaskScheduler;

namespace DCTR_Cliest
{
    struct disk
    {
        public string Name { get; set; }
        public string DriveFormat { get; set; }
        public DriveType DiskType { get; set; }
        public long AvailableFreeSpace { get; set; }
        public bool IsReady { get; set; }
        public long TotalFreeSpace { get; set; }
        public long TotalSize { get; set; }
        public string VolumeLabel { get; set; }
    }

    struct PerfOS
    {
        public string Name { get; set; }
        public string InterruptsPersec { get; set; }
        public string PercentIdleTime { get; set; }
        public string PercentPrivilegedTime { get; set; }
        public string PercentProcessorTime { get; set; }
        public string PercentUserTime { get; set; }
    }

    struct ProcessorInfo
    {
        public string Name { get; set; }
        public int ProcessorCount { get; set; }
        public List<PerfOS> PerfOS { get; set; }
    }

    struct SysInfo
    {
        public string ComputerName { get; set; }
        public ulong Memory { get; set; }
        public int MemoryFree { get; set; }
        public int UpTime { get; set; }
        public ProcessorInfo Processor { get; set; }
        public Version OSVersion { get; set; }
        public PlatformID Platform { get; set; }
        public string ServicePack { get; set; }
        public string VerString { get; set; }
        public List<disk> drives { get; set; }
    }

    struct TaskItem
    {
        public string Name { get; set; }
        public string State { get; set; }
        public string LastTaskResult { get; set; }
        public DateTime LastRunTime { get; set; }
        public DateTime NextRunTime { get; set; }
    }

    struct SysDataCollection
    {
        public string Company { get; set; }
        public string CompanyPrefix { get; set; }

        public SysInfo SystemInfo { get; set; }
    }

    struct TaskDataCollection
    {
        public string Company { get; set; }
        public string CompanyPrefix { get; set; }

        public List<TaskItem> TaskItems { get; set; }
    }

    static class ListIsoStorageFile
    {
        public static Queue<string> ListFile { get; set; }

        static ListIsoStorageFile()
        {
            ListFile = new Queue<string>();
        }

        public static void Add(string fileName)
        {
            ListFile.Enqueue(fileName);
        }

        public static string Get()
        {
            return ListFile.Dequeue();
        }
    }

    class DataFileSave
    {
        public string FileName { get; set; }
        public string json { get; set; }

        internal void Save()
        {
            long epochTicks = new DateTime(1970, 1, 1).Ticks;
            long unixTime = ((DateTime.UtcNow.Ticks - epochTicks) / TimeSpan.TicksPerSecond);

            FileName += $"_{unixTime}.json";

            IsolatedStorageFile machine = IsolatedStorageFile.GetMachineStoreForAssembly();
            IsolatedStorageFileStream stream = new IsolatedStorageFileStream(FileName, FileMode.Create, machine);

            StreamWriter str = new StreamWriter(stream);
            str.WriteLine(json);
            str.Close();
            ListIsoStorageFile.Add(FileName);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class MemoryStatus
    {
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatus lpBuffer);

        private uint dwLength;
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;

        private static volatile MemoryStatus singleton;
        private static readonly object syncroot = new object();

        public static MemoryStatus CreateInstance()
        {
            if (singleton == null)
                lock (syncroot)
                    if (singleton == null)
                        singleton = new MemoryStatus();
            return singleton;
        }

        [SecurityCritical]
        private MemoryStatus()
        {
            dwLength = (uint)Marshal.SizeOf(typeof(MemoryStatus));
            GlobalMemoryStatusEx(this);
        }
    }

    public class ProcessorInf
    {
        // kernel load percentage
        public static List<Dictionary<string, string>> GetPerfOS()
        {
            List<Dictionary<string, string>> perfOS = new List<Dictionary<string, string>>();

            ObjectQuery PerfRawDataQuery = new System.Management.ObjectQuery(
                 "SELECT Name, InterruptsPersec, PercentIdleTime, PercentPrivilegedTime, PercentProcessorTime, PercentUserTime " +
                 "FROM Win32_PerfFormattedData_PerfOS_Processor"
                 );
            using (ManagementObjectSearcher PerfRawData = new ManagementObjectSearcher(PerfRawDataQuery))
            {
                foreach (ManagementObject obj in PerfRawData.Get())
                {
                    perfOS.Add(
                        new Dictionary<string, string>
                        {
                            ["Name"] = obj["Name"].ToString(),
                            ["InterruptsPersec"] = obj["InterruptsPersec"].ToString(),
                            ["PercentIdleTime"] = obj["PercentIdleTime"].ToString(),
                            ["PercentPrivilegedTime"] = obj["PercentPrivilegedTime"].ToString(),
                            ["PercentProcessorTime"] = obj["PercentProcessorTime"].ToString(),
                            ["PercentUserTime"] = obj["PercentUserTime"].ToString(),
                        });
                }
            };

            return perfOS;
        }

        // Name processor
        public static string GetName()
        {
            string Proc = null;
            ObjectQuery win32ProcQuery = new System.Management.ObjectQuery("SELECT Name from Win32_Processor");
            using (ManagementObjectSearcher win32Proc = new ManagementObjectSearcher(win32ProcQuery))
            {
                var obj = win32Proc.Get().OfType<ManagementObject>().First();
                Proc = obj["Name"].ToString();
            }
            return Proc;
        }
    }


    class SavePackage
    {

        public void SaveAll()
        {
            CreateSystemInfo();
            CreateTasksInfo();
        }

        public void CreateSystemInfo()
        {
            ////////////////////////////////////////////////////
            // Данные о системе
            //

            // Получение данных о объеме памяти в системе
            PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            MemoryStatus status = MemoryStatus.CreateInstance();
            ulong ram = status.TotalPhys;

            List<disk> drives = new List<disk>();
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType == DriveType.Fixed  || drive.DriveType == DriveType.Removable)
                drives.Add(new disk()
                {
                    Name = drive.Name,
                    DriveFormat = drive.DriveFormat,
                    DiskType = drive.DriveType,
                    AvailableFreeSpace = drive.AvailableFreeSpace,
                    IsReady = drive.IsReady,
                    TotalFreeSpace = drive.TotalFreeSpace,
                    TotalSize = drive.TotalSize,
                    VolumeLabel = drive.VolumeLabel
                });
            }
            OperatingSystem os = Environment.OSVersion;

            List<Dictionary<string, string>> dicPerfOS = ProcessorInf.GetPerfOS();
            List<PerfOS> perfOS = new List<PerfOS>();
            foreach (var dic in dicPerfOS)
            {
                perfOS.Add( new PerfOS
                {
                    Name = dic["Name"],
                    InterruptsPersec = dic["InterruptsPersec"],
                    PercentIdleTime = dic["PercentIdleTime"],
                    PercentPrivilegedTime = dic["PercentPrivilegedTime"],
                    PercentProcessorTime = dic["PercentProcessorTime"],
                    PercentUserTime = dic["PercentUserTime"]
                });
            }

            ProcessorInfo procInfo = new ProcessorInfo
            {
                Name = ProcessorInf.GetName(),
                ProcessorCount = Environment.ProcessorCount,
                PerfOS = perfOS

            };

            SysInfo sysInf = new SysInfo
            {
                ComputerName = Environment.MachineName,
                Processor = procInfo,
                Memory = ram / 1024 / 1024,
                MemoryFree = (int)ramCounter.NextValue(),
                UpTime = Environment.TickCount,
                OSVersion = os.Version,
                Platform = os.Platform,
                ServicePack = os.ServicePack,
                VerString = os.VersionString,
                drives = drives
            };

            // Формирование набора данных с информацией о системе
            SysDataCollection ThisComp = new SysDataCollection
            {
                Company = Cliest.Company,
                CompanyPrefix = Cliest.CompanyPrefix,
                SystemInfo = sysInf
            };

            // Сохранение пакета данных о системе
            DataFileSave SysFile = new DataFileSave
            {
                FileName = "system",
                json = JsonConvert.SerializeObject(ThisComp, Formatting.Indented)
            };
            SysFile.Save();
        }

        public void CreateTasksInfo()
        {
            ////////////////////////////////////////////////////
            // Данные о заданиях планировщика Windows
            //
            TaskService ts = new TaskService();

            List<TaskItem> taskItems = new List<TaskItem>();
            foreach (Microsoft.Win32.TaskScheduler.Task SelestTask in ts.AllTasks.ToList())
            {
                if (SelestTask.Name.StartsWith(Cliest.TaskPrefix))
                {
                    taskItems.Add(new TaskItem()
                    {
                        Name = SelestTask.Name,
                        State = SelestTask.State.ToString(),
                        LastTaskResult = String.Format("0x{0:X}", SelestTask.LastTaskResult),
                        LastRunTime = SelestTask.LastRunTime,
                        NextRunTime = SelestTask.NextRunTime
                    });
                }
            }

            TaskDataCollection ThisComp = new TaskDataCollection
            {
                Company = Cliest.Company,
                CompanyPrefix = Cliest.CompanyPrefix,
                TaskItems = taskItems
            };

            // Сохранение пакета данных о задачах
            DataFileSave TaskFile = new DataFileSave
            {
                FileName = "task",
                json = JsonConvert.SerializeObject(ThisComp, Formatting.Indented)
            };
            TaskFile.Save();
        }
    }
}
