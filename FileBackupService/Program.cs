using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace FileBackupService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            
            if (Environment.UserInteractive)
            {
                BackupService service1 = new BackupService();
                service1.TestStartandStop(args);
            }
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                new BackupService()
                };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
