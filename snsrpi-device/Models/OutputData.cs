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
        public CultureInfo culture;
        public abstract int Write(List<VibrationData> data);

        public string GetFileName(VibrationData data)
        {
            return outputPrefix + data.time.ToString("yyyy-MM-dd_HH-mm-ss") + fileExt;

        }
    }

    public class CSVOutput : OutputData
    {
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
            Console.WriteLine("Writing data...");
            Console.WriteLine($"#samples = {data.Count}");
            var filepath = Path.Combine(outputDir, GetFileName(data[0]));
            try
            {
                using StreamWriter writer = new(filepath);
                using CsvWriter csv = new(writer, culture);
                csv.WriteRecords(records: data);
            }
            catch
            {
                Console.WriteLine("File write failed");
                return 0;
            }
            return data.Count;
        }

        
    }

    public class FeatherOutput : OutputData
    {

        public FeatherOutput(string _outputDir, string _outputPrefix)
        {
            outputDir = _outputDir;
            outputPrefix = _outputPrefix;
            fileExt = ".feather";
            culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.DateTimeFormat.FullDateTimePattern = "yyyy-MM-dd_HH-mm-ss:ffffff";
        }
        public override int Write(List<VibrationData> data)
        {
            string filepath = GetFileName(data[0]);
            
            using (var writer = new FeatherWriter(filepath))
            {
                writer.AddColumn("time", data);
            }
            return 1;
        }
    }
}
