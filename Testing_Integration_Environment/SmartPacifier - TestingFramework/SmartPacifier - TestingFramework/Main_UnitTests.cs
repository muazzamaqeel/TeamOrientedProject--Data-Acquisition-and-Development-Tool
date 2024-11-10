using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using SmartPacifier___TestingFramework.UnitTests.UTBackEnd.UTDatabaseLayer.UTInitializeDockerImage;
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
        private bool _isBrokerConnected;  //------------ Flag to check if broker is connected DUDU NOv4
        private readonly UTInitializeDockerImage _dockerInitializer;


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

            // Initialize Docker container for InfluxDB
            _dockerInitializer = new UTInitializeDockerImage();
            _dockerInitializer.StartDockerContainer();

        }


        private async Task EnsureBrokerConnected() //-------------------- ADDED DUDU Nov4
        {
            if (!_isBrokerConnected)
            {
                await _broker.ConnectBroker(); // Connect the broker
                _isBrokerConnected = true; // Mark as connected
            }
        }

        private bool IsInfluxDBContainerRunning()
        {
            // Here, we should check if InfluxDB is accepting connections.
            // This is a simple check to ping the InfluxDB API or use a health check API.
            // Assuming a local InfluxDB container, you can check the HTTP endpoint for health status.
            try
            {
                var client = new System.Net.Http.HttpClient();
                var response = client.GetAsync("http://localhost:8086/health").Result;
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private bool InsertTestDataIntoInfluxDB()
        {
            // In reality, you'd interact with InfluxDB using an appropriate client (InfluxDB .NET client, HTTP API, etc.)
            // Here we can just simulate the process and return true for demonstration.
            return true;
        }

        public void Dispose()
        {
            // Clean up by stopping the Docker container after the tests are complete
            _dockerInitializer.StopDockerContainer();
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
        [Fact]
        public async Task RunCampaignExistsTest()
        {
            // Instantiate UnitTestMAN to access the test
            var unitTestMAN = new UnitTestMAN();

            // Run the CampaignExists_ShouldReturnTrue_WhenCampaignExists test
            await unitTestMAN.CampaignExists_ShouldReturnTrue_WhenCampaignExists();

            Console.WriteLine("CampaignExists_ShouldReturnTrue_WhenCampaignExists passed.");
        }





        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        /// <summary>
        /// Below are the Tests Related to MQTT Broker
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task TestMQTTConnectBroker()
        {
            // Instantiate the MQTTBrokerTestCases to run the connection test
            var mqttTestCases = new MQTTBrokerTestCases();
            await mqttTestCases.InitializeAsync();

            // Run the connection test and ensure no exceptions occur
            await mqttTestCases.TestMQTTConnectBroker();
        }
        [Fact]
        public async Task TestMQTTSubscribe()
        {
            // Instantiate the MQTTBrokerTestCases to run the subscription test
            var mqttTestCases = new MQTTBrokerTestCases();
            await mqttTestCases.InitializeAsync();

            // Run the subscription test to verify no exceptions are thrown
            await mqttTestCases.TestMQTTSubscribe();
        }
        [Fact]
        public async Task TestMQTTSendMessage()
        {
            // Instantiate the MQTTBrokerTestCases to run the send message test
            var mqttTestCases = new MQTTBrokerTestCases();
            await mqttTestCases.InitializeAsync();

            // Run the send message test to ensure no exceptions are thrown
            await mqttTestCases.TestMQTTSendMessage();
        }

        [Fact]
        public void RunInfluxDBContainerTests()
        {
            try
            {
                _dockerInitializer.StartDockerContainer();
                Console.WriteLine("InfluxDB container started successfully.");

                bool isContainerRunning = _dockerInitializer.IsContainerRunning();
                Assert.True(isContainerRunning, "Expected the InfluxDB container to be running.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test failed with exception: {ex.Message}");
                throw;
            }
            finally
            {
                _dockerInitializer.StopDockerContainer();
                Console.WriteLine("InfluxDB container stopped and removed.");
            }
        }
    }
}
































    /*


    // Docker Initialization Test ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    //------------------- ADDED DUDU Nov8

    [Fact]
        public void TestInfluxDBConnection()
        {
            // Example test case: check if the container is up and running
            Assert.True(IsInfluxDBContainerRunning(), "InfluxDB container is not running!");

            // Here you would add further code to interact with InfluxDB
            // For example, querying InfluxDB, inserting data, etc.
        }

        [Fact]
        public void TestDatabaseOperations()
        {
            // Example test case: check some database operation in InfluxDB
            // This could involve inserting data and checking if it persists.

            // Example of interacting with InfluxDB, but actual code to interact with InfluxDB will depend
            // on how your system is designed to communicate with the database
            bool isDataInserted = InsertTestDataIntoInfluxDB();
            Assert.True(isDataInserted, "Test data insertion into InfluxDB failed!");
        }




    */



