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
		public string Device_id {get; set;}
		public Thread DeviceThread {get; set;}
		public Thread FileThread {get; set;}
		public CXCom Cx { get; }
		public CXDevice Device { get; }
		public OutputData Output { get; set; }
		public ConcurrentQueue<VibrationData> DataBuffers { get; }
		public CancellationToken TokenDeviceThread {get; set;}
		public AcquisitionSettings Settings {get; set;}

		public bool IsActive {get; set;}
		public bool Demo {get; set;}

		private readonly ILogger<LoggerManagerService> Logs;



		public Logger(bool demo, string device_id, ILogger<LoggerManagerService> _logger, CancellationToken token)
		{
			Cx = new();
			Device_id = device_id;
			Device = null;
			Settings = InitialiseSettings();
			Output = new CSVOutput(Settings.Output_directory, device_id + "_");
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
			switch (input.outputType)
			{
				case "csv":
					Output = new CSVOutput(input.outputDirectory, "CX1_");
					break;
				case "feather":
					Output = new FeatherOutput();
					break;
				default:
					Output = new CSVOutput(input.outputDirectory, "CX1_");
					break;
			}
			DataBuffers = new ();
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
			{
				Cx.Disconnect();
			}
			else
			{
				Console.WriteLine("No device is connected");
			}
		}

		public void Start()
		{
			Logs.LogInformation("Starting demo Acqusition...");
			// IsActive = true;
			if (Demo)
				RunDemo();
		}

		public void RunDemo()
		{
			

			Logs.LogDebug("Creating File Writing Thread");
			CancellationTokenSource source = new();
			FileThread = new(WriteFiles);
			FileThread.Start(source.Token);
			
			var dt = 0.01;
			var t = 0;
			double _t;
			int sampleSize = 1000;
			int[] sample = Enumerable.Range(0,sampleSize).ToArray();
			List<string> timestamps;
			List<double> accel_x;
			List<double> accel_y;
			List<double> accel_z;

			while(!TokenDeviceThread.IsCancellationRequested)
			{
				timestamps = new();
				accel_x = new();
				accel_y = new();
				accel_z = new();

				foreach(int s in sample)
				{
					_t = t + s*dt;
					timestamps.Add(_t.ToString());
					accel_x.Add(Math.Sin(_t));
					accel_y.Add(Math.Cos(_t));
					accel_z.Add(5*Math.Sin(_t));
				}

				Thread.Sleep(TimeSpan.FromSeconds(60));
				t ++;
				DataBuffers.Enqueue(new VibrationData(timestamps, accel_x, accel_y, accel_z));

			}

			Logs.LogInformation("Cancellation Request received. Sending file thread cancel");
			// IsActive = false;
			source.Cancel();

			// Console.

		}
		public void StartAcquisition()
		{
			int timeLimit = 100;

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
			Stopwatch timer = new();
			timer.Start();
			var timestamps = new List<string>();
			var accel_x = new List<double>();
			var accel_y = new List<double>();
			var accel_z = new List<double>();
			//TODO Change hardcoded time limit
			
			while (timer.Elapsed.TotalSeconds < timeLimit)
			{
				
				List<CXCom.Sample> samples = Cx.GetSamples();
				foreach (var s in samples)
				{
					//buffer.AppendSample(s.TimeStamp, s.Acceleration_X, s.Acceleration_Y, s.Acceleration_Z);
					if (s.AccelerationValid)
					{
						timestamps.Add(s.TimeStamp.ToString("MM/dd/yyyy HH:mm:ss.ffffff"));
						accel_x.Add(s.Acceleration_X);
						accel_y.Add(s.Acceleration_Y);
						accel_z.Add(s.Acceleration_Z);
					}
				}
				Thread.Sleep(1000);
				//break;

				DataBuffers.Enqueue(new VibrationData(timestamps, accel_x, accel_y, accel_z));
			}
		}

		public void StopAcquisition()
		{

		}

		public void WriteFiles(Object obj){
			var token = (CancellationToken)obj;
			VibrationData data;
            while (!token.IsCancellationRequested)
			{
				if(DataBuffers.TryDequeue(out data))
				{
					//Write the files...
					Logs.LogDebug("Writing files...");	
					Output.Write(data);
				}
			}
			Logs.LogInformation("Cancellation request received: Stopping file writes");
		}

		private AcquisitionSettings InitialiseSettings()
		{
			string configPath = System.Environment.GetEnvironmentVariable("DEVICE_CONFIG_DIR");
			if (configPath != null)
			{
				var configFileName = Device_id + "_config.json";
				configPath = Path.Combine(configPath, configFileName);
				if (File.Exists(configPath))
				{
					AcquisitionSettings settings = AcquisitionSettings.LoadFromFile(configPath);
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