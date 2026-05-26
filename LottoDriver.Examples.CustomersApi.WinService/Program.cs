using System;
using System.ServiceProcess;

namespace LottoDriver.Examples.CustomersApi.WinService
{
    /// <summary>
    /// Entry point for the .NET Framework example. The same executable runs as a
    /// Windows Service when launched by the SCM, or as an interactive console app
    /// when run directly (useful for debugging under Visual Studio).
    /// </summary>
    static class Program
    {
        /// <summary>
        /// Dispatches to <c>ServiceBase.Run</c> in non-interactive (SCM) mode, or
        /// runs the service on the console in interactive mode and waits for the
        /// user to press Enter before stopping.
        /// </summary>
        static void Main(string[] args)
        {
            var svc = new Service1();

            if (Environment.UserInteractive)
            {
                Console.WriteLine("Starting example customers api client service");
                svc.ConsoleStart(args);
                Console.WriteLine("Started! Press Enter to stop.");

                Console.ReadLine();

                Console.WriteLine("Stopping example customers api client service");
                svc.Stop();
                Console.WriteLine("Stopped.");
            }
            else
            {
                ServiceBase.Run(svc);
            }
        }
    }
}
