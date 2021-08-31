using System;
using System.Collections.Generic;
using System.Collections;
using Sensr.CX;
using Sensr.Utils;

namespace snsrpi.Models
{

	public class VibrationData
	{
		public List<string> time;
		public List<double> accel_x;
		public List<double> accel_y;
		public List<double> accel_z;
		public int reconstructed;


		// Constructor class takes in list of samples and populates data fields
		public VibrationData()
		{
			time 	= new List<string>();
			accel_x = new List<double>();
			accel_y = new List<double>();
			accel_z = new List<double>();
			reconstructed = 0;
		}

		public VibrationData(List<string> _time, List<double> _x, List<double> _y, List<double> _z)
		{
			time = _time;
			accel_x = _x;
			accel_y = _y;
			accel_z = _z;
			reconstructed = 0;
		}

		public VibrationData(DateTime[] _time, double[] _x, double[] _y, double[] _z)
		{
			time = new List<string>();
			accel_x = new List<double>(_x);
			accel_y = new List<double>(_y);
			accel_z = new List<double>(_z);
			for (int i = 0; i < _time.Length; i++)
			{
				time.Add(_time[i].ToString("MM/dd/yyyy HH:mm:ss.ffffff"));

			}
			reconstructed = 0;
		}

		public void AppendSample(DateTime _time, double _x, double _y, double _z)
		{
			time.Add(_time.ToString("MM/dd/yyyy HH:mm:ss.ffffff"));
			accel_x.Add(_x);
			accel_x.Add(_y);
			accel_x.Add(_z);
		}

	}

}