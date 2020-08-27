using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.InteropServices;
using System.Security;
using System.Diagnostics;
using Newtonsoft.Json;
using Microsoft.Win32.TaskScheduler;

namespace DCTR_Cliest
{
    struct DataCollection
    {
        public string ComputerName { get; set; }
        public int ProcessorCount { get; set; }
        public ulong Memory { get; set; }
        public int MemoryFree { get; set; }
        public int UpTime { get; set; }
        public OperatingSystem OSVersion { get; set; }
        public Version Ver { get; set; }
        public Array TaskList { get; set; }
    }

    struct TaskItem
    {
        public string Name { get; set; }
        public string State { get; set; }
        public string LastTaskResult { get; set; }
        public DateTime LastRunTime { get; set; }
        public DateTime NextRunTime { get; set; }
    }

    static class ListIsoStorageFile
    {
        //public static List<string> ListFile { get; set; }
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

            FileName += unixTime.ToString() + ".json";
            ListIsoStorageFile.Add(FileName);

            IsolatedStorageFile machine = IsolatedStorageFile.GetMachineStoreForAssembly();
            IsolatedStorageFileStream stream = new IsolatedStorageFileStream(FileName, FileMode.Create, machine);

            StreamWriter str = new StreamWriter(stream);
            str.WriteLine(json);
            str.Close();
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

            // Формирование набора данных с информацией о системе
            DataCollection ThisComp = new DataCollection
            {
                ComputerName = Environment.MachineName,
                ProcessorCount = Environment.ProcessorCount,
                Memory = ram / 1024 / 1024,
                MemoryFree = (int)ramCounter.NextValue(),
                UpTime = Environment.TickCount,
                OSVersion = Environment.OSVersion,
                Ver = Environment.Version
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


            // Сохранение пакета данных о задачах
            DataFileSave TaskFile = new DataFileSave
            {
                FileName = "task",
                json = JsonConvert.SerializeObject(taskItems, Formatting.Indented)
            };
            TaskFile.Save();
        }
    }
}
