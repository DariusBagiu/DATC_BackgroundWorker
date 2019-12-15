using System;

namespace BackgroundWorker.Worker
{
	public interface ICalculate
   {
		public bool AnimalSpeed(double Lat1, double Long1, double Lat2, double Long2, DateTimeOffset timestamp1, DateTimeOffset timestamp2);

	}
}
