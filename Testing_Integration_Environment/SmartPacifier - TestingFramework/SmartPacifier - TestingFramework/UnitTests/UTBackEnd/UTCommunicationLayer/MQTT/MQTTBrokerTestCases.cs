using SmartPacifier.BackEnd.CommunicationLayer.MQTT;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SmartPacifier___TestingFramework.UnitTests.UTBackEnd.UTCommunicationLayer.MQTT
{
    public class MQTTBrokerTestCases : IAsyncLifetime
    {
        private readonly Broker _broker;
        private static bool _isBrokerConnected = false; // Static variable to track connection state

        public MQTTBrokerTestCases()
        {
            _broker = Broker.Instance;
            _broker.MessageReceived += OnMessageReceived; // Subscribe to message events
        }

        public async Task InitializeAsync()
        {
            await EnsureBrokerConnected(); // Ensure broker is connected at the start of each test
        }

        public Task DisposeAsync()
        {
            // No need to disconnect here, as we'll rely on the static connection state
            return Task.CompletedTask;
        }

        private async Task EnsureBrokerConnected()
        {
            if (!_isBrokerConnected)
            {
                try
                {
                    await _broker.ConnectBroker();
                    _isBrokerConnected = true; // Update static connection state
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Failed to connect to the broker.", ex);
                }
            }
        }

        [Fact]
        public async Task TestMQTTConnectBroker()
        {
            await EnsureBrokerConnected(); // Ensure the broker is connected before asserting
            Assert.True(_isBrokerConnected); // Check if connected
        }

        [Fact]
        public async Task TestMQTTSubscribe()
        {
            await EnsureBrokerConnected(); // Ensure connection before subscription
            string topic = "Pacifier/test";
            Exception exception = await Record.ExceptionAsync(async () => await _broker.Subscribe(topic));
            Assert.Null(exception); // Verify that no exception was thrown during subscription
        }

        [Fact]
        public async Task TestMQTTSendMessage()
        {
            await EnsureBrokerConnected(); // Ensure connection before sending
            string topic = "Pacifier/test";
            string message = "Test Message";

            Exception exception = await Record.ExceptionAsync(async () => await _broker.SendMessage(topic, message));
            Assert.Null(exception); // Verify that no exception was thrown during message sending
        }

        [Fact]
        public async Task TestMQTTReceiveMessage()
        {
            await EnsureBrokerConnected(); // Ensure connection before receiving
            string topic = "Pacifier/test";
            string expectedMessage = "Hello from Test";

            bool messageReceived = false;
            _broker.MessageReceived += (sender, args) =>
            {
                if (args.Topic == topic && args.Payload == expectedMessage)
                {
                    messageReceived = true;
                }
            };

            await _broker.SendMessage(topic, expectedMessage); // Send message to test receiving
            await Task.Delay(500); // Wait briefly for message to be received

            Assert.True(messageReceived, "Expected message was not received.");
        }

        private void OnMessageReceived(object? sender, Broker.MessageReceivedEventArgs e)
        {
            Console.WriteLine($"Received message on topic '{e.Topic}': {e.Payload}");
        }
    }
}
