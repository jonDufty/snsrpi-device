//using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using snsrpi.Models;
using snsrpi.Services;
using Sensr.CX;
using Sensr.Utils;
using System.IO;
using System.Diagnostics;

namespace snsrpi.Tests
{
    [TestClass]
    public class LoggerTests
    {
        [TestMethod]
        public void LoggerCreateAndConnectTest()
        {
            CXCom cx = new();
            List<CXDevice> devs = cx.ListDevices();
            if (devs.Count < 1)
            {
                Assert.Fail("No devices found - test is invalid");
            } else
            {
                var dev = devs[0];
                Logger logger = new(cx, dev);
                var result = logger.Connect();
                Assert.IsTrue(result);
                Assert.IsTrue(cx.IsConnected());

            }
        }

        [TestMethod]
        public void LoggerCreateOverloadTest()
        {
            CXCom cx = new();
            List<CXDevice> devs = cx.ListDevices();
            if (devs.Count < 1)
            {
                Assert.Fail("No devices found - test is invalid");
            }
            else
            {
                //Create input file
                var ParentDir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
                var inputpath = Path.Combine(ParentDir, "resources", "tests", "input_test.json");
                InputData testInputDefault = InputData.InitFromJSON(inputpath);

                //Create new logger object
                var dev = devs[0];
                Logger logger = new(cx, dev, testInputDefault);
                var result = logger.Connect();
                Assert.IsTrue(result);
                Assert.IsTrue(cx.IsConnected());
            }
        }

        [TestMethod]
        public void StartAcquisitionTest()
        {
            CXCom cx = new();
            CXDevice dev = cx.ListDevices()[0];
            Logger logger = new(cx, dev);
            logger.Connect();

            //Start acqusition for 10s. Test to see if exception gets thrown
            logger.StartAcquisition();
        }


        [TestMethod]
        public void TestDisconnect()
        {
            CXCom cx = new();
            List<CXDevice> devs = cx.ListDevices();
            if (devs.Count < 1)
            {
                Assert.Fail("No devices found - test is invalid");
            }
            else
            {
                var dev = devs[0];
                Logger logger = new(cx, dev);
                var result = logger.Connect();
                if (!result)
                {
                    Assert.Fail("Connection Failed");
                } else
                {
                    Assert.IsTrue(cx.IsConnected());
                    cx.Disconnect();
                    Assert.IsFalse(cx.IsConnected());
                }

            }
        }

        [TestMethod]
        public void AcqusitionTimerTest()
        {
            CXCom cx = new();
            CXDevice dev = cx.ListDevices()[0];
            Logger logger = new(cx, dev);
            logger.Connect();

            //Start acqusition for 10s. Test to see if exception gets thrown
            Stopwatch timer = new();
            int limit = 5;
            timer.Start();
            logger.StartAcquisition();
            timer.Stop();
            Assert.AreEqual(timer.Elapsed.TotalSeconds, (double)limit, 0.5);

        }
    }
}