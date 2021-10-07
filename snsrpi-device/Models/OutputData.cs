using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FeatherDotNet;
using CsvHelper;
using CsvHelper.Configuration;

namespace snsrpi.Models
{
    public abstract class OutputData
    {
        public string outputDir;
        public string outputPrefix;
        public string fileExt;
        public int Decimate;

        public abstract int Write(List<VibrationData> data);

        /// <summary>
        /// Determins file name based on first data point/timestamp 
        /// </summary>
        /// <param name="data">Data point to based filename on</param>
        /// <returns>filename</returns>
        public string GetFileName(VibrationData data)
        {
            return outputPrefix + data.time.ToString("yyyy-MM-dd_HH-mm-ss") + fileExt;

        }
    }

    /// <summary>
    /// Class for writing out csv files 
    /// </summary>
    public class CSVOutput : OutputData
    {
        public CultureInfo culture;
        public CSVOutput(string _outputDir, string _outputPrefix, int decimate = 1)
        {
            outputDir = _outputDir;
            outputPrefix = _outputPrefix;
            fileExt = ".csv";
            Decimate = decimate;
            // Change the culture to format dates the way we want
            culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.DateTimeFormat.ShortDatePattern = "yyyy-MM-dd";
            culture.DateTimeFormat.LongTimePattern = "HH:mm:ss:ffffff";
        }

        /// <summary>
        /// Writes out csv files 
        /// </summary>
        /// <param name="data">List of data objects</param>
        /// <returns>Number of samples written to file</returns>
        public override int Write(List<VibrationData> data)
        {
            var filepath = Path.Combine(outputDir, GetFileName(data[0]));
            Console.WriteLine($"Writing data to {filepath}");
            Console.WriteLine($"#samples = {data.Count}");
            try
            {
                using StreamWriter writer = new(filepath);
                using CsvWriter csv = new(writer, culture);
                csv.WriteRecords(records: data);
            }
            catch
            {
                Console.WriteLine("File write failed");
                return -1;
            }
            return data.Count;
        }
    }

    /// <summary>
    /// Class for writing out feather outputs. Feather is a lightweight binary
    /// format that works well with Python and Pandas
    /// </summary>
    public class FeatherOutput : OutputData
    {
        public string DatetimeFormat;

        public FeatherOutput(string _outputDir, string _outputPrefix, int decimate = 1)
        {
            outputDir = _outputDir;
            outputPrefix = _outputPrefix;
            Decimate = decimate;
            fileExt = ".feather";
            DatetimeFormat = "yyyy-MM-dd_HH-mm-ss:ffffff";
        }
        /// <summary>
        /// Writes out files 
        /// </summary>
        /// <param name="data"></param>
        /// <returns> Number of samples written </returns>
        public override int Write(List<VibrationData> data)
        {
            var filepath = Path.Combine(outputDir, GetFileName(data[0]));
            Console.WriteLine($"Writing data to {filepath}");

            // Build column data
            List<string> time = new();
            List<double> accel_x = new();
            List<double> accel_y = new();
            List<double> accel_z = new();
            
            // Convert rows to columns
            foreach (var row in data)
            {
                time.Add(row.time.ToString(DatetimeFormat));
                accel_x.Add(row.accel_x);
                accel_y.Add(row.accel_y);
                accel_z.Add(row.accel_z);
            }

            try
            {

                using (var writer = new FeatherWriter(filepath, WriteMode.Eager))
                {
                    writer.AddColumn<string>("time", time);
                    writer.AddColumn<double>("accel_x", accel_x);
                    writer.AddColumn<double>("accel_y", accel_y);
                    writer.AddColumn<double>("accel_z", accel_z);
                }
                return data.Count;
            }
            catch
            {
                Console.WriteLine("Error writing file...");
                return -1;
            }
        }
    }
}
