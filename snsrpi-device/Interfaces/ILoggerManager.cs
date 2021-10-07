using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sensr.CX;
using Sensr.Utils;
using snsrpi.Models;

namespace snsrpi.Interfaces
{
    
    /// <summary>
    /// Interface for accessing LoggerManagerService with API controllers
    /// </summary>
    public interface ILoggerManager
    {
        List<string> ListDevices();
        Logger GetDevice(string id);
        bool CheckDevice(string id);
        void StartDevice(string id);
        void StopDevice(string id);
        Health HealthCheck();

    }
}