using System;
using System.Collections.Generic;
using System.Text;

namespace azmsg.common
{

    class DeviceTelemetrySimulator : IDeviceTelemetrySimulator
    {
        private readonly bool celcius;

        public DeviceTelemetrySimulator(bool celcius)
        {
            this.celcius = celcius;
        }

        public IEnumerable<double> Measure()
        {
            double avgTemperature = celcius ? 21.11D : 70.0D;

            var rand = new Random();      

            while (true)
            {
                double currentTemperature = avgTemperature + rand.NextDouble() * 4 - 3;
                yield return currentTemperature; 
            }
        }
    }
}
