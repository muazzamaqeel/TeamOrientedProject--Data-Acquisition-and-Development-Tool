using SmartPacifier.Interface.Services;
using System;
using System.IO;
using System.Text;

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
    }
}
