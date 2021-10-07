using System.IO;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Sensr.CX;
using Sensr.Utils;
using snsrpi.Models;
using snsrpi.Interfaces;


namespace snsrpi.Services
{
    /// <summary>
    /// Overarching manager class for all individual sensors/loggers
    /// Most API calls use this class (or the interface) to control each device
    /// </summary>
    public class LoggerManagerService : ILoggerManager
    {
        // Name of host device (i.e. raspberry pi name)
        private string DeviceName { get; }
        //Lookup dict of all loggers, indexed by logger names
        private Dictionary<string, Logger> Loggers { get; }
        // Lookup of all cancellation tokens, indexed by logger names
        private Dictionary<string, CancellationTokenSource> LoggerTokens { get; set; }
        //Global cancellation token to use for all devices. Currently not used
        public CancellationTokenSource GlobalSource;
        //.NET logging object
        private readonly ILogger<LoggerManagerService> Logs;

        /// <summary>
        /// Initialises Logger manager as a service accessible by API
        /// controllers. Finds all devices and connects on startup
        /// </summary>
        /// <param name="demo">Flag for whether to generate fake devices or search for real ones</param> 
        /// <param name="_logger">ASP.NET Logging object</param>
        public LoggerManagerService(bool demo, ILogger<LoggerManagerService> _logger)
        {

            DeviceName = System.Environment.GetEnvironmentVariable("DEVICE_NAME");
            DeviceName ??= "local_device"; //If Device name is null, sets to a default name local_device
            Logs = _logger;

            if (!demo)
            {
                // Search for devices and if found. Initialise Logger classes and create cancellation tokens
                Logs.LogInformation("Searching for devices...");
                List<CXDevice> loggers = CXUtils.List();

                Logs.LogInformation("Creating cancellation token");
                LoggerTokens = new();
                Loggers = new();
                foreach (CXDevice dev in loggers)
                {
                    LoggerTokens.Add(dev.Name, new CancellationTokenSource());
                    Loggers.Add(dev.Name, new Logger(dev, _logger, LoggerTokens[dev.Name].Token));
                }

            }
            else
            {
                //  If demo mode, manually create arbitrary devices and pass in cancellation tokens
                LoggerTokens = new()
                {
                    { "CX1_1901", new CancellationTokenSource() },
                    { "CX1_1902", new CancellationTokenSource() },
                    { "CX1_1903", new CancellationTokenSource() },
                };

                Loggers = new()
                {
                    { "CX1_1901", new Logger(true, "CX1_1901", _logger, LoggerTokens["CX1_1901"].Token) },
                    { "CX1_1902", new Logger(true, "CX1_1902", _logger, LoggerTokens["CX1_1902"].Token) },
                    { "CX1_1903", new Logger(true, "CX1_1903", _logger, LoggerTokens["CX1_1903"].Token) },
                };
            }

            GlobalSource = new();
            var token = GlobalSource.Token;

            // Just output some info about current devices

            Logs.LogInformation("Bootstrapping system...");
            var log_string = $"Found {ListDevices().Count} devices";
            foreach (var device in ListDevices())
            {
                log_string += $"\n{device}";
            }
            Logs.LogInformation(log_string);

            // If not a demo. Autostart all devices (can change this want)
            if (!demo)
            {
                foreach( string device_id in Loggers.Keys)
                    StartDevice(device_id);
            }
        }

        
        /// <summary>
        /// Returns list of device IDs
        /// </summary>
        /// <returns></returns>
        public List<string> ListDevices()
        {
            return Loggers.Keys.ToList();
        }


        /// <summary>
        /// Checks if a device_id exists in current devices. Used a lot by API controller
        /// </summary>
        /// <param name="deviceID">Device ID to check</param>
        /// <returns>True if device exists, false otherwise</returns>
        public bool CheckDevice(string deviceID)
        {
            return Loggers.ContainsKey(deviceID);
        }

        /// <summary>
        /// Starts device deviceID. Assumes device exists
        /// </summary>
        /// <param name="deviceID">Device id to start</param>
        public void StartDevice(string deviceID)
        {
            var device = Loggers[deviceID];
            Logs.LogInformation($"Starting device {deviceID}");
        
            // Create a new thread for the device so we can give it a new cancellationt oken
            device.SetNewDeviceThread();
            var cst = new CancellationTokenSource();
            
            // Add token to our dict so we can cancel it later
            LoggerTokens[deviceID] = cst;
            device.TokenDeviceThread = cst.Token;
            device.DeviceThread.Start();
            device.IsActive = true;
            return;
        }

        
        /// <summary>
        /// Stop device acqusition for particular device
        /// </summary>
        /// <param name="deviceID">Device to stop</param>
        public void StopDevice(string deviceID)
        {
            Logs.LogInformation($"Cancelling device {deviceID}");
            // Create cancellation request with devices token
            LoggerTokens[deviceID].Cancel();
            Loggers[deviceID].IsActive = false;
            return;
        }

        /// <summary>
        /// Get Logger object for a given deviceID
        /// </summary>
        /// <param name="deviceID">Specific deviceID</param>
        /// <returns>Logger object for device</returns>
        public Logger GetDevice(string deviceID)
        {
            return Loggers[deviceID];
        }

        /// <summary>
        /// Return basic overview of sensor states
        /// </summary>
        /// <returns></returns>
        public Health HealthCheck()
        {
            Logs.LogDebug("Healthcheck");
            List<SensorStatus> sensors = new();
            foreach (var dev in Loggers.Keys)
            {
                sensors.Add(new SensorStatus(dev, Loggers[dev].IsActive));
            }
            return new Health(DeviceName, sensors);
        }

    }
}