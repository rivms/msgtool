using azmsg.eventhub;
using azmsg.iothub;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
//using System.CommandLine.DragonFruit;
using System.IO;
using System.Security.Cryptography;


namespace azmsg
{
    [Serializable]
    class AzMsgConfig
    {

        public AzMsgConfig()
        {
            EventHubContexts = new Dictionary<string, EventHubContext>();
            IoTHubContexts = new Dictionary<string, IoTHubContext>();
        }

        public string ConfigVersion { get { return "0.1"; } }

        public string CurrentEventHubContext { get; set;  }

        public string CurrentIoTHubContext { get; set; }

        public IDictionary<string, EventHubContext> EventHubContexts { get; set;}

        public IDictionary<string, IoTHubContext> IoTHubContexts { get; set; }
    }
}