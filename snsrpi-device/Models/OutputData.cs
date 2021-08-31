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
            var filepath = Path.Combine(outputDir, GetFileName(data));
            using(var writer = new StreamWriter(filepath))
            using(var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                var records = new List<object>();
                
                for(int i=0; i<data.accel_x.Count; i++)
                {
                    i++;
                    records.Add(new {time=data.time[i], x = data.accel_x[i], y=data.accel_y[i], z=data.accel_z[i]});                    
                }
                csv.WriteRecords(records);
            }
            
            
            return true;
        }

        public string GetFileName(VibrationData data)
        {
            return outputPrefix + data.time[0].ToString() + ".csv";
            // DateTime timestamp = DateTime.Parse(data.time[0].ToString());
            // return outputPrefix + timestamp.ToString("yyyy_MM_dd_hh_mm") + ".csv";

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
