using System;
using System.Collections.Generic;
using System.Text;

namespace BackgroundWorker.Worker
{
    class Calculate : ICalculate
    {
		public bool AnimalSpeed(double Lat1, double Long1, double Lat2, double Long2, DateTimeOffset timestamp1,DateTimeOffset timestamp2)
		{

			// Convert degrees to radians
			Lat1 = Lat1 * Math.PI / 180.0;
			Long1 = Long1 * Math.PI / 180.0;

			Lat2 = Lat2 * Math.PI / 180.0;
			Long2 = Long2 * Math.PI / 180.0;

			// radius of earth in metres
			double r = 6378100;

			// P
			double rho1 = r * Math.Cos(Lat1);
			double z1 = r * Math.Sin(Lat1);
			double x1 = rho1 * Math.Cos(Long1);
			double y1 = rho1 * Math.Sin(Long1);

			// Q
			double rho2 = r * Math.Cos(Lat2);
			double z2 = r * Math.Sin(Lat2);
			double x2 = rho2 * Math.Cos(Long2);
			double y2 = rho2 * Math.Sin(Long2);

			// Dot product
			double dot = (x1 * x2 + y1 * y2 + z1 * z2);
			double cos_theta = dot / (r * r);

			double theta = Math.Acos(cos_theta);

			// Distance in Metres
			double dist = r * theta;

			//Timestamp date time offset
			double time_s = (timestamp1 - timestamp2) / 1000.0;

			double speed_mps = dist / time_s;

			double speed_kph = (speed_mps * 3600.0) / 1000.0;

			if(speed_kph<35)
			{
				return true;
			}
			else
			{
				return false;
			}
			
		}
	}
}
