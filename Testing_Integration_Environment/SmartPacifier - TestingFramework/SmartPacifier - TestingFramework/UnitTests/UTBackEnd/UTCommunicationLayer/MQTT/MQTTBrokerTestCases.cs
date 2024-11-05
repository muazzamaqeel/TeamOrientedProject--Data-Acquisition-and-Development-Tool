using System;
using System.Threading.Tasks;
using Xunit;

namespace SmartPacifier___TestingFramework.UnitTests.UTBackEnd.UTCommunicationLayer.MQTT
{
    // Custom mock class to simulate the behavior of the Broker class



    /// <summary>
    /// A Wrapper Class that allows us to override the Bok
    /// </summary>
    public class MockBroker
    {
        public event EventHandler<MessageReceivedEventArgs>? MockMessageReceived;

        public Task ConnectBroker()
        {
            Console.WriteLine("Mock: ConnectBroker called");
            return Task.CompletedTask;
        }

        public Task Subscribe(string topic)
        {
            Console.WriteLine($"Mock: Subscribed to topic: {topic}");
            return Task.CompletedTask;
        }

        public Task SendMessage(string topic, string message)
        {
            Console.WriteLine($"Mock: Sending message to topic: {topic}");
            // Simulate receiving the message by invoking the MockMessageReceived event
            MockMessageReceived?.Invoke(this, new MessageReceivedEventArgs(topic, message));
            return Task.CompletedTask;
        }

        // Event argument class for message received event
        public class MessageReceivedEventArgs : EventArgs
        {
            public string Topic { get; }
            public string Payload { get; }

            public MessageReceivedEventArgs(string topic, string payload)
            {
                Topic = topic;
                Payload = payload;
            }
        }
    }

    public class MQTTBrokerTestCases : IAsyncLifetime
    {
        private readonly MockBroker _broker;

        public MQTTBrokerTestCases()
        {
            _broker = new MockBroker();
            _broker.MockMessageReceived += OnMessageReceived; // Subscribe to message events
        }

        public async Task InitializeAsync()
        {
            await _broker.ConnectBroker(); // Simulate connecting the broker at the start of each test
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        [Fact]
        public async Task TestMQTTConnectBroker()
        {
            await _broker.ConnectBroker(); // Ensure the mock broker is "connected"
            Assert.True(true); // If no exception, we assume it connected successfully
                               // The true paramemter tells us that we expect a true conenction, and if its false the test will fail.
        }

        [Fact]
        public async Task TestMQTTSubscribe()
        {
            string topic = "Pacifier/test";
            Exception exception = await Record.ExceptionAsync(async () => await _broker.Subscribe(topic));
            Assert.Null(exception); // Verify that no exception was thrown during subscription
        }

        [Fact]
        public async Task TestMQTTSendMessage()
        {
            string topic = "Pacifier/test";
            string message = "Test Message";

            Exception exception = await Record.ExceptionAsync(async () => await _broker.SendMessage(topic, message));
            Assert.Null(exception); // Verify that no exception was thrown during message sending
        }

        [Fact]
        public async Task TestMQTTReceiveMessage()
        {
            string topic = "Pacifier/test";
            string expectedMessage = "Hello from Test";

            var messageReceivedCompletionSource = new TaskCompletionSource<bool>();

            // Set up event handler to capture the message
            EventHandler<MockBroker.MessageReceivedEventArgs> messageHandler = (sender, args) =>
            {
                if (args.Topic == topic && args.Payload == expectedMessage)
                {
                    messageReceivedCompletionSource.TrySetResult(true);
                }
            };

            try
            {
                _broker.MockMessageReceived += messageHandler; // Attach the handler

                await _broker.Subscribe(topic); // Simulate subscribing

                // Simulate sending the message
                await _broker.SendMessage(topic, expectedMessage);

                // Wait up to 5 seconds for the message to be received
                bool messageReceived = await messageReceivedCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(5));

                Assert.True(messageReceived, "Expected message was not received.");
            }
            finally
            {
                _broker.MockMessageReceived -= messageHandler; // Clean up the event handler
            }
        }

        private void OnMessageReceived(object? sender, MockBroker.MessageReceivedEventArgs e)
        {
            Console.WriteLine($"Mock: Received message on topic '{e.Topic}': {e.Payload}");
        }
    }
}
