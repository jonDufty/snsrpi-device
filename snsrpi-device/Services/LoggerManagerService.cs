using System.IO;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sensr.CX;
using Sensr.Utils;
using snsrpi.Models;
using snsrpi.Interfaces;

namespace snsrpi.Services
{
    public class LoggerManagerService : ILoggerManager
    {
        private Dictionary<string,Logger> Loggers {get;}
        private Dictionary<string,CancellationTokenSource> LoggerTokens {get; set;}
        public CancellationTokenSource GlobalSource;

        private CXCom CX {get;}

        public LoggerManagerService()
        {
            Loggers = new();
            CX = new();
        }

        public LoggerManagerService(bool _demo)
        {
            if(!_demo){
                Loggers = new();
                CX = new();
                return;
            }
            CX = new();

            Console.WriteLine("Creating cancellation token");
            GlobalSource = new();
            var token = GlobalSource.Token;

            LoggerTokens = new(){
                {"CX1_1901", new CancellationTokenSource()},
                {"CX1_1902", new CancellationTokenSource()},
                {"CX1_1903", new CancellationTokenSource()},
                {"CX1_1904", new CancellationTokenSource()},
            };

            Loggers = new(){
                {"CX1_1901", new Logger(true,"CX1_1901_", LoggerTokens["CX1_1901"].Token)},
                {"CX1_1902", new Logger(true,"CX1_1902_", LoggerTokens["CX1_1902"].Token)},
                {"CX1_1903", new Logger(true,"CX1_1903_", LoggerTokens["CX1_1903"].Token)},
                {"CX1_1904", new Logger(true,"CX1_1904_", LoggerTokens["CX1_1904"].Token)},
            };

            Console.WriteLine("Bootstrapping system...");
            Console.WriteLine($"Found {ListDevices().Count} devices");
            foreach (var device in ListDevices())
            {
                Console.WriteLine(device);
            }
        }

        public List<string> ListDevices()
        {
            return Loggers.Keys.ToList();
        }

        public bool CheckDevice(string deviceID)
        {
            return Loggers.ContainsKey(deviceID);
        }

        public void StartDevice(string deviceID)
        {
            var device = Loggers[deviceID];
            Console.WriteLine($"Starting device {deviceID}");
            // Create new token

            device.SetNewDeviceThread();
            var cst = new CancellationTokenSource();
            LoggerTokens[deviceID] = cst;
            device.TokenDeviceThread = cst.Token;            
            device.DeviceThread.Start();
            return;
        }

        public void StopAllDevices()
        {
            Console.WriteLine("Calling cancel requests");
            GlobalSource.Cancel();
        }

        public void StopDevice(string deviceID)
        {
            Console.WriteLine($"Cancelling device {deviceID}");
            LoggerTokens[deviceID].Cancel();
            return;
        }

        public Logger GetDevice(string deviceID)
        {
            return Loggers[deviceID];
        }

        public void SendHeartbeat()
        {
            return;
        }

    }
}