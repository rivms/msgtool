using System;
using System.Collections.Generic;
using System.Text;

namespace azmsg.common
{
    public enum SimulatorPattern
    {
        None,
        Sine
    }

    class DeviceTelemetryFactory
    {
        public static IDeviceTelemetrySimulator CreateTemperatureSimulator(string pattern, string patternType, bool celcius)
        {
            IDeviceTelemetrySimulator sim;

            if (String.Compare(pattern, "sine", true)==0)
            {
                sim = new SineDeviceTelemetrySimulator(celcius);
            }
            else
            {
                sim = new DeviceTelemetrySimulator(celcius);
            }
            return sim;
        }
    }
}
