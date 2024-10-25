using System;
using System.Threading.Tasks;
using Xunit;
using SmartPacifier___TestingFramework.UnitTests.Unit_Tests_BackEnd.Unit_Tests_DatabaseLayer.Unit_Tests_InfluxDB.Unit_Tests_Connection;
using SmartPacifier___TestingFramework.UnitTests.Unit_Tests_BackEnd.UnitTest_Services;

namespace SmartPacifier___TestingFramework
{
    public class Main_UnitTests
    {
        private readonly IUnitTestDB _unitTest;

        public Main_UnitTests()
        {
            // Instantiate UnitTestDB through the interface
            _unitTest = new UnitTestDB();
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
    }
}
