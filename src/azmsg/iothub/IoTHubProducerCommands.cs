using azmsg.common;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
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

        //public async Task SimulateTemperatureSensor(int messageDelay, int n)
        //{
        //    var deviceSimulator = new DeviceTelemetrySimulator();

        //    int messageCount = 0;
        //    foreach (var temperature in deviceSimulator.Temperature(true))
        //    {
        //        var dataPoint = new
        //        {
        //            Temperature = temperature
        //        };

        //        var messageString = JsonSerializer.Serialize(dataPoint);
        //        await Device2CloudMessage(messageString);


        //        if (n > 0)
        //        {
        //            messageCount = messageCount + 1;
        //            if (messageCount >= n)
        //            {
        //                Console.WriteLine("All messages sent");
        //                break;
        //            }
        //        }

        //        await Task.Delay(messageDelay);
        //    }

        //    await Task.CompletedTask;
        //}

        public string CreateSimulatedSensorPairDataPoint(IEnumerator<double> tempSimulator)
        {
            var hostName = Dns.GetHostName().ToLower();
            var tag1 = $"{hostName}_tmp_p23";
            var tag2 = $"{hostName}_tmp_ambient";
            var ts = DateTime.Now;
            tempSimulator.MoveNext();
            var temp1 = tempSimulator.Current;
            tempSimulator.MoveNext();
            var temp2 = tempSimulator.Current;


            var dataPoint = $@"[
                {{
                    ""tag"": ""{tag1}"",
                    ""ts"": ""{ts.ToString("O")}"",
                    ""value"": {temp1}
                }},    
                {{
                    ""tag"": ""{tag2}"",
                    ""ts"": ""{ts.ToString("O")}"",
                    ""value"": {temp2}
                }}
            ]";

            return dataPoint;
        }


        public async Task SimulateTemperatureSensorPair(int messageDelay, int n)
        {
            var deviceSimulator = DeviceTelemetryFactory.CreateTemperatureSimulator("none", "", true);
            var tempEnumerator = deviceSimulator.Measure().GetEnumerator();

            DeviceClient dc = CreateDeviceClient(TransportType.Mqtt);
            int messageCount = 0;
            while (true)
            {
                var messageString = CreateSimulatedSensorPairDataPoint(tempEnumerator);
                await Device2CloudMessage(messageString, dc, "UTF-8", "application/json");


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


        public async Task SimulateMultipleTemperatureSensors(string[] deviceContext, int messageDelay, int n, string pattern, string patternPeriod)
        {
            var deviceSimulators = new IDeviceTelemetrySimulator[deviceContext.Length];
            var deviceTemperature = new IEnumerator<double>[deviceContext.Length];
            var deviceClients = new DeviceClient[deviceContext.Length];

            try
            {



                for (int i = 0; i < deviceContext.Length; i++)
                {
                    deviceSimulators[i] = DeviceTelemetryFactory.CreateTemperatureSimulator(pattern, patternPeriod, true);
                    deviceTemperature[i] = deviceSimulators[i].Measure().GetEnumerator();
                    var client = CreateDeviceClient(deviceContext[i], TransportType.Mqtt);

                    if (client == null)
                    {
                        Console.WriteLine($"Device context {deviceContext[i]} not found");
                        return;
                    }
                    deviceClients[i] = client;
                }

                int messageCount = 0;
                while (true)
                {
                    for (int i = 0; i < deviceContext.Length; i++)
                    {
                        deviceTemperature[i].MoveNext();
                        var temperature = deviceTemperature[i].Current;

                        var dataPoint = new
                        {
                            Temperature = temperature
                        };

                        var messageString = JsonSerializer.Serialize(dataPoint);

                        await Device2CloudMessage(messageString, deviceClients[i], "UTF-8", "application/json", deviceContext[i]);
                    }

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

            }
            finally
            {
                // Clean up
                foreach (var e in deviceTemperature)
                {
                    e.Dispose();
                }
            }
            

            await Task.CompletedTask;
        }

        public async Task SimulateTemperatureSensor(int messageDelay, int n, string pattern, string patternPeriod)
        {
            var deviceSimulator = DeviceTelemetryFactory.CreateTemperatureSimulator(pattern.ToLower(), patternPeriod, true);

            DeviceClient dc = CreateDeviceClient(TransportType.Mqtt);

            int messageCount = 0;
            foreach (var temperature in deviceSimulator.Measure())
            {
                var dataPoint = new
                {
                    Temperature = temperature
                };

                var messageString = JsonSerializer.Serialize(dataPoint);
                await Device2CloudMessage(messageString, dc, "UTF-8", "application/json");


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

        private DeviceClient CreateDeviceClient(string contextName, TransportType? transportType)
        {
            DeviceClient deviceClient;

            var config = service.LoadConfig();

            IoTHubContext context = null;

            if (!config.IoTHubContexts.TryGetValue(contextName, out context))
            {
                return null;
            }

            if (transportType != null)
            {
                var transportSettings = CreateTransportSettingsFromName(transportType.Value, null);
                deviceClient = DeviceClient.CreateFromConnectionString(context.DeviceConnectionString, new ITransportSettings[] { transportSettings });
            }
            else
            {
                deviceClient = DeviceClient.CreateFromConnectionString(context.DeviceConnectionString);
            }

            return deviceClient;

        }

        private DeviceClient CreateDeviceClient(TransportType? transportType)
        {
            DeviceClient deviceClient;

            if (transportType != null)
            {
                var transportSettings = CreateTransportSettingsFromName(transportType.Value, null);
                deviceClient = DeviceClient.CreateFromConnectionString(currentContext.DeviceConnectionString, new ITransportSettings[] { transportSettings });
            }
            else
            {
                deviceClient = DeviceClient.CreateFromConnectionString(currentContext.DeviceConnectionString);
            }

            return deviceClient;
        }


        public async Task Device2CloudSingleMessage(string message, TransportType transportType)
        {
            var dc = CreateDeviceClient(transportType);

            await Device2CloudMessage(message, dc, "UTF-8", null);
        }

        public async Task Device2CloudMessage(string message, DeviceClient deviceClient, string contentEncoding, string contentType, string contextMessage = null)
        {
            //DeviceClient deviceClient;

            //if (transportType != null)
            //{
            //    var transportSettings = CreateTransportSettingsFromName(transportType, null);
            //    deviceClient = DeviceClient.CreateFromConnectionString(currentContext.DeviceConnectionString, new ITransportSettings[] { transportSettings });
            //}
            //else
            //{
            //    deviceClient = DeviceClient.CreateFromConnectionString(currentContext.DeviceConnectionString);
            //}

            var d2cMessage = new Message(Encoding.UTF8.GetBytes(message));

            if (contentEncoding != null)
            {
                d2cMessage.ContentEncoding = contentEncoding;
            }

            if (contentType != null)
            {
                d2cMessage.ContentType = contentType;
            }

            try
            {
                await deviceClient.SendEventAsync(d2cMessage);
                if (contextMessage != null)
                {
                    Console.WriteLine($"Message sent [{contextMessage}]: {message}");
                }
                else
                {
                    Console.WriteLine($"Message sent: {message}");
                }
                
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error sending message: {ex}");
                throw;
            }
            

            await Task.CompletedTask; 
        }


        public async Task Device2CloudMessageFromFile(string path, TransportType transportType)
        {
            var message = File.ReadAllText(path);

            var transportSettings = CreateTransportSettingsFromName(transportType, null);

            var deviceClient = DeviceClient.CreateFromConnectionString(currentContext.DeviceConnectionString, new ITransportSettings[] { transportSettings } );

            var d2cMessage = new Message(Encoding.UTF8.GetBytes(message));

            await deviceClient.SendEventAsync(d2cMessage);
            Console.WriteLine($"Message with transport type {transportType} sent: {message}");

            await Task.CompletedTask;
        }


        private static ITransportSettings CreateTransportSettingsFromName(Microsoft.Azure.Devices.Client.TransportType transportType, string proxyAddress)
        {
            switch (transportType)
            {
                case Microsoft.Azure.Devices.Client.TransportType.Http1:
                    return new Http1TransportSettings
                    {
                        Proxy = proxyAddress == null ? null : new WebProxy(proxyAddress),
                    };

                case Microsoft.Azure.Devices.Client.TransportType.Amqp:
                case Microsoft.Azure.Devices.Client.TransportType.Amqp_Tcp_Only:
                    return new AmqpTransportSettings(TransportType.Amqp_Tcp_Only); // Overriding as per error message if using Amqp only

                case Microsoft.Azure.Devices.Client.TransportType.Amqp_WebSocket_Only:
                    return new AmqpTransportSettings(transportType)
                    {
                        Proxy = proxyAddress == null ? null : new WebProxy(proxyAddress),
                    };

                case Microsoft.Azure.Devices.Client.TransportType.Mqtt:
                case Microsoft.Azure.Devices.Client.TransportType.Mqtt_Tcp_Only:
                    return new MqttTransportSettings(TransportType.Mqtt_Tcp_Only); // Overriding as per error message if using Mqtt only

                case Microsoft.Azure.Devices.Client.TransportType.Mqtt_WebSocket_Only:
                    return new MqttTransportSettings(transportType)
                    {
                        Proxy = proxyAddress == null ? null : new WebProxy(proxyAddress),
                    };
            }

            throw new NotSupportedException($"Unknown transport: '{transportType}'.");
        }
    }
}
