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
        public string Device_id { get; set; }
        public Thread DeviceThread { get; set; }
        public Thread FileThread { get; set; }
        public CXCom Cx { get; }
        public CXDevice Device { get; }
        public OutputData Output { get; set; }
        public ConcurrentQueue<VibrationData> DataBuffers { get; }
        public CancellationToken TokenDeviceThread { get; set; }
        public AcquisitionSettings Settings { get; set; }

        public bool IsActive { get; set; }
        public bool Demo { get; set; }

        private readonly ILogger<LoggerManagerService> Logs;



        public Logger(bool demo, string device_id, ILogger<LoggerManagerService> _logger, CancellationToken token)
        {
            Cx = new();
            Device_id = device_id;
            Device = null;
            Settings = InitialiseSettings();
            Output = Settings.Output_type switch
            {
                "csv" => new CSVOutput(Settings.Output_directory, device_id + "_"),
                "feather" => new FeatherOutput(Settings.Output_directory, device_id + "_"),
                _ => new CSVOutput(Settings.Output_directory, device_id + "_"),
            };
            DataBuffers = new();
            DataBuffers = new();
            DeviceThread = null;
            FileThread = null;
            TokenDeviceThread = token;
            Logs = _logger;
            IsActive = false;
            Demo = demo;
        }

        public Logger(CXCom _cx, CXDevice _device, InputData input)
        {
            Cx = _cx;
            Device = _device;
            Output = input.outputType switch
            {
                "csv" => new CSVOutput(input.outputDirectory, "CX1_"),
                "feather" => new FeatherOutput(Settings.Output_directory, "Device" + "_"),
                _ => new CSVOutput(input.outputDirectory, "CX1_"),
            };
            DataBuffers = new();
        }

        public void SetNewDeviceThread()
        {
            DeviceThread = new(new ThreadStart(Start));
        }

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
            Logs.LogInformation("Starting demo Acqusition...");
            IsActive = true;
            if (Demo)
                RunDemo();
        }

        public void RunDemo()
        {


            Logs.LogDebug("Creating File Writing Thread");
            CancellationTokenSource sourceFileThread = new();
            FileThread = new(WriteFiles);
            FileThread.Start(sourceFileThread.Token);

            var t = 0;
            DateTime startTime;
            double _t;
            int sampleSize = 100;
            var dt = 1 / sampleSize;
            int[] sample = Enumerable.Range(0, sampleSize).ToArray();
            DateTime timestamp;
            double accel_x;
            double accel_y;
            double accel_z;

            while (!TokenDeviceThread.IsCancellationRequested)
            {
                startTime = DateTime.Now;
                foreach (int s in sample)
                {
                    _t = t + s * dt;
                    timestamp = startTime.Add(TimeSpan.FromSeconds(s * dt));
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
            if (!Cx.IsConnected())
            {
                Console.WriteLine("No Device is connected. Attempting reconnect");
                var result = Connect();
                if (!result)
                {
                    Console.WriteLine("Cancelling Acquisition");
                }
            }

            if (!Cx.StreamEnable())
            {
                Console.WriteLine("Could not enable streaming");
                return;
            }

            //Create stopwatch timer for now
            while (!TokenDeviceThread.IsCancellationRequested)
            {

                List<CXCom.Sample> samples = Cx.GetSamples();
                foreach (var s in samples)
                {
                    //buffer.AppendSample(s.TimeStamp, s.Acceleration_X, s.Acceleration_Y, s.Acceleration_Z);
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
                Thread.Sleep(1000);
            }
        }

        public void StopAcquisition()
        {

        }

        public void WriteFiles(Object obj)
        {
            var token = (CancellationToken)obj;
            // Total number of samples per file
            var numSamples = Settings.Save_interval.TotalSeconds() * Settings.Sample_rate;
            int samplesWritten;
            List<VibrationData> buffer = new();
            while (!token.IsCancellationRequested)
            {
                if (DataBuffers.TryDequeue(out VibrationData data))
                    buffer.Add(data);

                if (buffer.Count >= numSamples)
                {
                    //Write the files...
                    Logs.LogDebug("Writing files...");
                    samplesWritten = Output.Write(buffer);
                    if (samplesWritten > 0)
                    {
						Logs.LogInformation("File write successful");
						buffer.Clear();
                    }
                    else
                    {
                        Logs.LogError("Error writing files. No samples written");
                    }
                }
            }

            Logs.LogInformation("Cancellation request received: Writing remaining data and stopping file writes");
			buffer = DataBuffers.ToList();
			Output.Write(buffer);
			DataBuffers.Clear();

			Logs.LogInformation("Final file written, cancelling file thread");
			return;
        }

        private AcquisitionSettings InitialiseSettings()
        {
            string configPath = System.Environment.GetEnvironmentVariable("DEVICE_CONFIG_DIR");
            string outputDir = System.Environment.GetEnvironmentVariable("OUTPUT_DATA_DIR");
            if (configPath != null)
            {
                var configFileName = Device_id + "_config.json";
                configPath = Path.Combine(configPath, configFileName);
                if (File.Exists(configPath))
                {
                    AcquisitionSettings settings = AcquisitionSettings.LoadFromFile(configPath);
                    if (outputDir != null)
                    {
                        settings.Output_directory = outputDir;
                        settings.SaveToFile(configPath);
                    }
                    return settings;
                }
            }
            return AcquisitionSettings.Create(500, "csv", "/home/jondufty/data");

        }

        public void SaveSettings()
        {
            string configPath = System.Environment.GetEnvironmentVariable("DEVICE_CONFIG_DIR");
            if (Directory.Exists(configPath))
                configPath = Path.Combine(configPath, Device_id + "_config.json");
            Logs.LogInformation($"Saving config to {configPath}");
            Settings.SaveToFile(configPath);
        }

    }
}