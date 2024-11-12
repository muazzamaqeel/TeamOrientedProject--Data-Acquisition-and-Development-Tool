﻿using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using Protos;
using SmartPacifier.BackEnd.CommunicationLayer.Protobuf;

namespace SmartPacifier.BackEnd.CommunicationLayer.MQTT
{
    public class Broker : IDisposable
    {
        private readonly string BROKER_ADDRESS = "localhost";
        private readonly int BROKER_PORT = 1883;

        private static Broker? _brokerInstance;
        private static readonly object _lock = new object();

        private IMqttClient _mqttClient;
        private bool disposed = false;

        public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

        private Broker()
        {
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();

            _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
            _mqttClient.ConnectedAsync += OnConnectedAsync;
            _mqttClient.DisconnectedAsync += OnDisconnectedAsync;
        }

        public void Dispose()
        {
            if (!disposed)
            {
                _mqttClient?.Dispose();
                disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        ~Broker()
        {
            Dispose();
        }

        public static Broker Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_brokerInstance == null)
                    {
                        _brokerInstance = new Broker();
                    }
                    return _brokerInstance;
                }
            }
        }

        public async Task ConnectBroker()
        {
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(BROKER_ADDRESS, BROKER_PORT)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(20))
                .WithCleanSession(false)
                .Build();

            try
            {
                await _mqttClient.ConnectAsync(options);
                Console.WriteLine("Successfully connected to Docker MQTT broker.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to connect to MQTT broker: " + ex.Message);
                throw;
            }
        }

        public async Task Subscribe(string topic)
        {
            var topicFilter = new MqttTopicFilterBuilder()
                .WithTopic(topic)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
                .Build();

            var subscribeResult = await _mqttClient.SubscribeAsync(topicFilter);

            Console.WriteLine($"Subscribed to topic: {topic} with QoS {topicFilter.QualityOfServiceLevel}");
            foreach (var result in subscribeResult.Items)
            {
                Console.WriteLine($"Subscription result for topic '{result.TopicFilter.Topic}': {result.ResultCode}");
            }
        }


        private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                byte[] rawPayload = e.ApplicationMessage.PayloadSegment.ToArray();
                string topic = e.ApplicationMessage.Topic;

                Console.WriteLine($"Received raw data on topic '{topic}'");

                if (rawPayload.Length > 0)
                {
                    string[] topicParts = topic.Split('/');
                    if (topicParts.Length >= 2 && topicParts[0] == "Pacifier")
                    {
                        string pacifierId = topicParts[1];
                        var (parsedPacifierId, sensorType, parsedData) = ExposeSensorDataManager.Instance.ParseSensorData(pacifierId, topic, rawPayload);

                        MessageReceived?.Invoke(this, new MessageReceivedEventArgs(topic, rawPayload, parsedPacifierId, sensorType, parsedData));
                    }
                    else
                    {
                        Console.WriteLine($"Invalid topic format: {topic}");
                    }
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to process message: {ex.Message}");
            }
        }


        private async Task OnConnectedAsync(MqttClientConnectedEventArgs e)
        {
            Console.WriteLine("Connected successfully with MQTT Broker.");
            await Task.CompletedTask;
        }

        private async Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs e)
        {
            Console.WriteLine("Disconnected from MQTT Broker.");

            await Task.Delay(TimeSpan.FromSeconds(5));
            try
            {
                await _mqttClient.ReconnectAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Reconnection failed: " + ex.Message);
            }
        }

        public class MessageReceivedEventArgs : EventArgs
        {
            public string Topic { get; set; }
            public byte[] Payload { get; set; }
            public string PacifierId { get; set; }
            public string SensorType { get; set; }
            public Dictionary<string, object> ParsedData { get; set; }

            public MessageReceivedEventArgs(string topic, byte[] payload, string pacifierId, string sensorType, Dictionary<string, object> parsedData)
            {
                Topic = topic;
                Payload = payload;
                PacifierId = pacifierId;
                SensorType = sensorType;
                ParsedData = parsedData;
            }
        }
    }
}
