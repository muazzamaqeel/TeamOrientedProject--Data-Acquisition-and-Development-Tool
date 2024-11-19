using System;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace SmartPacifier___TestingFramework.UnitTests.UTBackEnd.UTAlgorithmLayer
{
    public class TestAlgorithm
    {
        private const string PythonScriptPath = "path/to/your/test_script.py";

        [Fact]
        public async Task SendDataToScriptAndGetResponse_ReturnsExpectedResponse()
        {
            // Arrange
            var scriptEngine = new PythonScriptEngine();
            var inputData = new
            {
                key = "testKey",
                value = "testValue"
            };

            string dataJson = JsonSerializer.Serialize(inputData);

            // Act
            string responseJson = await scriptEngine.ExecuteScriptAsync(PythonScriptPath, dataJson, usePort: false);
            var response = JsonSerializer.Deserialize<ResponseModel>(responseJson);

            // Assert
            Assert.NotNull(response);
            Assert.Equal("received", response.Status);
            Assert.Equal(inputData.key, response.Data.key);
            Assert.Equal(inputData.value, response.Data.value);
        }

        private class ResponseModel
        {
            public string Status { get; set; }
            public dynamic Data { get; set; }
        }
    }
}
