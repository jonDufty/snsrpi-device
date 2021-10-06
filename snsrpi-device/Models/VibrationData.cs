using System;
using System.Collections.Generic;
using System.Collections;
using Sensr.CX;
using Sensr.Utils;

namespace snsrpi.Models
{

    public class VibrationData
    {
        public DateTime time {get;}
        public double accel_x {get;} 
        public double accel_y {get;}
        public double accel_z {get;}
        public int reconstructed;


        // Constructor class takes in list of samples and populates data fields

        public VibrationData(DateTime _time, double _x, double _y, double _z)
        {
            time = _time;
            accel_x = _x;
            accel_y = _y;
            accel_z = _z;
        }

    }
}