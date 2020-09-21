using azmsg.common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace azmsg.powerbi
{
    class PowerBIProducerCommands
    {
        private PowerBIContext currentContext;
        private ConfigService service;

        public PowerBIProducerCommands(PowerBIContext context, ConfigService service)
        {
            this.currentContext = context;
            this.service = service;
        }

        public async Task StreamNDJSON(string fromFile, int delay)
        {
            string line;
            bool canWait = false;

            try
            {
                using (var file = new StreamReader(fromFile))
                {
                    while ((line = file.ReadLine()) != null)
                    {
                        if (canWait)
                        {
                            await Task.Delay(delay);
                        }
                        else
                        {
                            canWait = true;
                        }

                        await Send(line);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error {ex}");
            }
        }

        public async Task Send(string message)
        {
            var config = service.LoadConfig();

            this.currentContext = config.PowerBIContexts[config.CurrentPowerBIContext];

            //await RunAsync(currentContext.ConnectionString, currentContext.EventHubName);
            try
            {
                var r = await HttpUtilities.PostAsync(currentContext.PushURL, message);
                r.EnsureSuccessStatusCode();
                Console.WriteLine($"Http post sent with message: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public async Task SendFromFile(string path)
        {
            var config = service.LoadConfig();

            this.currentContext = config.PowerBIContexts[config.CurrentPowerBIContext];

            //await RunAsync(currentContext.ConnectionString, currentContext.EventHubName);
            try
            {
                var message = File.ReadAllText(path);
                var r = await HttpUtilities.PostAsync(currentContext.PushURL, message);
                r.EnsureSuccessStatusCode();
                Console.WriteLine($"Http post sent with message: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
