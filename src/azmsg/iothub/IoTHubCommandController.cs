using async_enumerable_dotnet;
using azmsg.common;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Producer;
using Azure.Storage.Blobs;
using Microsoft.Azure.Devices.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.ComponentModel.Design;
//using System.CommandLine.DragonFruit;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace azmsg.iothub
{
    class IoTHubCommandController : ICommandController
    {

        private ConfigService service;

        private IoTHubContext CurrentContext
        {
            get
            {
                var config = service.LoadConfig();
                return config.IoTHubContexts[config.CurrentIoTHubContext];
            }
        }

        private string CurrentContextName
        {
            get
            {
                var config = service.LoadConfig();
                return config.CurrentIoTHubContext;
            }
        }

        public Command CreateCommand()
        {
            var iothubCommand = new Command("iothub");


            // set-context
            var iotHubContextCommand = new Command("set-context");
            iotHubContextCommand.AddArgument(new Argument<string>("name"));
            iotHubContextCommand.Add(new Option<string>("--eventhub-endpoint"));
            iotHubContextCommand.Add(new Option<string>("--eventhub-name"));
            iotHubContextCommand.Add(new Option<string>("--storage-connection"));
            iotHubContextCommand.Add(new Option<string>("--consumer-group"));
            iotHubContextCommand.Add(new Option<string>("--device-connection-string"));
            iotHubContextCommand.Handler = CommandHandler.Create<string, string, string, string, string, string>(SetContext);
            iothubCommand.AddCommand(iotHubContextCommand);

            // get-context
            var iotHubGetContextCommand = new Command("get-context");
            iotHubGetContextCommand.Add(new Option<string>("--name"));
            iotHubGetContextCommand.Handler = CommandHandler.Create<string>(GetContext);
            iothubCommand.AddCommand(iotHubGetContextCommand);

            // use-context
            var iotHubUseContextCommand = new Command("use-context");
            iotHubUseContextCommand.AddArgument(new Argument("context-name"));
            iotHubUseContextCommand.Handler = CommandHandler.Create<string>(UseContext);           
            iothubCommand.AddCommand(iotHubUseContextCommand);

            // message
            var messageCommand = new Command("message");        
            messageCommand.Add(new Option<int>("--limit", "number of messages to retrieve"));
            messageCommand.Add(new Option<int>("--message-timeout", "message timeout in seconds"));
            messageCommand.Add(new Option("--follow", "stream latest messages"));
            messageCommand.Add(new Option<string>("--context", "context for reading messages"));
            messageCommand.Handler = CommandHandler.Create<bool, int, int, string>(WatchMessage);
            iothubCommand.AddCommand(messageCommand);

            // d2c
            var d2cCommand = new Command("d2c");
            d2cCommand.Add(new Option<string>("--message", "text to send"));
            d2cCommand.Add(new Option<string>("--from-file", "send contents of file"));
            d2cCommand.Add(new Option<TransportType>("--transport-type", "Override the default, options are as per Microsoft.Azure.Devices.Client.TransportType"));
            d2cCommand.Handler = CommandHandler.Create<string, string, TransportType>(Device2CloudMessage);
            iothubCommand.AddCommand(d2cCommand);

            // simulate
            var simulateCommand = new Command("simulate-device");
            simulateCommand.Add(new Option<string>("--device-type", () => { return "temperature"; }, "Supported types: [\"temperature\", \"temperature_pair\"]"));
            simulateCommand.Add(new Option<int>("--message-delay", () => { return 5000; }, "Delay between messages in milliseconds. Default 5 seconds"));
            simulateCommand.Add(new Option<int>("--n", () => { return -1; }, "Number of messages to send. Default is unbounded"));
            simulateCommand.Add(new Option<string>("--ca-file", "CA cert to be trusted"));
            simulateCommand.Add(new Option<string[]>("--device-context", "List of context names to generate simulated data for. Separate context names with spaces"));
            simulateCommand.Add(new Option<string>("--pattern", () => { return "none"; }, "Supported patterns: [\"none\", \"sine\"]"));
            simulateCommand.Add(new Option<string>("--pattern-period", "Cycle time for pattern generator"));
            simulateCommand.Handler = CommandHandler.Create<string, int, int, string, string[], string, string>(SimulateDevice);
            iothubCommand.Add(simulateCommand);

            
            return iothubCommand;
        }

        public IoTHubCommandController(ConfigService service)
        {
            this.service = service;
        }

        public async Task Device2CloudMessage(string message, string fromFile, TransportType transportType)
        {
            var pc = new IoTHubProducerCommands(CurrentContext, service);

            if (fromFile != null)
            {
                await pc.Device2CloudMessageFromFile(fromFile, transportType);
            }
            else
            {                
                await pc.Device2CloudSingleMessage(message, transportType);
            }            
        }

        public async Task SimulateDevice(string deviceType, int messageDelay, int n, string caFile, string[] deviceContext, string pattern, string patternPeriod)
        {
            
            var producer = new IoTHubProducerCommands(CurrentContext, service);


            if (!String.IsNullOrWhiteSpace(caFile))
            {
                Console.WriteLine($"Registering CA certificate: {caFile}");
                var caCert = await CertificateUtilities.LoadPemCACertificate(caFile, null);
                CertificateUtilities.RegisterCert2(caCert);
            }

            if (string.Compare(deviceType, "temperature", true) == 0)
            {
                if (deviceContext == null || deviceContext.Length == 0)
                {
                    await producer.
                    SimulateTemperatureSensor(messageDelay, n, pattern, patternPeriod);
                }
                else
                {
                    await producer.SimulateMultipleTemperatureSensors(deviceContext, messageDelay, n, pattern, patternPeriod);
                }
                
            }
            else if (string.Compare(deviceType, "temperature_pair", true) == 0)
            {
                await producer.
                    SimulateTemperatureSensorPair(messageDelay, n);
            }
            else
            {
                Console.WriteLine($"No matching device type: {deviceType}");
            }

            await Task.CompletedTask;
        }


        public async Task WatchMessage(bool follow, int limit, int messageTimeout, string context)
        {
            var listenContext = CurrentContext;

            if (!String.IsNullOrWhiteSpace(context))
            {
                var config = service.LoadConfig();

                if (!config.IoTHubContexts.TryGetValue(context, out listenContext))
                {
                    throw new Exception($"Invalid context name: {context}");
                }
            }
            var cc = new IoTHubConsumerCommands(CurrentContext, service);


            await cc.WatchMessage(follow, limit, messageTimeout);

            //Console.WriteLine($"WatchMessage with follow {follow}, limit {limit} and timeout {messageTimeout}");

            //var config = service.LoadConfig();

            //var eu = new EventHubUtilities();

            //try
            //{
            //    if (follow)
            //    {
            //        //await FollowMessages(messageTimeout);
            //        await eu.FollowMessages(messageTimeout, CurrentContext.ConsumerGroup, CurrentContext.ConnectionString, CurrentContext.EventHubName);
            //    }
            //    else
            //    {
            //        //await WatchMessages(limit, messageTimeout);
            //        await eu.WatchMessagesWithLimit(limit, messageTimeout, CurrentContext.ConsumerGroup, CurrentContext.ConnectionString, CurrentContext.EventHubName);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"{ex}");
            //}

        }

        //private async Task WatchMessages(int limit, int messageTimeout)
        //{

        //    string consumerGroup = EventHubConsumerClient.DefaultConsumerGroupName;

        //    if (currentContext.ConsumerGroup != null)
        //    {
        //        consumerGroup = currentContext.ConsumerGroup;
        //    }

        //    await using (var consumer = new EventHubConsumerClient(consumerGroup, currentContext.ConnectionString, currentContext.EventHubName))
        //    {
        //        EventPosition startingPosition = EventPosition.Latest;
        //        using var cancellationSource = new System.Threading.CancellationTokenSource();

        //        int maxWaitTime = messageTimeout == 0 ? 30 : messageTimeout;
        //        cancellationSource.CancelAfter(TimeSpan.FromSeconds(maxWaitTime));

        //        string[] partitionIds = await consumer.GetPartitionIdsAsync();

        //        var partitions = new IAsyncEnumerable<PartitionEvent>[partitionIds.Length];

        //        for (int i = 0; i < partitionIds.Length; i++)
        //        {
        //            partitions[i] = consumer.ReadEventsFromPartitionAsync(partitionIds[i], startingPosition, cancellationSource.Token);
        //        }

        //        var mergedPartitions = AsyncEnumerable.Merge<PartitionEvent>(partitions);

        //        var maxMessages = Math.Max(1, limit);

        //        try
        //        {

        //            Console.WriteLine("Waiting for messages..");
        //            //for (int i = 0; i < maxMessages; i++)
        //            //{
        //            //    var singlePe = mergedPartitions.Take<PartitionEvent>(maxMessages);

        //            //    await foreach (var pe in singlePe)
        //            //    {
        //            //        Console.WriteLine($"Event received {Encoding.UTF8.GetString(pe.Data.Body.ToArray())}");
        //            //    }
        //            //}

        //            int messageCount = 0;
        //            await foreach (var pe in mergedPartitions.Take<PartitionEvent>(maxMessages))
        //            {

        //                Console.WriteLine($"Event received on partition {pe.Partition.PartitionId} with body {Encoding.UTF8.GetString(pe.Data.Body.ToArray())}");
        //                messageCount = messageCount + 1;
        //                if (messageCount >= maxMessages)
        //                {
        //                    Console.WriteLine($"Total messages received: {messageCount}");
        //                    break;
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"{ex}");
        //        }
        //        //await foreach (PartitionEvent receivedEvent in consumer.ReadEventsFromPartitionAsync(partitionId, startingPosition, cancellationSource.Token))
        //        //{
        //        //    // At this point, the loop will wait for events to be available in the partition.  When an event
        //        //    // is available, the loop will iterate with the event that was received.  Because we did not
        //        //    // specify a maximum wait time, the loop will wait forever unless cancellation is requested using
        //        //    // the cancellation token.

        //        //}
        //    }
        //}


        //private async Task FollowMessages(int messageTimeout)
        //{

        //    string consumerGroup = EventHubConsumerClient.DefaultConsumerGroupName;

        //    if (currentContext.ConsumerGroup != null)
        //    {
        //        consumerGroup = currentContext.ConsumerGroup;
        //    }

        //    await using (var consumer = new EventHubConsumerClient(consumerGroup, currentContext.ConnectionString, currentContext.EventHubName))
        //    {
        //        EventPosition startingPosition = EventPosition.Latest;
        //        using var cancellationSource = new System.Threading.CancellationTokenSource();

        //        //int maxWaitTime = messageTimeout == 0 ? 30 : messageTimeout;
        //        //cancellationSource.CancelAfter(TimeSpan.FromSeconds(maxWaitTime));

        //        string[] partitionIds = await consumer.GetPartitionIdsAsync();

        //        var partitions = new IAsyncEnumerable<PartitionEvent>[partitionIds.Length];

        //        for (int i = 0; i < partitionIds.Length; i++)
        //        {
        //            partitions[i] = consumer.ReadEventsFromPartitionAsync(partitionIds[i], startingPosition, cancellationSource.Token);
        //        }

        //        var mergedPartitions = AsyncEnumerable.Merge<PartitionEvent>(partitions);



        //        try
        //        {

        //            Console.WriteLine("Following messages..");
        //            //for (int i = 0; i < maxMessages; i++)
        //            //{
        //            //    var singlePe = mergedPartitions.Take<PartitionEvent>(maxMessages);

        //            //    await foreach (var pe in singlePe)
        //            //    {
        //            //        Console.WriteLine($"Event received {Encoding.UTF8.GetString(pe.Data.Body.ToArray())}");
        //            //    }
        //            //}

        //            int messageCount = 0;
        //            await foreach (var pe in mergedPartitions)
        //            {

        //                Console.WriteLine($"Event received on partition {pe.Partition.PartitionId} with body {Encoding.UTF8.GetString(pe.Data.Body.ToArray())}");
        //                messageCount = messageCount + 1;

        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"{ex}");
        //        }
        //        //await foreach (PartitionEvent receivedEvent in consumer.ReadEventsFromPartitionAsync(partitionId, startingPosition, cancellationSource.Token))
        //        //{
        //        //    // At this point, the loop will wait for events to be available in the partition.  When an event
        //        //    // is available, the loop will iterate with the event that was received.  Because we did not
        //        //    // specify a maximum wait time, the loop will wait forever unless cancellation is requested using
        //        //    // the cancellation token.

        //        //}
        //    }
        //}


        public void SetContext(string name,
            string eventhubEndpoint,
            string eventHubName,
            string storageConnection,
            string consumerGroup,
            string deviceConnectionString)
        {
            Console.WriteLine($"Setting context {name} with eventhub endpoint {eventhubEndpoint} and hub {eventHubName}");
            var config = service.LoadConfig();
            IoTHubContext ctxt = null;
            if (!config.IoTHubContexts.TryGetValue(name, out ctxt))
            {
                ctxt = new IoTHubContext();
            }

            if (eventhubEndpoint != null)
            {
                ctxt.EventHubEndpoint = eventhubEndpoint;
            }

            if (eventHubName != null)
            {
                ctxt.EventHubName = eventHubName;
            }

            if (storageConnection != null)
            {
                ctxt.StorageConnectionString = storageConnection;
            }

            if (consumerGroup != null)
            {
                ctxt.ConsumerGroup = consumerGroup;
            }

            if (deviceConnectionString != null)
            {
                ctxt.DeviceConnectionString = deviceConnectionString;
            }

            config.IoTHubContexts[name.ToLower()] = ctxt;

            service.UpdateConfig(config);
        }

        public void UseContext(string contextName)
        {
            var config = service.LoadConfig();

            if (config.IoTHubContexts.ContainsKey(contextName))
            {
                config.CurrentIoTHubContext = contextName;
                service.UpdateConfig(config);
                Console.WriteLine($"Using context {contextName}");
            }


        }

        public void GetContext(string name)
        {
            var config = service.LoadConfig();

            var options = new JsonSerializerOptions()
            {
                WriteIndented = true
            };

            if (String.IsNullOrWhiteSpace(name))
            {
                foreach (var kv in config.IoTHubContexts)
                {
                    Console.WriteLine($"{kv.Key} : {JsonSerializer.Serialize(kv.Value, options)}");
                }

                Console.WriteLine($"\nCurrent context: {CurrentContextName}");
            }
            else
            {
                if (config.IoTHubContexts.ContainsKey(name))
                {
                    service.UpdateConfig(config);
                    Console.WriteLine(JsonSerializer.Serialize(config.IoTHubContexts[name], options));
                }
                else
                {
                    Console.WriteLine($"Context {name} not found");
                }
            }

        }



        public async Task Send(string message, string messageFile)
        {
            var config = service.LoadConfig();

            try
            {
                await using (var producerClient = new EventHubProducerClient(CurrentContext.EventHubEndpoint, CurrentContext.EventHubName))
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}