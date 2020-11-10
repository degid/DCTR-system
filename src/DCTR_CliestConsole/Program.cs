using System;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using Microsoft.Win32.TaskScheduler;


using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.InteropServices;
using System.Security;

namespace DCTR_CliestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Discovery API Sample");
            Console.WriteLine("====================");

            try
            {
                //new Program().Run().Wait();
                new Program().Run2();
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Console.WriteLine("ERROR: " + e.Message);
                }
            }
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private void Run2()
        {
            ObjectQuery PerfRawDataQuery = new System.Management.ObjectQuery(
                 "SELECT Name, InterruptsPersec, PercentIdleTime, PercentPrivilegedTime, PercentProcessorTime, PercentUserTime " +
                 "FROM Win32_PerfFormattedData_PerfOS_Processor"
                 );
            ObjectQuery win32ProcQuery = new System.Management.ObjectQuery("SELECT Name from Win32_Processor");

            using (ManagementObjectSearcher win32Proc = new ManagementObjectSearcher(win32ProcQuery),
                            PerfRawData = new ManagementObjectSearcher(PerfRawDataQuery))
            {
                var ob = win32Proc.Get().OfType<ManagementObject>().First();
                Console.WriteLine($"Name!!: {ob["Name"]}");

                foreach (ManagementObject obj in PerfRawData.Get())
                {
                    Console.WriteLine("-----------------------------------");
                    Console.WriteLine($"Name: {obj["Name"]}");
                    Console.WriteLine($"Прерываний/сек:  {obj["InterruptsPersec"]}");
                    Console.WriteLine($"Процент времени бездействия: {obj["PercentIdleTime"]}");
                    Console.WriteLine($"% работы в привилегированном режиме: {obj["PercentPrivilegedTime"]}");
                    Console.WriteLine($"% загруженности процессора: {obj["PercentProcessorTime"]}");
                    Console.WriteLine($"% работы в пользовательском режиме: {obj["PercentUserTime"]}");
                }
            }

        }

        private async System.Threading.Tasks.Task Run()
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


            ////////////////////////////////////////////////////
            // Данные о заданиях планировщика Windows
            //
            TaskService ts = new TaskService();

            List<TaskItem> taskItems = new List<TaskItem>();
            foreach (Microsoft.Win32.TaskScheduler.Task SelestTask in ts.AllTasks.ToList())
            {
                if (SelestTask.Name.StartsWith("NVIDIA"))   // Your task-prefix
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


            //**************
            // Просмотр содержимого диска

            Console.WriteLine("~~~~~~~~~~");
            // Create the service.
            var service = GoogleDrive.CreateService(GoogleDrive.GetUserCredential());

            List<string> GlistFile = await GoogleDrive.GetListAsync(service);
            foreach (string FileName in GlistFile)
            {
                Console.WriteLine(FileName);
            }
            Console.WriteLine("~~~~~~~~~~");

            List<string> GListId = await GoogleDrive.UploadFilesAsync(service, ListIsoStorageFile.ListFile);
            foreach (string FileName in GListId)
            {
                Console.WriteLine("File ID: " + FileName);
            }
            Console.WriteLine("================");

        }
    }
}
