using Moq;
using SmartPacifier.BackEnd.Database.InfluxDB.Managers;
using SmartPacifier.Interface.Services;
using SmartPacifier___TestingFramework.UnitTests.Unit_Tests_BackEnd.UnitTest_Services;
using SmartPacifier___TestingFramework.UnitTests.UTBackEnd.UTManagers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;


namespace SmartPacifier___TestingFramework.UnitTests.UTBackEnd.UTManagers
{
    public class UnitTestMAN : IUnitTestMAN
    {
        private readonly Mock<IDatabaseService> _mockDatabaseService;
        private readonly Mock<IManagerPacifiers> _mockManagerPacifiers;
        private readonly ManagerCampaignWrapper _managerCampaignWrapper;

        public UnitTestMAN()
        {
            _mockDatabaseService = new Mock<IDatabaseService>();
            _mockManagerPacifiers = new Mock<IManagerPacifiers>();

            // Create ManagerCampaign instance with mocked dependencies
            var managerCampaign = new ManagerCampaign(_mockDatabaseService.Object, _mockManagerPacifiers.Object);
            // Wrap ManagerCampaign in the ManagerCampaignWrapper
            _managerCampaignWrapper = new ManagerCampaignWrapper(managerCampaign, "SmartPacifier-Bucket1");
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

        // Additional tests...
    }
}
