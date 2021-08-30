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

            Loggers = new(){
                {"CX1_1901", new Logger(true)},
                {"CX1_1902", new Logger(true)},
                {"CX1_1903", new Logger(true)},
                {"CX1_1904", new Logger(true)},
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
            device.StartAcquisition();
            return;
        }

        public void StopDevice(string deviceID)
        {
            Loggers[deviceID].StopAcquisition();
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