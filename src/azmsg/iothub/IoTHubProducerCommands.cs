using azmsg.common;
using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace azmsg.iothub
{
    class IoTHubProducerCommands
    {
        private IoTHubContext currentContext;
        private ConfigService service;

        public IoTHubProducerCommands(IoTHubContext context, ConfigService service)
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
                await Device2CloudMessage(messageString);


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

        public async Task Device2CloudMessage(string message)
        {
            var deviceClient = DeviceClient.CreateFromConnectionString(currentContext.DeviceConnectionString);

            var d2cMessage = new Message(Encoding.UTF8.GetBytes(message));

            await deviceClient.SendEventAsync(d2cMessage);
            Console.WriteLine($"Message sent: {message}");

            await Task.CompletedTask; 
        }


        public async Task Device2CloudMessageFromFile(string path)
        {
            var message = File.ReadAllText(path);
            var deviceClient = DeviceClient.CreateFromConnectionString(currentContext.DeviceConnectionString);

            var d2cMessage = new Message(Encoding.UTF8.GetBytes(message));

            await deviceClient.SendEventAsync(d2cMessage);
            Console.WriteLine($"Message sent: {message}");

            await Task.CompletedTask;
        }
    }
}
