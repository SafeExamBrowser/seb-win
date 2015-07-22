using System;
using System.ServiceProcess;
using SebWindowsServiceWCF.ServiceImplementations;

namespace SebWindowsServiceWCF
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            try
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] 
            { 
                new SebWindowsServiceWCF() 
            };
                ServiceBase.Run(ServicesToRun);
            }
            catch (Exception ex)
            {
                Logger.Log(ex,"Unable to run the service!");
            }
        }
    }
}
