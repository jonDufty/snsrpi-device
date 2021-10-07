using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.IO;
using Sensr.CX;
using Sensr.Utils;
using snsrpi.Services;

namespace snsrpi.Models
{
    /// <summary>
    /// Health Check object. Contains basic device state 
    /// </summary>
    public class Health
    {
        public string Device_id {get;}
        public List<SensorStatus> Sensors {get;}

        public Health(string device, List<SensorStatus> sensors)
        {
            Device_id = device;
            Sensors = sensors;
        }
        
    }

    public class SensorStatus
    {
        public string Sensor_id {get; set;}
        public bool Active {get; set;}

        public SensorStatus(string sensor, bool isActive)
        {
            Sensor_id = sensor;
            Active = isActive;
        }
    }
}