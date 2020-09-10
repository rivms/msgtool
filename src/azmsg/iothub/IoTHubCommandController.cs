using async_enumerable_dotnet;
using azmsg.common;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Producer;
using Azure.Storage.Blobs;
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

        public Command CreateCommand()
        {
            var iothubCommand = new Command("iothub");


            // set-context
            var iotHubContextCommand = new Command("set-context");
            iotHubContextCommand.AddArgument(new Argument<string>("name"));
            iotHubContextCommand.Add(new Option<string>("--connection-string"));
            iotHubContextCommand.Add(new Option<string>("--eventhub-name"));
            iotHubContextCommand.Add(new Option<string>("--storage-connection"));
            iotHubContextCommand.Add(new Option<string>("--consumer-group"));
            iotHubContextCommand.Add(new Option<string>("--device-connection-string"));
            iotHubContextCommand.Handler = CommandHandler.Create<string, string, string, string, string, string>(SetContext);
            iothubCommand.AddCommand(iotHubContextCommand);


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
            messageCommand.Handler = CommandHandler.Create<bool, int, int>(WatchMessage);
            iothubCommand.AddCommand(messageCommand);

            // d2c
            var d2cCommand = new Command("d2c");
            d2cCommand.Add(new Option<string>("--message", "text to send"));
            d2cCommand.Add(new Option<string>("--from-file", "send contents of file"));
            d2cCommand.Handler = CommandHandler.Create<string, string>(Device2CloudMessage);
            iothubCommand.AddCommand(d2cCommand);
            return iothubCommand;
        }

        public IoTHubCommandController(ConfigService service)
        {
            this.service = service;
        }

        public async Task Device2CloudMessage(string message, string fromFile)
        {
            var pc = new IoTHubProducerCommands(CurrentContext, service);

            if (fromFile != null)
            {
                await pc.Device2CloudMessageFromFile(fromFile);
            }
            else
            {
                await pc.Device2CloudMessage(message);
            }            
        }

        public async Task WatchMessage(bool follow, int limit, int messageTimeout)
        {
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
            string connectionString,
            string eventHubName,
            string storageConnection,
            string consumerGroup,
            string deviceConnectionString)
        {
            Console.WriteLine($"Setting context {name} with connection {connectionString} and hub {eventHubName}");
            var config = service.LoadConfig();
            IoTHubContext ctxt = null;
            if (!config.IoTHubContexts.TryGetValue(name, out ctxt))
            {
                ctxt = new IoTHubContext();
            }

            if (connectionString != null)
            {
                ctxt.ConnectionString = connectionString;
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

        public async Task Send(string message, string messageFile)
        {
            var config = service.LoadConfig();

            try
            {
                await using (var producerClient = new EventHubProducerClient(CurrentContext.ConnectionString, CurrentContext.EventHubName))
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