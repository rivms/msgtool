using async_enumerable_dotnet;
using Azure.Messaging.EventHubs.Consumer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace azmsg.common
{
    class EventHubUtilities
    {
        public async Task WatchMessagesWithLimit(int limit, int messageTimeout, string consumerGroup, string connectionString, string eventHubName)
        {

            string consumerGroupWithDefault = EventHubConsumerClient.DefaultConsumerGroupName;

            if (consumerGroup != null)
            {
                consumerGroupWithDefault = consumerGroup;
            }

            await using (var consumer = new EventHubConsumerClient(consumerGroupWithDefault, connectionString, eventHubName))
            {
                EventPosition startingPosition = EventPosition.Latest;
                using var cancellationSource = new System.Threading.CancellationTokenSource();

                int maxWaitTime = messageTimeout == 0 ? 30 : messageTimeout;
                cancellationSource.CancelAfter(TimeSpan.FromSeconds(maxWaitTime));

                string[] partitionIds = await consumer.GetPartitionIdsAsync();

                var partitions = new IAsyncEnumerable<PartitionEvent>[partitionIds.Length];

                for (int i = 0; i < partitionIds.Length; i++)
                {
                    partitions[i] = consumer.ReadEventsFromPartitionAsync(partitionIds[i], startingPosition, cancellationSource.Token);
                }

                var mergedPartitions = AsyncEnumerable.Merge<PartitionEvent>(partitions);

                var maxMessages = Math.Max(1, limit);

                try
                {

                    Console.WriteLine("Waiting for messages..");

                    int messageCount = 0;
                    await foreach (var pe in mergedPartitions.Take<PartitionEvent>(maxMessages))
                    {

                        Console.WriteLine($"Event received on partition {pe.Partition.PartitionId} with body {Encoding.UTF8.GetString(pe.Data.Body.ToArray())}");
                        messageCount = messageCount + 1;
                        if (messageCount >= maxMessages)
                        {
                            Console.WriteLine($"Total messages received: {messageCount}");
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex}");
                }
            }
        }

        public async Task FollowMessages(int messageTimeout, string consumerGroup, string connectionString, string eventHubName)
        {

            string consumerGroupWithDefault = EventHubConsumerClient.DefaultConsumerGroupName;

            if (consumerGroup != null)
            {
                consumerGroupWithDefault = consumerGroup;
            }

            await using (var consumer = new EventHubConsumerClient(consumerGroupWithDefault, connectionString, eventHubName))
            {
                EventPosition startingPosition = EventPosition.Latest;
                using var cancellationSource = new System.Threading.CancellationTokenSource();

                //int maxWaitTime = messageTimeout == 0 ? 30 : messageTimeout;
                //cancellationSource.CancelAfter(TimeSpan.FromSeconds(maxWaitTime));

                string[] partitionIds = await consumer.GetPartitionIdsAsync();

                var partitions = new IAsyncEnumerable<PartitionEvent>[partitionIds.Length];

                for (int i = 0; i < partitionIds.Length; i++)
                {
                    partitions[i] = consumer.ReadEventsFromPartitionAsync(partitionIds[i], startingPosition, cancellationSource.Token);
                }

                var mergedPartitions = AsyncEnumerable.Merge<PartitionEvent>(partitions);



                try
                {

                    Console.WriteLine("Following messages..");

                    int messageCount = 0;
                    await foreach (var pe in mergedPartitions)
                    {

                        Console.WriteLine($"Event received on partition {pe.Partition.PartitionId} with body {Encoding.UTF8.GetString(pe.Data.Body.ToArray())}");
                        messageCount = messageCount + 1;

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex}");
                }

            }
        }
    }
}
