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
        public async Task TestMQTTConnectBroker()
        {
            await EnsureBrokerConnected(); // Ensure the broker is connected before asserting
            Assert.True(_isBrokerConnected); // Check if connected
        }
        public async Task TestMQTTSubscribe()
        {
            await EnsureBrokerConnected(); // Ensure connection before subscription
            string topic = "Pacifier/test";
            Exception exception = await Record.ExceptionAsync(async () => await _broker.Subscribe(topic));
            Assert.Null(exception); // Verify that no exception was thrown during subscription
        }
        public async Task TestMQTTSendMessage()
        {
            await EnsureBrokerConnected(); // Ensure connection before sending
            string topic = "Pacifier/test";
            string message = "Test Message";

            Exception exception = await Record.ExceptionAsync(async () => await _broker.SendMessage(topic, message));
            Assert.Null(exception); // Verify that no exception was thrown during message sending
        }



        /*
        [Fact]
        public async Task TestMQTTReceiveMessage()
        {
            await EnsureBrokerConnected(); // Ensure the broker is connected

            string topic = "Pacifier/test";
            string expectedMessage = "Hello from Test";

            var messageReceivedCompletionSource = new TaskCompletionSource<bool>();

            // Set up event handler to capture the message
            EventHandler<Broker.MessageReceivedEventArgs> messageHandler = (sender, args) =>
            {
                if (args.Topic == topic && args.Payload == expectedMessage)
                {
                    messageReceivedCompletionSource.TrySetResult(true);
                }
            };

            try
            {
                _broker.MessageReceived += messageHandler; // Attach the handler

                await _broker.Subscribe(topic); // Ensure subscription before sending

                // Send the message to test receiving
                await _broker.SendMessage(topic, expectedMessage);

                // Wait up to 5 seconds for the message to be received
                bool messageReceived = await messageReceivedCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(5));

                Assert.True(messageReceived, "Expected message was not received.");
            }
            finally
            {
                _broker.MessageReceived -= messageHandler; // Clean up the event handler
            }
        }


        */


        private void OnMessageReceived(object? sender, Broker.MessageReceivedEventArgs e)
        {
            Console.WriteLine($"Received message on topic '{e.Topic}': {e.Payload}");
        }
    }
}
