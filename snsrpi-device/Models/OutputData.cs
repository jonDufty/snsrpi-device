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

        public abstract int Write(List<VibrationData> data);

        public string GetFileName(VibrationData data)
        {
            return outputPrefix + data.time.ToString("yyyy-MM-dd_HH-mm-ss") + fileExt;

        }
    }

    public class CSVOutput : OutputData
    {
        public CultureInfo culture;
        public CSVOutput(string _outputDir, string _outputPrefix)
        {
            outputDir = _outputDir;
            outputPrefix = _outputPrefix;
            fileExt = ".csv";
            culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.DateTimeFormat.ShortDatePattern = "yyyy-MM-dd";
            culture.DateTimeFormat.LongTimePattern = "HH:mm:ss:ffffff";
        }

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

    public class FeatherOutput : OutputData
    {
        public string DatetimeFormat;

        public FeatherOutput(string _outputDir, string _outputPrefix)
        {
            outputDir = _outputDir;
            outputPrefix = _outputPrefix;
            fileExt = ".feather";
            DatetimeFormat = "yyyy-MM-dd_HH-mm-ss:ffffff";
        }
        public override int Write(List<VibrationData> data)
        {
            var filepath = Path.Combine(outputDir, GetFileName(data[0]));
            Console.WriteLine($"Writing data to {filepath}");

            // Build column data
            List<string> time = new();
            List<double> accel_x = new();
            List<double> accel_y = new();
            List<double> accel_z = new();
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
