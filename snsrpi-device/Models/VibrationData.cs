using System;
using System.Collections.Generic;
using System.Collections;
using Sensr.CX;
using Sensr.Utils;

namespace snsrpi.Models
{

	public class VibrationData
	{
		public DateTime time;
		public double accel_x;
		public double accel_y;
		public double accel_z;
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