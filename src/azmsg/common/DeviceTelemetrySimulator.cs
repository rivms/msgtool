using System;
using System.Collections.Generic;
using System.Text;

namespace azmsg.common
{
    class DeviceTelemetrySimulator
    {
        public IEnumerable<double> Temperature(bool celcius)
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
