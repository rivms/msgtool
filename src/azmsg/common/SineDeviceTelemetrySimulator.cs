using System;
using System.Collections.Generic;
using System.Text;

namespace azmsg.common
{

    

    class SineDeviceTelemetrySimulator : IDeviceTelemetrySimulator
    {
        private readonly bool celcius;
        private readonly int period;
        private readonly double amplitude;
        

        public SineDeviceTelemetrySimulator(bool celcius)
        {
            this.celcius = celcius;
            this.period = 20;
            this.amplitude = 10;
            
        }

        public IEnumerable<double> Measure()
        {
            double avgTemperature = celcius ? 21.11D : 70.0D;
            double radiansPerIncrement;
            double currentRadians;

            var rand = new Random();

            radiansPerIncrement = Math.PI * 2 / (double)period;
            currentRadians = rand.NextDouble() * Math.PI * 2;      
            
            while (true)
            {
                double seasonalVariation = amplitude * Math.Sin(currentRadians);
                double randomVariation = rand.NextDouble() * 4 - 3;
                double currentTemperature = avgTemperature + seasonalVariation + randomVariation;

                currentRadians += radiansPerIncrement;

                if (currentRadians > 2 * Math.PI)
                {
                    currentRadians = 0;
                }

                yield return currentTemperature;
            }
        }
    }
}
