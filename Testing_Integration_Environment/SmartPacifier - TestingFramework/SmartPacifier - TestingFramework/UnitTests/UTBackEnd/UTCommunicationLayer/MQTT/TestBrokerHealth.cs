using System.Threading.Tasks;
using Xunit;
using SmartPacifier.BackEnd.CommunicationLayer.MQTT;

namespace SmartPacifier___TestingFramework.UnitTests.UTBackEnd.UTCommunicationLayer.MQTT
{
    public class TestBrokerHealth
    {
        private readonly BrokerHealth brokerHealth;

        public TestBrokerHealth()
        {
            // Initialize the class being tested
            brokerHealth = new BrokerHealth();
        }

        [Fact]
        public async Task BrokerReachableReceivingData()
        {
            // Act
            string result = await brokerHealth.CheckBrokerHealthAsync();

            // Assert
            Assert.Contains("Healthy", result);
        }


        [Fact]
        public async Task BrokerReachable()
        {
            // Act
            bool isReachable = await brokerHealth.IsBrokerReachableAsync();

            // Assert
            Assert.True(isReachable);
        }

        [Fact]
        public async Task BrokerNotReachable()
        {
            // Act
            bool isReachable = await brokerHealth.IsBrokerReachableAsync();

            // Assert
            Assert.False(isReachable); // Modify this if your broker is reachable
        }

        [Fact]
        public async Task ReceivingDataCheck()
        {
            // Act
            bool isReceiving = await brokerHealth.IsReceivingDataAsync();

            // Assert
            Assert.True(isReceiving);
        }

        [Fact]
        public async Task NotReceivingDataCheck()
        {
            // Act
            bool isReceiving = await brokerHealth.IsReceivingDataAsync();

            // Assert
            Assert.False(isReceiving); // Modify this if you simulate no messages
        }
    }
}
