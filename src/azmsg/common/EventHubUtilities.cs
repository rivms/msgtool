﻿using async_enumerable_dotnet;
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

                        //Console.WriteLine($"Event received on partition {pe.Partition.PartitionId} with body {Encoding.UTF8.GetString(pe.Data.Body.ToArray())}");
                        DisplayMessage(pe);
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

        public static bool IsNumeric(string s)
        {
            decimal d;
            return decimal.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out d);
        }

        public static string FormatPayloadField(string payload)
        {
            if (payload.StartsWith("{"))
            {
                return payload;
            };

            if (IsNumeric(payload))
            {
                return payload;
            }

            return $"\"{payload}\"";
        }

        public void DisplayMessage(PartitionEvent pe)
        {
            var payload = Encoding.UTF8.GetString(pe.Data.Body.ToArray());
            var outputMessage = $@"{{
                ""body"": {FormatPayloadField(payload)},
                ""enqueuedTime"": ""{pe.Data.EnqueuedTime}"",
                ""systemProperties"": {{
                    {pe.Data.SystemProperties.Count}
                }}
                ""properties"": {{ 
                {pe.Data.Properties.Count}
                }}
            }}";

            var sb = new StringBuilder();
            var padding1 = "  ";
            var padding2 = "    ";

            sb.AppendLine("{");
            sb.Append(padding1 + "\"body\": ");
            sb.Append(FormatPayloadField(payload));
            sb.AppendLine(",");
            sb.AppendLine(padding1 + "\"systemProperties\": {");
            foreach(var kv in pe.Data.SystemProperties)
            {
                sb.AppendLine(padding2 + $"\"{kv.Key}\": {FormatPayloadField(kv.Value.ToString())}");
            }
            sb.AppendLine(padding1 + "}");
            sb.AppendLine(padding1 + "\"properties\": {");
            foreach (var kv in pe.Data.Properties)
            {
                sb.AppendLine(padding2 + $"\"{kv.Key}\": \"{kv.Value}\"");
            }
            sb.AppendLine(padding1 + "}");
            sb.AppendLine("}");
            Console.WriteLine(sb);
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

                        //Console.WriteLine($"Event received on partition {pe.Partition.PartitionId} with body {Encoding.UTF8.GetString(pe.Data.Body.ToArray())}");
                        DisplayMessage(pe);
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
