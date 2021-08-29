using Microsoft.VisualStudio.TestTools.UnitTesting;
using snsrpi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace snsrpi.Tests
{
    [TestClass]
    public class OutputDataTests
    {

        VibrationData data;
        string outputDir;
        string outputPrefix;

        [TestInitialize]
        public void TestInitialize()
        {
            
            
            DateTime[] time_array = new DateTime[100];
            int minute = 12;
            for (int i = 0; i < time_array.Length; i++)
            {
                if (i > 60) minute = 13;
                time_array[i] = new DateTime(2010, 10, 5, 12, minute, i % 60);
            }

            int min = -5;
            int max = 5;
            double[] test_array = new double[100];
            for (int i = 0; i < test_array.Length; i++)
            {
                Random randNum = new();
                test_array[i] = randNum.Next(min, max);
            }
            data = new VibrationData(time_array, test_array, test_array, test_array);
            var ParentDir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
            outputDir= Path.Combine(ParentDir, "resources", "tests","outputs");
            outputPrefix = "TEST_";


        }

        [TestMethod]
        public void WriteCSVTest()
        {
            CSVOutput output = new(outputDir, outputPrefix);
            output.Write(data);

        }
    }
}