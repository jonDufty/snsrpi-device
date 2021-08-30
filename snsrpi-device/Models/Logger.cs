using System;
using System.Collections.Generic;
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
		public Thread Thread {get;}
		public CXCom Cx { get; }
		public CXDevice Device { get; }
		public OutputData Output { get; set; }
		public Queue<VibrationData> DataBuffers { get; }

		public AcqusitionSettings Settings {get; set;}

		public Logger(CXCom _cx, CXDevice _device) 
		{
			Cx = _cx;
			Device = _device;
			Output = new CSVOutput("./", "CX1_");
			DataBuffers = new Queue<VibrationData>();
		}

		public Logger(bool demo)
		{
			Cx = new();
			Device = null;
			Output = new FeatherOutput();
			DataBuffers = new();
			Settings = new AcqusitionSettings(500, "feather", "~/data");

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
					new CSVOutput(input.outputDirectory, "CX1_");
					break;
			}
			DataBuffers = new Queue<VibrationData>();
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
				Thread.Sleep(20);
				//break;

				DataBuffers.Enqueue(new VibrationData(timestamps, accel_x, accel_y, accel_z));
			}
		}

		public void StopAcquisition()
		{

		}
		public void WriteSamples()
		{
			while (DataBuffers.Count > 0)
			{
				Output.Write(DataBuffers.Dequeue());
				Thread.Sleep(20);

			}

			return;
		}

	}
}