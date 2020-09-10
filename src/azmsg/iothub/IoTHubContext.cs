using System;
using System.CommandLine;
using System.CommandLine.Invocation;
//using System.CommandLine.DragonFruit;
using System.IO;

namespace azmsg.iothub
{
    [Serializable]
    class IoTHubContext
    {

        public IoTHubContext()
        {

        }

        public string DeviceConnectionString { get; set; }

        public string ConnectionString { get; set; }
        public string StorageConnectionString { get; set; }

        public string ContainerName { get; set; }

        public string ConsumerGroup { get; set; }
        public string EventHubName { get; set; }

        public bool Enabled { get; set; }
    }
}