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
    public class LoggerManagerService : ILoggerManager
    {
        private string DeviceName {get;}
        private Dictionary<string,Logger> Loggers {get;}
        private Dictionary<string,CancellationTokenSource> LoggerTokens {get; set;}
        public CancellationTokenSource GlobalSource;
        private CXCom CX {get;}
        public Timer HeartbeatTimer {get;}
        public HttpClient _HttpClient {get;}
        private readonly ILogger<LoggerManagerService> Logs;
        
        public LoggerManagerService(bool demo, ILogger<LoggerManagerService> _logger)
        {

            DeviceName = System.Environment.GetEnvironmentVariable("DEVICE_NAME");
            CX = new();
            Logs = _logger;
            Console.WriteLine("Trying CX API");
            var loggers = CXUtils.List();  

            Logs.LogInformation("Creating cancellation token");
            GlobalSource = new();
            var token = GlobalSource.Token;

            LoggerTokens = new(){
                {"CX1_1901", new CancellationTokenSource()},
                {"CX1_1902", new CancellationTokenSource()},
                {"CX1_1903", new CancellationTokenSource()},
                {"CX1_1904", new CancellationTokenSource()},
            };

            Loggers = new(){
                {"CX1_1901", new Logger(true,"CX1_1901",_logger, LoggerTokens["CX1_1901"].Token)},
                {"CX1_1902", new Logger(true,"CX1_1902",_logger, LoggerTokens["CX1_1902"].Token)},
                {"CX1_1903", new Logger(true,"CX1_1903",_logger, LoggerTokens["CX1_1903"].Token)},
                {"CX1_1904", new Logger(true,"CX1_1904",_logger, LoggerTokens["CX1_1904"].Token)},
            };

            Logs.LogInformation("Bootstrapping system...");
            var log_string = $"Found {ListDevices().Count} devices";
            foreach (var device in ListDevices())
            {
                log_string += $"\n{device}";
            }
            Logs.LogInformation(log_string);

            // Create timeer thread for sending regular heartbeats
            // HeartbeatTimer = new Timer(Heartbeat, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30));

            _HttpClient = new();

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
            Logs.LogInformation($"Starting device {deviceID}");
            // Create new token

            device.SetNewDeviceThread();
            var cst = new CancellationTokenSource();
            LoggerTokens[deviceID] = cst;
            device.TokenDeviceThread = cst.Token;            
            device.DeviceThread.Start();
            device.IsActive = true;
            
            return;
        }

        public void StopAllDevices()
        {
            Logs.LogDebug("Calling cancel requests");
            GlobalSource.Cancel();
        }

        public void StopDevice(string deviceID)
        {
            Logs.LogInformation($"Cancelling device {deviceID}");
            LoggerTokens[deviceID].Cancel();
            Loggers[deviceID].IsActive = false;
            return;
        }

        public Logger GetDevice(string deviceID)
        {
            return Loggers[deviceID];
        }

        public void Heartbeat(Object stateInfo)
        {
            Logs.LogDebug("Sending heartbeat...");
            // Logs.LogInformation("Sending heartbeat");
            // var response = _HttpClient.GetAsync("http://aws.heartbeat.endpoint").Result;
            // Logs.LogDebug(response.ToString());            
        }

        public Health HealthCheck(){
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