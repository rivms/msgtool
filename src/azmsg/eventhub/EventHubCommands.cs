using azmsg.eventhub;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
//using System.CommandLine.DragonFruit;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace azmsg
{
    class EventHubCommands2
    {

        private ConfigService service;
        private EventHubContext currentContext; 

        public EventHubCommands2(ConfigService service)
        {
            this.service = service;
        }

        public void SetContext(string name, string connectionString, string eventHubName)
        {
            Console.WriteLine($"Setting context {name} with connection {connectionString} and hub {eventHubName}");
            var config = service.LoadConfig();
            EventHubContext ctxt = null;
            if (!config.EventHubContexts.TryGetValue(name, out ctxt)) 
            {
                ctxt = new EventHubContext();
            }

            if (connectionString != null)
            { 
                ctxt.ConnectionString = connectionString;
            }

            if (eventHubName != null)
            {
                ctxt.EventHubName = eventHubName;
            }

            config.EventHubContexts[name.ToLower()] = ctxt;

            service.UpdateConfig(config);
        }

        public void UseContext(string contextName)
        {
            var config = service.LoadConfig();

            if (config.EventHubContexts.TryGetValue(contextName, out currentContext))
            {
                config.CurrentEventHubContext = contextName;
                service.UpdateConfig(config);
                Console.WriteLine($"Using context {contextName}");
            }

            
        }

        public async Task Send(string message)
        {
            var config = service.LoadConfig();

            this.currentContext = config.EventHubContexts[config.CurrentEventHubContext];

            //await RunAsync(currentContext.ConnectionString, currentContext.EventHubName);
            try
            {
                await using (var producerClient = new EventHubProducerClient(currentContext.ConnectionString, currentContext.EventHubName))
                {
                    // Create a batch of events 
                    using EventDataBatch eventBatch = await producerClient.CreateBatchAsync();

                    // Add events to the batch. An event is a represented by a collection of bytes and metadata. 
                    eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(message)));
                    //eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes("Second event")));
                    //eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes("Third event")));

                    // Use the producer client to send the batch of events to the event hub
                    await producerClient.SendAsync(eventBatch);
                    Console.WriteLine("Message sent");
                }
            } catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

    }
}