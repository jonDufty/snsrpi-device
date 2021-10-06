using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using snsrpi.Services;
using snsrpi.Interfaces;
using snsrpi.Models;

namespace snsrpi
{
    public class Program
    {
        public static void Main(string[] args)
        {

            var host = CreateHostBuilder(args).Build();

            // Init logger manager before starting host
            // InitDataAcquisition();

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        public static LoggerManagerService InitDataAcquisition() 
        {
            var loggerFactory = LoggerFactory.Create(
                logging => {
                    logging.AddConsole();
                    logging.AddDebug();
                }
            );

            var manager = new LoggerManagerService(true, loggerFactory.CreateLogger<LoggerManagerService>());
            manager.StartDevice(manager.ListDevices()[0]);
            Thread.Sleep(5000);
            manager.StopAllDevices();

            return manager;

        }
    }
}
