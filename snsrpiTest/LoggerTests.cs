//using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using snsrpi.Models;
using snsrpi.Interfaces;
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
        LoggerManagerService loggerManager;

        [TestInitialize]
        public void TestInitialize()
        {
            // loggerManager = new(true);
        }

        [TestMethod]
        public void TestListDevices()
        {
            var devices = loggerManager.ListDevices();
            Assert.AreEqual(4, devices.Count);
        }

        [TestMethod]
        public void TestStartDevice()
        {
            var device = loggerManager.GetDevice(loggerManager.ListDevices()[0]);
            device.Start();
            Thread.Sleep(5000);
            loggerManager.StopAllDevices();
        }
    }
}