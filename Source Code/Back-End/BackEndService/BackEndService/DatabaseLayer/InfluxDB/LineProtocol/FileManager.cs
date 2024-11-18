using SmartPacifier.Interface.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace SmartPacifier.BackEnd.DatabaseLayer.InfluxDB.LineProtocol
{
    public class FileManager : ILineProtocol
    {
        public void CreateFileCamp(string campaignName, string entryTime)
        {
            // Define the relative path for the LiveDataFiles directory
            string relativePath = @"Resources\OutputResources\LiveDataFiles";
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string fullPath = Path.Combine(baseDirectory, relativePath);

            // Ensure the directory exists
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            // Base file name using campaignName
            string baseFileName = $"{campaignName}.txt";
            string filePath = Path.Combine(fullPath, baseFileName);
            int fileCounter = 1;

            // Ensure the file name is unique
            while (File.Exists(filePath))
            {
                filePath = Path.Combine(fullPath, $"{campaignName}{fileCounter}.txt");
                fileCounter++;
            }

            // Prepare the content
            var content = new StringBuilder();
            content.AppendLine($"campaign_metadata,campaign_name={campaignName},entry_id=1 status=\"created\",entry_time=\"{entryTime}\"");
            content.AppendLine($"campaign_metadata,campaign_name={campaignName},entry_id=2 status=\"started\",entry_time=\"{entryTime}\"");
            content.AppendLine($"campaign_metadata,campaign_name={campaignName},entry_id=3 status=\"stopped\",entry_time=null");

            try
            {
                // Write the content to the file
                File.WriteAllText(filePath, content.ToString());

                // Verify file creation
                if (File.Exists(filePath))
                {
                    Console.WriteLine($"File created successfully: {filePath}");
                }
                else
                {
                    Console.WriteLine("Failed to create the file.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while creating the file: {ex.Message}");
            }
        }

        public void AppendToCampaignFile(string campaignName, string pacifierName, string sensorType, List<Dictionary<string, object>> parsedData, string entryTime)
        {
            // Define the file path relative to the application's base directory
            string relativePath = @"Resources\OutputResources\LiveDataFiles";
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string filePath = Path.Combine(baseDirectory, relativePath, $"{campaignName}.txt");

            try
            {
                // Ensure the directory exists
                if (!Directory.Exists(Path.Combine(baseDirectory, relativePath)))
                {
                    Directory.CreateDirectory(Path.Combine(baseDirectory, relativePath));
                }

                // Prepare the data to be appended
                var contentBuilder = new StringBuilder();
                foreach (var dictionary in parsedData)
                {
                    contentBuilder.Append($"campaign_metadata,campaign_name={campaignName},pacifier_name={pacifierName},sensor_type={sensorType} ");

                    foreach (var kvp in dictionary)
                    {
                        if (kvp.Key != "sensorGroup")
                        {
                            if (kvp.Value is int)
                            {
                                contentBuilder.Append($"{kvp.Key}={kvp.Value}i,");
                            }
                            else
                            {
                                contentBuilder.Append($"{kvp.Key}={kvp.Value},");
                            }
                        }
                    }

                    // Append entry_time at the end
                    contentBuilder.AppendLine($"entry_time=\"{entryTime}\"");
                }

                // Append the content to the file
                File.AppendAllText(filePath, contentBuilder.ToString());
                Debug.WriteLine($"Data successfully appended to file: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error appending to campaign file: {ex.Message}");
                MessageBox.Show($"Failed to append data to campaign file. Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        public int GetNextEntryId(string filePath)
        {
            try
            {
                var lines = File.ReadAllLines(filePath);
                var lastEntry = lines.LastOrDefault();
                if (lastEntry != null)
                {
                    var entryIdPart = lastEntry.Split(',').FirstOrDefault(part => part.Contains("entry_id"));
                    if (entryIdPart != null && int.TryParse(entryIdPart.Split('=')[1], out int lastEntryId))
                    {
                        return lastEntryId + 1;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting next entry ID: {ex.Message}");
            }

            // Default to 1 if no valid entries found
            return 1;
        }
    }
}
