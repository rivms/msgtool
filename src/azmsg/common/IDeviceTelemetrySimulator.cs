using System;
using System.Collections.Generic;
using System.Text;

namespace azmsg.common
{
    interface IDeviceTelemetrySimulator
    {
        IEnumerable<double> Measure();
    }
}
