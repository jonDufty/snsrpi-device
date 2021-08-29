//using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using snsrpi.Models;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace snsrpi.Tests
{
    [TestClass]
    public class InputDataTests
    {
        [TestMethod]
        public void InputDataTest()
        {
            InputData testInputDefault = new();
            int sampleRateActual = 500;
            DateTime currentDate = DateTime.Now.Date;
            string cwd = Directory.GetCurrentDirectory();
            string outputDirectoryActual = Path.Combine(cwd, $"CX1_Data_{currentDate:yyyyMMdd}/").ToString();
            string outputTypeActual = "csv";

            Assert.AreEqual(testInputDefault.sampleRate, sampleRateActual);
            Assert.AreEqual(testInputDefault.outputType, outputTypeActual);
            Assert.AreEqual(testInputDefault.outputDirectory, outputDirectoryActual);
        }

        [TestMethod]
        public void InitFromJSONTest()
        {
            var ParentDir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
            var inputpath = Path.Combine(ParentDir,"resources", "tests", "input_test.json");
            InputData testInputDefault = InputData.InitFromJSON(inputpath);
            int sampleRateActual = 500;
            string outputDirectoryActual = "./CX1_Test_Json/";
            string outputTypeActual = "csv";

            Assert.AreEqual(testInputDefault.sampleRate, sampleRateActual);
            Assert.AreEqual(testInputDefault.outputType, outputTypeActual);
            Assert.AreEqual(testInputDefault.outputDirectory, outputDirectoryActual);
        }
    }
}