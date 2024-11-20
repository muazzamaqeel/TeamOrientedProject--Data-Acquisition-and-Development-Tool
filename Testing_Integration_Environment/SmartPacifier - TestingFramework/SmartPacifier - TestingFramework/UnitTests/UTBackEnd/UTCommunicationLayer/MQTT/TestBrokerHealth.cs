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
            string result = await brokerHealth.CheckBrokerHealthAsync();
            Assert.Contains("Healthy", result);
        }
        [Fact]
        public async Task BrokerReachable()
        {
            bool isReachable = await brokerHealth.IsBrokerReachableAsync();
            Assert.True(isReachable);
        }
        [Fact]
        public async Task BrokerNotReachable()
        {
            bool isReachable = await brokerHealth.IsBrokerReachableAsync();
            Assert.False(isReachable);
        }
        [Fact]
        public async Task ReceivingDataCheck()
        {
            bool isReceiving = await brokerHealth.IsReceivingDataAsync();
            Assert.True(isReceiving);
        }

        [Fact]
        public async Task NotReceivingDataCheck()
        {
            bool isReceiving = await brokerHealth.IsReceivingDataAsync();
            Assert.False(isReceiving);
        }
    }
}
