using azmsg.common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace azmsg.eventhub
{
    class EventHubConsumerCommands
    {
        private EventHubContext currentContext;
        private ConfigService service;

        public EventHubConsumerCommands(EventHubContext context, ConfigService service)
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
                    await eu.FollowMessages(messageTimeout, currentContext.ConsumerGroup, currentContext.ConnectionString, currentContext.EventHubName);
                }
                else
                {
                    //await WatchMessages(limit, messageTimeout);
                    await eu.WatchMessagesWithLimit(limit, messageTimeout, currentContext.ConsumerGroup, currentContext.ConnectionString, currentContext.EventHubName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex}");
            }

        }
    }
}
