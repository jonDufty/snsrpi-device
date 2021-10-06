using System;
using System.IO;
using Newtonsoft.Json;

namespace snsrpi.Models
{
	public class InputData
	{
		public int sampleRate { get; set; }
		public string outputDirectory { get; set; }
		public string outputType { get; set; }

		/* 
		Initialise with default values
		 */
		public InputData()
		{
			// var settings = Properties.MainSettings.Default;
			sampleRate = 500; //settings.SampleRate;
			outputType = "feather"; //settings.OutputType;

			DateTime currentDate = DateTime.Now.Date;
			string cwd = Directory.GetCurrentDirectory();
			outputDirectory = Path.Combine(cwd, $"CX1_Data_{currentDate:yyyyMMdd}/");


		}

		public static InputData InitFromJSON(string inputJSONFile)
		{
			InputData input;
			if (File.Exists(inputJSONFile))
			{
				try
				{
					using (StreamReader r = File.OpenText(inputJSONFile))
					{
						string inputJson = r.ReadToEnd();
						input = JsonConvert.DeserializeObject<InputData>(inputJson);
					}
					return input;
				}
				catch
				{
					Console.WriteLine("Error reading json file");
					return new InputData();
				}
			}
			else
			{
				input = new InputData();
				Console.WriteLine("No input file found... Creating with default inputs \n");
				return input;
			}
		}

		public void UpdateSettings()
		{
			// var settings = Properties.MainSettings.Default;
			// settings.SampleRate = sampleRate;
			// settings.OutputDirectory = outputDirectory;
			// settings.OutputType = outputType;

			// settings.Save();

		}

	}
}
