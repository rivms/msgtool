using azmsg.common;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
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
            var deviceSimulator = new DeviceTelemetrySimulator();
            var tempEnumerator = deviceSimulator.Temperature(true).GetEnumerator();

            int messageCount = 0;
            while (true)
            {
                var messageString = CreateSimulatedSensorPairDataPoint(tempEnumerator);
                await Device2CloudMessage(messageString, TransportType.Mqtt);


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
                await Device2CloudMessage(messageString, TransportType.Mqtt);


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

        public async Task Device2CloudMessage(string message, TransportType transportType)
        {
            DeviceClient deviceClient;

            if (transportType != null)
            {
                var transportSettings = CreateTransportSettingsFromName(transportType, null);
                deviceClient = DeviceClient.CreateFromConnectionString(currentContext.DeviceConnectionString, new ITransportSettings[] { transportSettings });
            }
            else
            {
                deviceClient = DeviceClient.CreateFromConnectionString(currentContext.DeviceConnectionString);
            }

            var d2cMessage = new Message(Encoding.UTF8.GetBytes(message));           

            await deviceClient.SendEventAsync(d2cMessage);
            Console.WriteLine($"Message with transport type {transportType} sent: {message}");

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
