using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;
using SmartPacifier.Interface.Services;
using InfluxDB.Client.Api.Domain;
using System.Net.Http;

namespace SmartPacifier.BackEnd.DatabaseLayer.InfluxDB.LineProtocol
{
    public class FileUpload
    {
        private readonly string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        private readonly string relativePath = @"Resources\OutputResources\LiveDataFiles";
        private readonly string fullPath;
        private readonly IDatabaseService _databaseService;

        public FileUpload(IDatabaseService databaseService)
        {
            fullPath = Path.Combine(baseDirectory, relativePath);
            _databaseService = databaseService;
        }

        /// <summary>
        /// Uploads the data from the campaign file to InfluxDB after confirming internet connectivity.
        /// </summary>
        /// <param name="campaignName">The name of the campaign whose data is to be uploaded.</param>
        public async Task UploadDataAsync(string campaignName)
        {
            // Ask the user if they are connected to the internet
            var result = MessageBox.Show("Are you connected to the internet?", "Internet Connection", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                if (!IsInternetConnected())
                {
                    MessageBox.Show("No internet connection detected. Please check your connection and try again.", "No Internet", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Read the data from the file
                string filePath = Path.Combine(fullPath, $"{campaignName}.txt");

                if (!System.IO.File.Exists(filePath))
                {
                    MessageBox.Show($"Campaign file does not exist: {campaignName}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                try
                {
                    string lineProtocolData = await System.IO.File.ReadAllTextAsync(filePath);

                    // Send data to InfluxDB
                    await WriteDataToInfluxDB(lineProtocolData);

                    MessageBox.Show("Data successfully uploaded to InfluxDB.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error uploading data to InfluxDB: {ex.Message}");
                    MessageBox.Show($"Failed to upload data to InfluxDB. Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                // User selected No
                MessageBox.Show("Data upload skipped.", "Upload Skipped", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Checks if the internet is connected by attempting to reach a known website.
        /// </summary>
        /// <returns>True if connected; otherwise, false.</returns>
        private bool IsInternetConnected()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    // Set a reasonable timeout
                    client.Timeout = TimeSpan.FromSeconds(5);
                    var result = client.GetAsync("https://www.google.com").Result;
                    return result.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Writes the Line Protocol data to InfluxDB using the InfluxDBClient.
        /// </summary>
        /// <param name="lineProtocolData">The data to write, in Line Protocol format.</param>
        private async Task WriteDataToInfluxDB(string lineProtocolData)
        {
            var influxClient = _databaseService.GetClient();
            var bucket = _databaseService.Bucket;
            var org = _databaseService.Org;

            var writeApi = influxClient.GetWriteApiAsync();

            // Split the line protocol data into individual records
            var lines = lineProtocolData.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            await writeApi.WriteRecordsAsync(lines, WritePrecision.Ns, bucket, org);
        }
    }
}
