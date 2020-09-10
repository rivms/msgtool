using azmsg.eventhub;
using azmsg.iothub;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
//using System.CommandLine.DragonFruit;
using System.IO;
using System.Threading.Tasks;
using static System.Environment;

namespace azmsg
{
    class Program
    {

        private static IServiceProvider serviceProvider;

        static async Task<int> Main(string[] args)
        {
            


            try
            {
                var services = new ServiceCollection();

                ConfigureServices(services);

                serviceProvider = services.BuildServiceProvider(true);

                var rootCommand = CreateCommands();

                return await rootCommand.InvokeAsync(args);

            }
            finally
            {
                if (serviceProvider is IDisposable)
                {
                    ((IDisposable)serviceProvider).Dispose();
                }
            }
            




            //// Initialize services
            //var configPath = Path.Combine(Environment.GetFolderPath(SpecialFolder.UserProfile, SpecialFolderOption.DoNotVerify), ".azmsg");
            //eh = new EventHubCommands(new ConfigService(configPath));
            //ih = new IoTHubCommandController(new ConfigService(configPath));

            //var rootCommand = CreateCommands();
            //// Parse the incoming args and invoke the handler
            //return await rootCommand.InvokeAsync(args);

        }

        private static Command CreateCommands()
        {
            var rootCommand = new RootCommand();

            IServiceScope scope = serviceProvider.CreateScope();

            var controllers =  scope.ServiceProvider.GetServices<ICommandController>();

            foreach(var controller in controllers)
            {
                rootCommand.AddCommand(controller.CreateCommand());
            }

            return rootCommand;
        }


        private static void ConfigureServices(IServiceCollection collection)
        {
            var configPath = Path.Combine(Environment.GetFolderPath(SpecialFolder.UserProfile, SpecialFolderOption.DoNotVerify), ".azmsg");

            collection.AddTransient<ConfigService>((sp) => { return new ConfigService(configPath); });
            collection.AddSingleton<ICommandController, IoTHubCommandController>();
            collection.AddSingleton<ICommandController, EventHubCommandController>();
        }


    //    static Command CreateCommands()
    //    {
    //        var rootCommand = new RootCommand();

    //rootCommand.Description = "My sample app";

    //var evtHubCommand = new Command("eventhub");


    //// eventhub send
    //var evtHubSendCommand = new Command("send");

    //        evtHubSendCommand.AddArgument(new Argument<string>("message"));
    //        evtHubSendCommand.Handler = CommandHandler.Create<string>(eh.Send);
            
    //// eventhub set-context
    //var eventHubContextCommand = new Command("set-context");
    //eventHubContextCommand.AddArgument(new Argument<string>("name"));
    ////eventHubContextCommand.Add(new Option<string>("--name"));
    //eventHubContextCommand.Add(new Option<string>("--connection-string"));
    //eventHubContextCommand.Add(new Option<string>("--eventhub-name"));
    //eventHubContextCommand.Handler = CommandHandler.Create<string, string, string>(eh.SetContext);


    //var eventHubUseContextCommand = new Command("use-context");
    //eventHubUseContextCommand.AddArgument(new Argument("context-name"));
    //eventHubUseContextCommand.Handler = CommandHandler.Create<string>(eh.UseContext);

    //evtHubCommand.Add(evtHubSendCommand);
    //evtHubCommand.Add(eventHubContextCommand);
    //evtHubCommand.Add(eventHubUseContextCommand);





    //rootCommand.Add(evtHubCommand);


    //        rootCommand.Add(ih.CreateCommand());

    //return rootCommand; 

    //    }


    //    static int IotHubSend(string message)
    //    {
    //        Console.WriteLine($"Sending message {message}");
    //        return 0;
    //    }
    }
}
