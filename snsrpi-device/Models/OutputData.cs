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
            return outputPrefix + data.time.ToString("yyyy_MM_dd_hh_mm") + fileExt;

        }
    }

    public class CSVOutput : OutputData
    {
        public CSVOutput(string _outputDir, string _outputPrefix)
        {
            outputDir = _outputDir;
            outputPrefix = _outputPrefix;
            fileExt = ".csv";
        }

        public override int Write(List<VibrationData> data)
        {
            var filepath = Path.Combine(outputDir, GetFileName(data[0]));
            try
            {
                using StreamWriter writer = new(filepath);
                using CsvWriter csv = new(writer, CultureInfo.InvariantCulture);
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
