using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.InteropServices;
using System.Security;

namespace DCTR_CliestConsole
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
        public static List<string> ListFile { get; set; }

        static ListIsoStorageFile()
        {
            ListFile = new List<string>();
        }

        public static void Add(string fileName)
        {
            ListFile.Add(fileName);
        }

        public static string GetLast()
        {
            return ListFile.Last();
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

            ListIsoStorageFile.Add(FileName + "-" + unixTime.ToString() + ".json");

            IsolatedStorageFile machine = IsolatedStorageFile.GetMachineStoreForAssembly();
            IsolatedStorageFileStream stream = new IsolatedStorageFileStream(ListIsoStorageFile.GetLast(), FileMode.Create, machine);

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
}
