using Xunit;
using InfluxDB.Client;
using InfluxDB.Client.Writes;
using System;
using System.Threading.Tasks;
using InfluxDB.Client.Api.Domain;
using SmartPacifier___TestingFramework.UnitTests.Unit_Tests_BackEnd.UnitTest_Services;

namespace SmartPacifier___TestingFramework.UnitTests.Unit_Tests_BackEnd.Unit_Tests_DatabaseLayer.Unit_Tests_InfluxDB.Unit_Tests_Connection
{
    public class UnitTestDB : IUnitTestDB
    {
        private readonly string _url = "http://localhost:8086";  
        private readonly string _validToken = "v0AsUfz15SKM5ez16l3iZRiDRxd_mbDTaYXluLBqXSvrorhtmgSGul6j-5GZ0ryL9avBfun4Ho1vbv6CKt0FeA==";
        private readonly string _bucket = "SmartPacifier-Bucket1";  
        private readonly string _org = "thu-de";  

 
        public async Task Test_ValidToken_Should_Allow_Connection()
        {
            // Arrange
            var client = new InfluxDBClient(_url, _validToken);
            var writeApi = client.GetWriteApiAsync();

            // Create a test data point
            var point = PointData.Measurement("test_measurement")
                .Field("test_field", 1)
                .Timestamp(DateTime.UtcNow, WritePrecision.Ns);

            try
            {
                // Act: Attempt to write data
                await writeApi.WritePointAsync(point, _bucket, _org);

                // If write succeeds, token is valid
                Assert.True(true, "The client was able to connect and write with the valid token.");
            }
            catch (Exception ex)
            {
                // If any exception occurs, fail the test
                Assert.False(true, $"Expected valid connection and write, but got an error: {ex.Message}");
            }
            finally
            {
                client.Dispose();
            }
        }
    }
}
