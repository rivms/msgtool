using azmsg.common;
using azmsg.iothub;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace azmsg.eventhub
{
    class EventHubCommandController : ICommandController
    {
        private ConfigService service;

        private EventHubContext CurrentContext
        {
            get
            {
                var config = service.LoadConfig();
                return config.EventHubContexts[config.CurrentEventHubContext];
            }
        }

        public EventHubCommandController(ConfigService service)
        {
            this.service = service;
        }

        public Command CreateCommand()
        {
            var eventhubCommand = new Command("eventhub");

            // eventhub send
            var evtHubSendCommand = new Command("send");
            evtHubSendCommand.Add(new Option<string>("--message"));
            evtHubSendCommand.Add(new Option<string>("--from-file"));
            evtHubSendCommand.Add(new Option<string>("--eventhub-name", "override event hub name in context"));
            evtHubSendCommand.Handler = CommandHandler.Create<string, string, string>(Send);
            eventhubCommand.AddCommand(evtHubSendCommand);

            // eventhub set-context
            var eventHubContextCommand = new Command("set-context");
            eventHubContextCommand.AddArgument(new Argument<string>("name"));
            eventHubContextCommand.Add(new Option<string>("--connection-string"));
            eventHubContextCommand.Add(new Option<string>("--eventhub-name"));
            eventHubContextCommand.Add(new Option<string>("--consumer-group"));
            eventHubContextCommand.Handler = CommandHandler.Create<string, string, string, string>(SetContext);
            eventhubCommand.AddCommand(eventHubContextCommand);

            // use-context
            var eventHubUseContextCommand = new Command("use-context");
            eventHubUseContextCommand.AddArgument(new Argument("context-name"));
            eventHubUseContextCommand.Handler = CommandHandler.Create<string>(UseContext);
            eventhubCommand.AddCommand(eventHubUseContextCommand);

            // message
            var messageCommand = new Command("message");
            messageCommand.Add(new Option<int>("--limit", "number of messages to retrieve"));
            messageCommand.Add(new Option<int>("--message-timeout", "message timeout in seconds"));
            messageCommand.Add(new Option("--follow", "stream latest messages"));
            messageCommand.Add(new Option<string>("--eventhub-name", "override event hub name in context"));
            messageCommand.Handler = CommandHandler.Create<bool, int, int, string>(WatchMessage);
            eventhubCommand.AddCommand(messageCommand);

            // simulate
            var simulateCommand = new Command("simulate");
            simulateCommand.Add(new Option<string>("--device-type", () => { return "temperature"; }, "Only support simulated device of type Temperature"));
            simulateCommand.Add(new Option<int>("--message-delay", () => { return 5000; }, "Delay between messages in milliseconds. Default 5 seconds"));
            simulateCommand.Add(new Option<int>("--n", () => { return -1; }, "Number of messages to send. Default is unbounded"));
            simulateCommand.Handler = CommandHandler.Create<string, int, int>(SimulateDevice);
            eventhubCommand.Add(simulateCommand);


            return eventhubCommand;
        }

        public async Task SimulateDevice(string deviceType, int messageDelay, int n)
        {
            var producer = new EventHubProducerCommands(CurrentContext, service);
            if (string.Compare(deviceType, "temperature", true)==0)
            {
                await producer.SimulateTemperatureSensor(messageDelay, n);
            }
            await Task.CompletedTask;
        }

        public async Task WatchMessage(bool follow, int limit, int messageTimeout, string eventhubName)
        {
            var cc = new EventHubConsumerCommands(CurrentContext, service);

            await cc.WatchMessage(follow, limit, messageTimeout, eventhubName);


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

        public async Task Send(string message, string fromFile, string eventhubName)
        {
            var pc = new EventHubProducerCommands(CurrentContext, service);

            if (fromFile != null)
            {
                await pc.SendFromFile(fromFile, eventhubName);
            }
            else
            {
                await pc.Send(message, eventhubName);
            }
            
        }



        public void SetContext(string name, string connectionString, string eventHubName, string consumerGroup)
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

            if (consumerGroup != null)
            {
                ctxt.ConsumerGroup = consumerGroup;
            }

            config.EventHubContexts[name.ToLower()] = ctxt;

            service.UpdateConfig(config);
        }

        public void UseContext(string contextName)
        {
            var config = service.LoadConfig();

            if (config.EventHubContexts.ContainsKey(contextName))
            {
                config.CurrentEventHubContext = contextName;
                service.UpdateConfig(config);
                Console.WriteLine($"Using context {contextName}");
            }


        }

    }
}
