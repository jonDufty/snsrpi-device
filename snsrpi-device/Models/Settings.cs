using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sensr.CX;
using Sensr.Utils;

namespace snsrpi.Models
{
    public class AcqusitionSettings
    {
        private int Sample_Rate {get; set;}
        private string Output_Type {get; set;}
        private bool Offline_Mode {get;}
        private string Output_Directory {get; set;}
        private FileUploadSettings File_Upload {get;}
        private SaveIntervalSettings Save_Interval {get;}

        public AcqusitionSettings(int sample, string outputType, string Directory)
        {
            Sample_Rate = sample;
            Output_Type = outputType;
            Offline_Mode = false;
            Output_Directory = Directory;
            File_Upload = new(true, "aws.endpoint.arup");
            Save_Interval = new("minute", 5);
        }
        public AcqusitionSettings(bool demo){

        }

    }

    public class SaveIntervalSettings
    {
        private string Unit {get; set;}
        private int Interval {get; set;}

        private static Dictionary<string, int> multipliers = new() {
            {"second",1},
            {"minute",60},
            {"hour",3600}
        };

        public SaveIntervalSettings(string unit, int interval)
        {
            Unit = unit;
            Interval = interval;
        }

        public int TotalSeconds()
        {
            return multipliers[Unit] * Interval;
        }
    }
    public class FileUploadSettings
    {
        private bool Active {get;}
        private string EndPoint {get; set;}

        public FileUploadSettings(bool active, string endpoint)
        {
            Active = active;
            EndPoint = endpoint;
        }

        public bool IsActive()
        {
            return Active;
        }

    }
}