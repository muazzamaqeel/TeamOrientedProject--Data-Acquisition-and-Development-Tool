using SmartPacifier.BackEnd.CommunicationLayer.MQTT;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SmartPacifier___TestingFramework.UnitTests.UTBackEnd.UTCommunicationLayer.MQTT
{
    public class MQTTBrokerTestCases
    {
        private readonly Broker _broker;
        private bool _isBrokerConnected; // Flag to check if broker is connected

        public MQTTBrokerTestCases()
        {
            _broker = Broker.Instance; // Initialize the broker singleton
            _isBrokerConnected = false; // Start with the broker disconnected
        }

        private async Task EnsureBrokerConnected()
        {
            if (!_isBrokerConnected)
            {
                try
                {
                    await _broker.ConnectBroker(); // Connect the broker
                    _isBrokerConnected = true; // Mark as connected
                }
                catch (Exception ex)
                {
                    // Handle or log the connection exception if needed
                    throw new InvalidOperationException("Failed to connect to the broker.", ex);
                }
            }
        }

        [Fact]
        public async Task TestMQTTConnectBroker()
        {
            await EnsureBrokerConnected(); // Ensure broker is connected
            Assert.True(_isBrokerConnected); // Check if connected
        }

        [Fact]
        public async Task TestMQTTSubscribe()
        {
            await EnsureBrokerConnected(); // Ensure broker is connected
            string topic = "Pacifier/test";

            Exception exception = await Record.ExceptionAsync(async () => await _broker.Subscribe(topic));
            Assert.Null(exception); // Verify that no exception was thrown during subscription
        }

        [Fact]
        public async Task TestMQTTSendMessage()
        {
            await EnsureBrokerConnected(); // Ensure broker is connected
            string topic = "Pacifier/test";
            string message = "Test Message";

            Exception exception = await Record.ExceptionAsync(async () => await _broker.SendMessage(topic, message));
            Assert.Null(exception); // Verify that no exception was thrown during message sending
        }
    }
}
