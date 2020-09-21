using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace azmsg.powerbi
{
    class PowerBICommandController : ICommandController
    {
        private ConfigService service;

        private PowerBIContext CurrentContext
        {
            get
            {
                var config = service.LoadConfig();
                return config.PowerBIContexts[config.CurrentPowerBIContext];
            }
        }
        public Command CreateCommand()
        {
            var powerBICommand = new Command("powerbi");


            // set-context
            var iotHubContextCommand = new Command("set-context");
            iotHubContextCommand.AddArgument(new Argument<string>("name"));
            iotHubContextCommand.Add(new Option<string>("--push-url"));
            iotHubContextCommand.Handler = CommandHandler.Create<string, string>(SetContext);
            powerBICommand.AddCommand(iotHubContextCommand);


            // use-context
            var iotHubUseContextCommand = new Command("use-context");
            iotHubUseContextCommand.AddArgument(new Argument("context-name"));
            iotHubUseContextCommand.Handler = CommandHandler.Create<string>(UseContext);
            powerBICommand.AddCommand(iotHubUseContextCommand);

            // send
            var powerBISendCommand = new Command("send");
            powerBISendCommand.Add(new Option<string>("--message"));
            powerBISendCommand.Add(new Option<string>("--from-file"));
            //powerBISendCommand.Add(new Option<string>("--eventhub-name", "override event hub name in context"));
            powerBISendCommand.Handler = CommandHandler.Create<string, string>(Send);
            powerBICommand.AddCommand(powerBISendCommand);


            // stream
            var powerBIStreamCommand = new Command("stream");
            powerBIStreamCommand.Add(new Option<string>("--from-file"));
            powerBIStreamCommand.Add(new Option<int>("--delay-ms", "Delay between sending messages in file (milliseconds). Default is 10seconds"));
            powerBIStreamCommand.Add(new Option<FileType>("--file-type"));
            //powerBISendCommand.Add(new Option<string>("--eventhub-name", "override event hub name in context"));
            powerBIStreamCommand.Handler = CommandHandler.Create<string, int, FileType>(Stream);
            powerBICommand.AddCommand(powerBIStreamCommand);

            return powerBICommand;
        }

        public PowerBICommandController(ConfigService service)
        {
            this.service = service;
        }

        public async Task Stream(string fromFile, int delay, FileType fileFormat)
        {
            var pc = new PowerBIProducerCommands(CurrentContext, service);

            int timeBetweenMessages = 1000;

            if (delay > 0)
            {
                timeBetweenMessages = delay;
            }

            if (fileFormat == FileType.NDJSON)
            {
                await pc.StreamNDJSON(fromFile, timeBetweenMessages);
            }
            else
            {
                Console.WriteLine($"Invalid file format: {fileFormat}, expected ndjson");
                return;
            }

            
        }

        public async Task Send(string message, string fromFile)
        {
            var pc = new PowerBIProducerCommands(CurrentContext, service);

            if (fromFile != null)
            {
                await pc.SendFromFile(fromFile);
            }
            else
            {
                await pc.Send(message);
            }

        }

        public void SetContext(string name,
            string pushUrl)
        {
            Console.WriteLine($"Setting context {name} with push url {pushUrl}");
            var config = service.LoadConfig();
            PowerBIContext ctxt = null;
            if (!config.PowerBIContexts.TryGetValue(name, out ctxt))
            {
                ctxt = new PowerBIContext();
            }


            if (pushUrl != null)
            {
                ctxt.PushURL = pushUrl;
            }

            config.PowerBIContexts[name.ToLower()] = ctxt;

            service.UpdateConfig(config);
        }

        public void UseContext(string contextName)
        {
            var config = service.LoadConfig();

            if (config.PowerBIContexts.ContainsKey(contextName))
            {
                config.CurrentPowerBIContext = contextName;
                service.UpdateConfig(config);
                Console.WriteLine($"Using context {contextName}");
            }


        }

    }
}
