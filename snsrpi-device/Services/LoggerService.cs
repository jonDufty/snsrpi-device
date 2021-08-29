using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sensr.CX;
using Sensr.Utils;
using snsrpi.Models;

namespace snsrpi.Services
{
    public class LoggerService
    {
        private Dictionary<string,Logger> Loggers {get;}
        private CXCom CX {get;}

        public LoggerService()
        {
            Loggers = new();
            CX = new();
        }

        public List<string> ListDevices()
        {
            return Loggers.Keys.ToList();
        }

        public bool CheckDevice(string deviceID)
        {
            return Loggers.ContainsKey(deviceID);
        }

        public bool StartDevice(string deviceID)
        {
            var device = Loggers[deviceID];
            device.StartAcquisition();
            return true;
        }

        public bool StopDevice(string deviceID)
        {
            Loggers[deviceID].StopAcquisition();
            return true;
        }

        public Logger GetDevice(string deviceID)
        {
            return Loggers[deviceID];
        }

    }
}