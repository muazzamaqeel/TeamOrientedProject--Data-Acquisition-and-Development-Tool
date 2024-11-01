using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using SmartPacifier.Interface.Services;

namespace SmartPacifier.BackEnd.DatabaseLayer.CSV
{
    public class CSVCampaignCreation: ICSVDataHandler
    {
        // Base directory for saving files
        private readonly string baseDirectory = Path.Combine("BackEndService", "BackEndService", "DatabaseLayer", "CSV", "Files");

        public CSVCampaignCreation()
        {
            // Ensure the directory exists
            Directory.CreateDirectory(baseDirectory);
        }

        // Function to create initial CSV file with campaign metadata
        public void CreateCSV(string campaignName, List<string> pacifierNames)
        {
            string filePath = Path.Combine(baseDirectory, $"{campaignName}.csv"); // Save file in specified directory
            var records = new List<CampaignData>();
            string creationTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

            foreach (var pacifier in pacifierNames)
            {
                records.Add(new CampaignData
                {
                    Measurement = "campaign_metadata",
                    CampaignName = campaignName,
                    PacifierName = pacifier,
                    Status = "created",
                    Creation = creationTime
                });
            }

            WriteToCSV(filePath, records);
        }

        // Function to start the campaign and begin recording sensor data
        public void StartCampaign(string campaignName, List<string> pacifierNames)
        {
            string filePath = Path.Combine(baseDirectory, $"{campaignName}.csv"); // Use campaign_name to identify the file
            var records = new List<CampaignData>();
            string startTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

            // Add an entry for the campaign start status
            records.Add(new CampaignData
            {
                Measurement = "campaign_metadata",
                CampaignName = campaignName,
                Status = "started",
                StartTime = startTime
            });

            // Add sensor data entries for each pacifier
            foreach (var pacifier in pacifierNames)
            {
                records.Add(new CampaignData
                {
                    Measurement = "campaigns",
                    CampaignName = campaignName,
                    PacifierName = pacifier,
                    SensorType = "ppg",
                    Led1 = 100,  // Dummy data
                    Led2 = 110,  // Dummy data
                    Led3 = 105,  // Dummy data
                    Temperature = 36.5,  // Dummy data
                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)
                });

                records.Add(new CampaignData
                {
                    Measurement = "campaigns",
                    CampaignName = campaignName,
                    PacifierName = pacifier,
                    SensorType = "imu",
                    AccX = -0.02,  // Dummy data
                    AccY = 0.03,   // Dummy data
                    AccZ = -0.01,  // Dummy data
                    GyroX = -0.0,  // Dummy data
                    GyroY = -0.1,  // Dummy data
                    GyroZ = 0.15,  // Dummy data
                    MagX = 0.03,   // Dummy data
                    MagY = -0.05,  // Dummy data
                    MagZ = 0.08,   // Dummy data
                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)
                });
            }

            WriteToCSV(filePath, records, append: true);
        }

        // Function to end the campaign
        public void EndCampaign(string campaignName)
        {
            string filePath = Path.Combine(baseDirectory, $"{campaignName}.csv"); // Use campaign_name to identify the file
            var records = new List<CampaignData>();
            string endTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

            // Add an entry for the campaign end status
            records.Add(new CampaignData
            {
                Measurement = "campaign_metadata",
                CampaignName = campaignName,
                Status = "stopped",
                EndTime = endTime
            });

            WriteToCSV(filePath, records, append: true);
        }

        // Helper function to write or append records to CSV
        private void WriteToCSV(string filePath, List<CampaignData> records, bool append = false)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = !append || !File.Exists(filePath)
            };

            using (var writer = new StreamWriter(filePath, append: append))
            using (var csv = new CsvWriter(writer, config))
            {
                csv.WriteRecords(records);
            }
        }
    }




    public class CampaignData
    {
        public string Measurement { get; set; }
        public string CampaignName { get; set; }
        public string PacifierName { get; set; }
        public string SensorType { get; set; }
        public int? Led1 { get; set; }
        public int? Led2 { get; set; }
        public int? Led3 { get; set; }
        public double? Temperature { get; set; }
        public double? AccX { get; set; }
        public double? AccY { get; set; }
        public double? AccZ { get; set; }
        public double? GyroX { get; set; }
        public double? GyroY { get; set; }
        public double? GyroZ { get; set; }
        public double? MagX { get; set; }
        public double? MagY { get; set; }
        public double? MagZ { get; set; }
        public string? Status { get; set; }
        public string? Creation { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public string? Timestamp { get; set; }
    }
}
