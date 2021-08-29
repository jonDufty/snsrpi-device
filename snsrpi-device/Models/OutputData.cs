using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FeatherDotNet;

namespace snsrpi.Models
{
    public abstract class OutputData
    {
        public string outputDir;
        public string outputPrefix;
        public abstract bool Write(VibrationData data);
    }

    public class CSVOutput : OutputData
    {
        public CSVOutput(string _outputDir, string _outputPrefix)
        {
            outputDir = _outputDir;
            outputPrefix = _outputPrefix;
        }
        
        public override bool Write(VibrationData data)
        {
            var csv = new StringBuilder();
            for (int i = 0; i < data.time.Count; i++)
            {
                var newline = $"{data.time[i]},{data.accel_x[i]},{data.accel_y[i]},{data.accel_z[i]}";
                csv.AppendLine(newline);
            }
            var filepath = Path.Combine(outputDir, GetFileName(data));
            File.WriteAllText(filepath, csv.ToString());
            return true;
        }

        public string GetFileName(VibrationData data)
        {
            DateTime timestamp = DateTime.Parse(data.time[0].ToString());
            return outputPrefix + timestamp.ToString("yyyy_MM_dd_hh_mm") + ".csv";

        }
    }

    public class FeatherOutput : OutputData
    {
        public override bool Write(VibrationData data)
        {
            string filepath = "./";
            using (var writer = new FeatherWriter(filepath))
            {
                writer.AddColumn("time", data.time);
            }
            return true;
        }
    }
}
