using azmsg.common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace azmsg.iothub
{
    class IoTHubConsumerCommands
    {
        private IoTHubContext currentContext;
        private ConfigService service;

        public IoTHubConsumerCommands(IoTHubContext context, ConfigService service)
        {
            this.currentContext = context;
            this.service = service;
        }

        public async Task WatchMessage(bool follow, int limit, int messageTimeout)
        {
            Console.WriteLine($"WatchMessage with follow {follow}, limit {limit} and timeout {messageTimeout}");

            var config = service.LoadConfig();

            var eu = new EventHubUtilities();

            try
            {
                if (follow)
                {
                    //await FollowMessages(messageTimeout);
                    await eu.FollowMessages(messageTimeout, currentContext.ConsumerGroup, 
                        currentContext.EventHubEndpoint, 
                        currentContext.EventHubName);
                }
                else
                {
                    //await WatchMessages(limit, messageTimeout);
                    await eu.WatchMessagesWithLimit(limit, messageTimeout, 
                        currentContext.ConsumerGroup, 
                        currentContext.EventHubEndpoint, currentContext.EventHubName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex}");
            }

        }
    }
}
