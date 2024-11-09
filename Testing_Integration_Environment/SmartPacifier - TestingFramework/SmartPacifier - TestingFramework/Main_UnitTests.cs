using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using SmartPacifier___TestingFramework.UnitTests.Unit_Tests_BackEnd.Unit_Tests_DatabaseLayer.Unit_Tests_InfluxDB.Unit_Tests_Connection;
using SmartPacifier___TestingFramework.UnitTests.Unit_Tests_BackEnd.UnitTest_Services;
using SmartPacifier___TestingFramework.UnitTests.UTBackEnd.UTManagers;
using SmartPacifier.BackEnd.Database.InfluxDB.Managers;
using SmartPacifier.Interface.Services;
using SmartPacifier.BackEnd.CommunicationLayer.MQTT; // Ensure this matches the namespace where IBroker and MQTTBroker are defined ADDED DUDU Nov4
using SmartPacifier___TestingFramework.UnitTests.UTBackEnd.UTCommunicationLayer.MQTT; // For MQTTBrokerTestCases

namespace SmartPacifier___TestingFramework
{
    public class Main_UnitTests
    {
        private readonly IUnitTestDB _unitTest;
        private readonly CampaignWrap _managerCampaignWrapper;
        private readonly Mock<IDatabaseService> _mockDatabaseService;
        private readonly Mock<IManagerPacifiers> _mockManagerPacifiers;
        private readonly Broker _broker; //-------------ADDED DUDU Nov4
        private bool _isBrokerConnected;  // Flag to check if broker is connected DUDU NOv4


        public Main_UnitTests()
        {
            // Instantiate UnitTestDB for connection testing
            _unitTest = new UnitTestDB();

            // Set up mocks for campaign-related tests
            _mockDatabaseService = new Mock<IDatabaseService>();
            _mockManagerPacifiers = new Mock<IManagerPacifiers>();

            // Create an instance of ManagerCampaign and wrap it with ManagerCampaignWrapper
            var managerCampaign = new ManagerCampaign(_mockDatabaseService.Object, _mockManagerPacifiers.Object);
            _managerCampaignWrapper = new CampaignWrap(managerCampaign, "SmartPacifier-Bucket1");

            _broker = Broker.Instance; //------------Initialize the broker singleton ADDED DUDU Nov4
        }


        private async Task EnsureBrokerConnected() //-------------------- ADDED DUDU Nov4
        {
            if (!_isBrokerConnected)
            {
                await _broker.ConnectBroker(); // Connect the broker
                _isBrokerConnected = true; // Mark as connected
            }
        }








        [Fact]
        public async Task RunTest_ValidToken_Should_Allow_Connection()
        {
            try
            {
                // Run the test for valid token connection
                await _unitTest.Test_ValidToken_Should_Allow_Connection();
                Console.WriteLine("Test_ValidToken_Should_Allow_Connection passed.");
            }
            catch (Exception ex)
            {
                // Log any failure and fail the test
                Console.WriteLine($"Test_ValidToken_Should_Allow_Connection failed: {ex.Message}");
                Assert.False(true, ex.Message); // Fail the test if an exception occurs
            }
        }

        [Fact]
        public async Task CampaignExists_ShouldReturnTrue_WhenCampaignExists()
        {
            // Arrange
            string campaignName = "Campaign1";
            _mockDatabaseService.Setup(s => s.ReadData(It.IsAny<string>()))
                                .ReturnsAsync(new List<string> { "Campaign1" });

            // Act
            bool result = await _managerCampaignWrapper.CampaignExistsAsync(campaignName);

            // Assert
            Assert.True(result, "Expected the campaign to exist.");
        }

        [Fact]
        public async Task CampaignExists_ShouldReturnFalse_WhenCampaignDoesNotExist()
        {
            // Arrange
            string campaignName = "NonExistentCampaign";
            _mockDatabaseService.Setup(s => s.ReadData(It.IsAny<string>()))
                                .ReturnsAsync(new List<string>());

            // Act
            bool result = await _managerCampaignWrapper.CampaignExistsAsync(campaignName);

            // Assert
            Assert.False(result, "Expected the campaign not to exist.");
        }

        [Fact]
        public async Task GetPacifiersInCampaign_ShouldReturnListOfPacifiers_WhenCampaignHasPacifiers()
        {
            // Arrange
            string campaignName = "Campaign1";
            var expectedPacifiers = new List<string> { "Pacifier1", "Pacifier2" };

            // Setup the mock to return the expected list when the method is called
            _mockManagerPacifiers.Setup(p => p.GetPacifiersAsync(campaignName))
                                 .ReturnsAsync(expectedPacifiers);

            // Act
            var result = await _managerCampaignWrapper.GetPacifiersInCampaignAsync(campaignName);

            // Assert
            Assert.Equal(expectedPacifiers, result);
        }


        [Fact]
        public async Task GetPacifiersInCampaign_ShouldReturnEmptyList_WhenCampaignHasNoPacifiers()
        {
            // Arrange
            string campaignName = "EmptyCampaign";
            _mockManagerPacifiers.Setup(p => p.GetPacifiersAsync(campaignName))
                                 .ReturnsAsync(new List<string>());

            // Act
            var result = await _managerCampaignWrapper.GetPacifiersInCampaignAsync(campaignName);

            // Assert
            Assert.Empty(result);
        }



        // ---------!!!!--------BELOW 4 [Fact] are related to : MQTT Broker DATA retrieval--trial Duygu Nov-4
        // Add tests for MQTT Broker functionality
        // Add a method to ensure the broker is connected before running tests
        // ---------!!!!--------BELOW 4 [Fact] are related to : MQTT Broker DATA retrieval
        [Fact]
        public async Task TestMQTTConnectBroker()
        {
            await EnsureBrokerConnected(); // Ensure broker is connected
            // No exception expected if connected successfully
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

        [Fact]
        public async Task RunMQTTBrokerTests()
        {
            var mqttBrokerTests = new UnitTests.UTBackEnd.UTCommunicationLayer.MQTT.MQTTBrokerTestCases();

            await mqttBrokerTests.TestMQTTConnectBroker();
            await mqttBrokerTests.TestMQTTSubscribe();
            await mqttBrokerTests.TestMQTTSendMessage();
        }


    }
}
