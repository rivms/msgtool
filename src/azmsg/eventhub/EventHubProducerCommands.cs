using azmsg.common;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace azmsg.eventhub
{
    class EventHubProducerCommands
    {
        private EventHubContext currentContext;
        private ConfigService service;

        public EventHubProducerCommands(EventHubContext context, ConfigService service)
        {
            this.currentContext = context;
            this.service = service;
        }

        public async Task SimulateTemperatureSensor(int messageDelay, int n)
        {
            var deviceSimulator = new DeviceTelemetrySimulator();

            int messageCount = 0;
            foreach (var temperature in deviceSimulator.Temperature(true))
            {
                var dataPoint = new
                {
                    Temperature = temperature
                };

                var messageString = JsonSerializer.Serialize(dataPoint);
                await Send(messageString, null);


                if (n > 0)
                {
                    messageCount = messageCount + 1;
                    if (messageCount >= n)
                    {
                        Console.WriteLine("All messages sent");
                        break;
                    }
                }

                await Task.Delay(messageDelay);
            }
          
            await Task.CompletedTask;
        }

        public async Task Send(string message, string eventHubName)
        {
            var config = service.LoadConfig();

            this.currentContext = config.EventHubContexts[config.CurrentEventHubContext];

            var hubName = currentContext.EventHubName;

            if (eventHubName != null)
            {
                hubName = eventHubName;
            }

            //await RunAsync(currentContext.ConnectionString, currentContext.EventHubName);
            try
            {
                await using (var producerClient = new EventHubProducerClient(currentContext.ConnectionString, hubName))
                {
                    // Create a batch of events 
                    using EventDataBatch eventBatch = await producerClient.CreateBatchAsync();

                    // Add events to the batch. An event is a represented by a collection of bytes and metadata. 
                    eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(message)));
                    //eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes("Second event")));
                    //eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes("Third event")));

                    // Use the producer client to send the batch of events to the event hub
                    await producerClient.SendAsync(eventBatch);
                    Console.WriteLine($"Message sent to event hub {hubName}: {message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public async Task SendFromFile(string path, string eventHubName)
        {
            var config = service.LoadConfig();

            this.currentContext = config.EventHubContexts[config.CurrentEventHubContext];

            var hubName = currentContext.EventHubName;

            if (eventHubName != null)
            {
                hubName = eventHubName;
            }

            //await RunAsync(currentContext.ConnectionString, currentContext.EventHubName);
            try
            {
                var message = File.ReadAllText(path);
                await using (var producerClient = new EventHubProducerClient(currentContext.ConnectionString, hubName))
                {
                    // Create a batch of events 
                    using EventDataBatch eventBatch = await producerClient.CreateBatchAsync();

                    // Add events to the batch. An event is a represented by a collection of bytes and metadata. 
                    eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(message)));
                    //eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes("Second event")));
                    //eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes("Third event")));

                    // Use the producer client to send the batch of events to the event hub
                    await producerClient.SendAsync(eventBatch);
                    Console.WriteLine($"Message sent to event hub {hubName}: {message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
