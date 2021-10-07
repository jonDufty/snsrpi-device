using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sensr.CX;
using Sensr.Utils;
using snsrpi.Services;

namespace snsrpi.Models
{
    public class Logger
    {
        // Main identifier for device
        public string Device_id { get; set; }
        // Thread reference for individual device
        public Thread DeviceThread { get; set; }
        
        // Thread ref for file writing thread 
        public Thread FileThread { get; set; }
        
        // CXCom interface used for operating device
        public CXCom Cx { get; }
        //snsr CXDevice object
        public CXDevice Device { get; }
        // Output data object for writing out files
        public OutputData Output { get; set; }
        // Thread safe queue for storing and writing out data
        public ConcurrentQueue<VibrationData> DataBuffers { get; }
        // Cancellation token passed down from parent functions
        public CancellationToken TokenDeviceThread { get; set; }
        // Device settings
        public AcquisitionSettings Settings { get; set; }
        // Flag for whether device is running or not
        public bool IsActive { get; set; }
        // Flag for demo mode
        public bool Demo { get; set; }

        private readonly int DEFAULT_SAMPLE = 2000;
        // Logging object
        private readonly ILogger<LoggerManagerService> Logs;
        
        /// <summary>
        /// Constructor for Logger class
        /// </summary>
        /// <param name="demo"> Use demo mode or not </param>
        /// <param name="device_id"> Device id </param>
        /// <param name="_logger"> In built logging service</param>
        /// <param name="token"> Cancellation token for device to be used to stop the thread </param>
        public Logger(bool demo, string device_id, ILogger<LoggerManagerService> _logger, CancellationToken token)
        {
            Logs = _logger;
            Cx = new();
            Device_id = device_id;
            Device = null;
            Settings = InitialiseSettings();
            Output = null; //Create this on StartAcqusition
            DataBuffers = new();
            DeviceThread = null; //Create this on StartAcqusition
            FileThread = null; //Create this on StartAcqusition
            TokenDeviceThread = token;
            IsActive = false;
            Demo = demo;
            Console.WriteLine($"Demo = {demo}");
        }

        /// <summary>
        /// Constructor used when not in demo mode and passing in a CXDevice object instead
        /// </summary>
        /// <param name="_device">CXDevice object. Returned when searching for device using CXUtils.List</param>
        /// <param name="_logger">.NET logging object</param>
        /// <param name="token">Cancellation token</param>
        public Logger(CXDevice _device, ILogger<LoggerManagerService> _logger, CancellationToken token)
        {
            
            Logs = _logger;
            Cx = new();
            Device_id = _device.Name;
            Device = _device;
            Settings = InitialiseSettings();
            Output = null;
            DataBuffers = new();
            DeviceThread = null;
            FileThread = null;
            TokenDeviceThread = token;
            IsActive = false;
            Demo = false;
            Console.WriteLine($"Demo = {Demo}");
        }

        /// <summary>
        /// On start, creates a new thread each time
        /// </summary>
        public void SetNewDeviceThread()
        {
            DeviceThread = new(new ThreadStart(Start));
            DeviceThread.Name = Device_id + "_run";
        }

        /// <summary>
        /// Attempts to connect to device
        /// </summary>
        /// <returns>bool indicating success or not</returns>
        public bool Connect()
        {
            try
            {
                Cx.Connect(Device);
                CXCom.LoginStatus login = Cx.Login(CXCom.LoginUserID.Admin, "admin");
                if (login != CXCom.LoginStatus.Ok)
                {
                    Logs.LogWarning("Could not log into device: {0}", login);
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                Logs.LogInformation($"{e}");
                return false;
            }
        }

        public void Disconnect()
        {
            if (Cx.IsConnected())
                Cx.Disconnect();
            else
                Console.WriteLine("No device is connected");
        }

        public void Start()
        {
            // Initialise output settings. This allows for settings to be updated on runtime
            Output = Settings.Output_type switch
            {
                "csv" => new CSVOutput(Settings.Output_directory, Device_id + "_"),
                "feather" => new FeatherOutput(Settings.Output_directory, Device_id + "_"),
                _ => new CSVOutput(Settings.Output_directory, Device_id + "_"),
            };            
            
            //Set isactive flag to true and run either real or demo acqusition
            IsActive = true;
            if (Demo)
                RunDemo();
            else
                StartAcquisition();
        }

        /// <summary>
        /// Generates fake data for demo acqusition. Behviour is the same as real acqusition otherwise
        /// </summary>
        public void RunDemo()
        {
            Logs.LogInformation("Starting demo Acqusition...");
            Logs.LogInformation("Creating File Writing Thread");
            
            // Create a new thread to write files as data gets added to the buffer
            CancellationTokenSource sourceFileThread = new();
            FileThread = new(WriteFiles);
            FileThread.Name = Device_id + "_file";
            FileThread.Start(sourceFileThread.Token);

            var t = 0;
            DateTime startTime;
            double _t;
            int sampleSize = Settings.Sample_rate;
            double dt = 1.0 / sampleSize;
            int[] sample = Enumerable.Range(0, sampleSize).ToArray();
            DateTime timestamp;
            double accel_x;
            double accel_y;
            double accel_z;

            // Loop until we get a cancellation request. This is a more elegant way of doing an infinite loop for 
            // a thread
            while (!TokenDeviceThread.IsCancellationRequested)
            {
                startTime = DateTime.Now;
                foreach (int s in sample)
                {
                    _t = t + s * dt;

                    timestamp = startTime.Add(TimeSpan.FromMilliseconds(s * dt * 1000));
                    accel_x = Math.Sin(_t);
                    accel_y = Math.Cos(_t);
                    accel_z = 5 * Math.Sin(_t);
                    DataBuffers.Enqueue(new VibrationData(timestamp, accel_x, accel_y, accel_z));
                }
                Thread.Sleep(TimeSpan.FromSeconds(1));
                t++;
            }

            Logs.LogInformation("Cancellation Request received. Sending file thread cancel");
            sourceFileThread.Cancel();
        }

        public void StartAcquisition()
        {
            // Try to connect to device
            Console.WriteLine("Starting Acquisition");
            if (!Cx.IsConnected())
            {
                Console.WriteLine("No Device is connected. Attempting reconnect");
                var result = Connect();
                if (!result)
                {
                    Console.WriteLine("Cancelling Acquisition");
                    IsActive = false;
                }
            }

            // Need to enable streaming before acqusition can be run
            if (!Cx.StreamEnable())
            {
                Console.WriteLine("Could not enable streaming");
                IsActive = false;
                return;
            }

            // Create separate thread for writing data tot files
            Logs.LogInformation("Creating File Writing Thread");

            // Pass cancellation token to function to be able to cancel function from here
            CancellationTokenSource sourceFileThread = new();
            FileThread = new(WriteFiles);
            FileThread.Name = Device_id + "_file";
            FileThread.Start(sourceFileThread.Token);

            // Loop until we recieve a cancellation event
            while (!TokenDeviceThread.IsCancellationRequested)
            {

                List<CXCom.Sample> samples = Cx.GetSamples();
                // Append each sample to thread safe queue
                foreach (var s in samples)
                {
                    if (s.AccelerationValid)
                    {
                        DataBuffers.Enqueue(new VibrationData(
                            s.TimeStamp,
                            s.Acceleration_X,
                            s.Acceleration_Y,
                            s.Acceleration_Z
                        ));
                    }
                }
                Thread.Sleep(50);
            }

            Logs.LogInformation("Cancellation Request received. Sending file thread cancel");
            sourceFileThread.Cancel();
        }

        /// <summary>
        /// Concurrent thread that takes samples from buffer as they are added and writes out files
        /// </summary>
        /// <param name="obj">Cancellation token</param>
        public void WriteFiles(object obj)
        {
            var token = (CancellationToken)obj;
            
            // Total number of samples per file
            var numSamples = Settings.Save_interval.TotalSeconds() * Settings.Sample_rate;
            var decimate = DEFAULT_SAMPLE / Settings.Sample_rate;
            int samplesWritten;
            
            Logs.LogInformation($"File thread started. #samples per file: {numSamples}");
            
            //Create new buffer to send to Output Data object
            List<VibrationData> buffer = new();
            int i = 0;
            int retries;

            // Run until we get cancel request from parent function
            while (!token.IsCancellationRequested)
            {
                // Try dequeue is a safe was to try pop next element from the queue
                if (DataBuffers.TryDequeue(out VibrationData data)){
                    if (i % decimate == 0)
                        buffer.Add(data); //Only add every n-th data point based on sample rate
                    i++;
                }

                if (buffer.Count >= numSamples)
                {
                    //Write the files...
                    retries = 0; // Limit retries of writing file before clearing buffer
                    Logs.LogDebug("Writing files...");
                    samplesWritten = Output.Write(buffer);
                    if (samplesWritten > 0)
                    {
                        Logs.LogInformation("File write successful");
                        buffer.Clear();
                        i = 0;
                        retries = 0;
                    }
                    else if (retries > 5) 
                    {
                        Logs.LogError("Number of retries exceeded for file write. Clearing buffer. May need to kill program");
                        buffer.Clear();
                        retries = 0;
                    }
                    else
                    {
                        Logs.LogError("Error writing files. No samples written");
                        retries++;
                    }
                    Thread.Sleep(TimeSpan.FromSeconds(60)); //If file size is <60s probably remove this.
                }

            }

            Logs.LogInformation("Cancellation request received: Writing remaining data and stopping file writes");

            // Graceful exit - Write remaining buffer to list and write to file
            buffer = DataBuffers.ToList();
            Output.Write(buffer);
            DataBuffers.Clear();

            Logs.LogInformation("Final file written, cancelling file thread");
            return;
        }

        /// <summary>
        /// Initialises acqusition settings. Looks for predefined config file, otherwise
        /// uses default settings
        /// </summary>
        /// <returns>new AcqusitionSettings objects</returns>
        private AcquisitionSettings InitialiseSettings()
        {
            // Check environment for path definitions
            string configPath = System.Environment.GetEnvironmentVariable("DEVICE_CONFIG_DIR");
            string outputDir = System.Environment.GetEnvironmentVariable("OUTPUT_DATA_DIR");
            if (configPath != null)
            {
                var configFileName = Device_id + "_config.json";
                configPath = Path.Combine(configPath, configFileName);
                if (File.Exists(configPath))
                {
                    Logs.LogInformation("Config file found. Loading settings...");
                    AcquisitionSettings settings = AcquisitionSettings.LoadFromFile(configPath);
                    if (outputDir != null)
                    {
                        settings.Output_directory = outputDir;
                        settings.SaveToFile(configPath);
                    }
                    return settings;
                }
            }
            Console.WriteLine("No config file found, creating default settings");
            // Current default is to write to current directory. This is not recommended
            return AcquisitionSettings.Create(100, "csv", Directory.GetCurrentDirectory());

        }

        /// <summary>
        /// Write settings to config file
        /// </summary>
        public void SaveSettings()
        {
            string configPath = System.Environment.GetEnvironmentVariable("DEVICE_CONFIG_DIR");
            if (Directory.Exists(configPath))
                configPath = Path.Combine(configPath, Device_id + "_config.json");
            else
                configPath = Path.Combine(Directory.GetCurrentDirectory(), Device_id + "_config.json");
            
            Logs.LogInformation($"Saving config to {configPath}");
            Settings.SaveToFile(configPath);
        }

    }
}