using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sensr.CX;
using Sensr.Utils;
using Newtonsoft.Json;

namespace snsrpi.Models
{
    public class AcquisitionSettings
    {
        public int Sample_rate { get; set; }
        public string Output_type { get; set; }
        public bool Offline_mode { get; set; }
        public string Output_directory { get; set; }
        public FileUploadSettings File_upload { get; set; }
        public SaveIntervalSettings Save_interval { get; set; }
        public static AcquisitionSettings Create(int sample, string outputType, string directory)
        {
            FileUploadSettings file_upload = new(true, "http://localhost:6000");
            SaveIntervalSettings save_interval = new("minute", 1);
            return new AcquisitionSettings(sample, outputType, false, directory, file_upload, save_interval);
        }

        public AcquisitionSettings(int sample_rate, string output_type, bool offline_mode, string output_directory,
        FileUploadSettings file_upload, SaveIntervalSettings save_interval)
        {
            Sample_rate = sample_rate;
            Output_type = output_type;
            Offline_mode = offline_mode;
            Output_directory = output_directory;
            File_upload = file_upload;
            Save_interval = save_interval;
        }

        public static AcquisitionSettings LoadFromFile(string filePath)
        {
            using StreamReader file = File.OpenText(filePath);
            var serializer = new JsonSerializer();
            try
            {
                AcquisitionSettings settings = (AcquisitionSettings)serializer.Deserialize(file, typeof(AcquisitionSettings));
                Console.WriteLine(settings);
                return settings;
                
            }
            catch
            {
                return Create(500, "csv", "./data");
            }
        }

        public void SaveToFile(string filePath)
        {
            using StreamWriter file = File.CreateText(filePath);
            var serializer = new JsonSerializer();
            serializer.Serialize(file, this);
        }
    }

    public class SaveIntervalSettings
    {
        public string Unit { get; set; }
        public int Interval { get; set; }

        private readonly static Dictionary<string, int> multipliers = new()
        {
            { "second", 1 },
            { "minute", 60 },
            { "hour", 3600 }
        };

        public SaveIntervalSettings(string unit, int interval)
        {
            this.Unit = unit;
            this.Interval = interval;
        }

        public int TotalSeconds()
        {
            return multipliers[Unit] * Interval;
        }
    }
    public class FileUploadSettings
    {
        public bool Active { get; set; }
        public string Endpoint { get; set; }

        public FileUploadSettings(bool active, string endpoint)
        {
            Active = active;
            Endpoint = endpoint;
        }

        public bool IsActive()
        {
            return Active;
        }

    }
}