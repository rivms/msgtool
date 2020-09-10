using System;
using System.CommandLine;
using System.CommandLine.Invocation;
//using System.CommandLine.DragonFruit;
using System.IO;

namespace azmsg.eventhub
{
    [Serializable]
    class EventHubContext
    {

        public EventHubContext()
        {

        }

        public string ConnectionString { get; set; }
        public string EventHubName { get; set; }
        public string ConsumerGroup { get; set; }

        public bool Enabled { get; set; }
    }
}