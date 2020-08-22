using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Diagnostics;
using System.Text;


using System.ComponentModel;
using System.Data;
using System.Threading.Tasks;
using System.IO;
using System.IO.IsolatedStorage;

namespace DCTR_Cliest
{
    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new Cliest()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
