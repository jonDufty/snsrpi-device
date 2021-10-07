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
        
        public int Sample_rate { get; set; } // Device sample rate
        public string Output_type { get; set; } //Output file format. Currently csv/feather are supported
        public bool Offline_mode { get; set; } //Offline mode, not implemented currently
        public string Output_directory { get; set; } //Location of output files. I realise this can contradict the environment variable at the moment
        public FileUploadSettings File_upload { get; set; } //See FileUploadSettings Class.
        public SaveIntervalSettings Save_interval { get; set; } //See SaveIntervalSettings Class
        /// <summary>
        /// Creates default Acqusition Settings based on simpler inputs
        /// </summary>
        /// <param name="sample">Sample Rate</param>
        /// <param name="outputType">Output type</param>
        /// <param name="directory">Output directory</param>
        /// <returns></returns>
        public static AcquisitionSettings Create(int sample, string outputType, string directory)
        {
            FileUploadSettings file_upload = new(true, "endpoint");
            SaveIntervalSettings save_interval = new("minute", 1);
            return new AcquisitionSettings(sample, outputType, false, directory, file_upload, save_interval);
        }

        /// <summary>
        /// Constructor for Acqusition settings. This is not called in the application itself, typically Create or LoadFromFile
        /// are called instead, but this definition is required so the API can automatically serialize/deserialize JSON objects
        /// into this class
        /// </summary>
        /// <param name="sample_rate">Sample rate</param>
        /// <param name="output_type">Output type: csv/feather </param>
        /// <param name="offline_mode">Offline mode: bool- currently not implemented</param>
        /// <param name="output_directory">Location of files - currently based on environment variable</param>
        /// <param name="file_upload">FileUploadSettings object</param>
        /// <param name="save_interval">SaveIntervalSettings object</param>
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

        /// <summary>
        /// Initialise settings from file
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Save settings to file
        /// </summary>
        /// <param name="filePath">Config file path</param>
        public void SaveToFile(string filePath)
        {
            using StreamWriter file = File.CreateText(filePath);
            var serializer = new JsonSerializer();
            serializer.Serialize(file, this);
        }
    }

    /// <summary>
    /// Save Interval settings class. Dictates how often to save files
    /// </summary>
    public class SaveIntervalSettings
    {
        public string Unit { get; set; } //Unit of time. Minutes, seconds or hours
        public int Interval { get; set; } //Save interval (i.e. 5min, 30 sec)

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

        /// <summary>
        /// Calculates total seconds in save interval
        /// </summary>
        /// <returns>int: Number of seconds in the save interval</returns>
        public int TotalSeconds()
        {
            return multipliers[Unit] * Interval;
        }
    }
    
    /// <summary>
    /// Settings storing information about cloud upload. 
    /// Currently this is not implemented
    /// </summary>
    public class FileUploadSettings
    {
        public bool Active { get; set; } //Whether to upload files or not
        public string Endpoint { get; set; } //e.g. AWS bucket name

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