using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sensr.CX;
using Sensr.Utils;

namespace snsrpi.Models
{
	public class Logger
	{
		public Thread DeviceThread {get; set;}
		public Thread FileThread {get; set;}
		public CXCom Cx { get; }
		public CXDevice Device { get; }
		public OutputData Output { get; set; }
		public ConcurrentQueue<VibrationData> DataBuffers { get; }
		public CancellationToken TokenDeviceThread {get; set;}
		public AcqusitionSettings Settings {get; set;}

		public Logger(CXCom _cx, CXDevice _device) 
		{
			Cx = _cx;
			Device = _device;
			Output = new CSVOutput("./", "CX1_");
			DataBuffers = new();
			Settings = new AcqusitionSettings(500, "feather", "/home/jondufty/data");
			DeviceThread = new(new ThreadStart(Start));
			FileThread = new(new ParameterizedThreadStart(WriteFiles));
		}

		public Logger(bool demo, string prefix, CancellationToken token)
		{
			Cx = new();
			Device = null;
			Settings = new AcqusitionSettings(500, "csv", "/home/jondufty/data");
			Output = new CSVOutput(Settings.Output_Directory,prefix);
			DataBuffers = new();
			// DeviceThread = new(new ThreadStart(Start));
			// FileThread = new(WriteFiles);
			TokenDeviceThread = token;
			Console.WriteLine(token);

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
					Console.WriteLine("Could not log into device: {0}", login);
					return false;
				}
				return true;
			}
			catch (Exception e)
			{
				Console.WriteLine($"{e}");
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
			Console.WriteLine("Starting demo Acqusition...");
			Run();
		}

		public void Run()
		{
			

			Console.WriteLine("Creating File Writing Thread");
			CancellationTokenSource source = new();
			FileThread = new(WriteFiles);
			FileThread.Start(source.Token);
			
			var dt = 0.01;
			var t = 0;
			double _t;
			int sampleSize = 100;
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

				Thread.Sleep(1000);
				t ++;
				Console.WriteLine($"Adding sample to queue. Token = {TokenDeviceThread.IsCancellationRequested}, IsCancellable = {TokenDeviceThread.CanBeCanceled}");
				DataBuffers.Enqueue(new VibrationData(timestamps, accel_x, accel_y, accel_z));

			}

			Console.WriteLine("Cancellation Request received. Sending file thread cancel");
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
					Console.Write("Writing files...");	
					Output.Write(data);
				}
			}
			Console.WriteLine("Cancellation request received: Stopping file writes");
		}

	}
}