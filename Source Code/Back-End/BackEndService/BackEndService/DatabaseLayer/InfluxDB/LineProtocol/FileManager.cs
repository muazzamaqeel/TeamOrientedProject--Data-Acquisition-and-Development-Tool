using SmartPacifier.Interface.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace SmartPacifier.BackEnd.DatabaseLayer.InfluxDB.LineProtocol
{
    public class FileManager : ILineProtocol
    {
        // Path configuration
        private readonly string relativePath = @"Resources\OutputResources\LiveDataFiles";
        private readonly string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        private readonly string fullPath;

        public FileManager()
        {
            fullPath = Path.Combine(baseDirectory, relativePath);
            Directory.CreateDirectory(fullPath);
        }

        public int GetNextEntryId(string campaignName)
        {
            string filePath = Path.Combine(fullPath, $"{campaignName}.txt");
            if (!File.Exists(filePath))
                return 1;

            try
            {
                var lines = File.ReadAllLines(filePath);
                if (lines.Length == 0)
                    return 1;

                var lastLine = lines.LastOrDefault(line => !string.IsNullOrWhiteSpace(line));
                if (lastLine == null)
                    return 1;

                var tagPart = lastLine.Split(' ')[0];
                var tags = tagPart.Split(',');
                var entryIdTag = tags.FirstOrDefault(tag => tag.StartsWith("entry_id=", StringComparison.OrdinalIgnoreCase));
                if (entryIdTag != null && int.TryParse(entryIdTag.Split('=')[1], out int lastEntryId))
                {
                    return lastEntryId + 1;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting next entry ID: {ex.Message}");
            }

            return 1;
        }

        public void CreateFileCamp(string campaignName, string entryTime)
        {
            string filePath = Path.Combine(fullPath, $"{campaignName}.txt");

            if (!DateTime.TryParseExact(entryTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime parsedEntryTime))
            {
                Debug.WriteLine($"Invalid entryTime format: {entryTime}");
                MessageBox.Show($"Invalid entryTime format. Please use 'yyyy-MM-dd HH:mm:ss'.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            long timestamp = ToUnixNanoseconds(parsedEntryTime);

            var content = new StringBuilder();
            content.AppendLine($"campaign_metadata,campaign_name={campaignName},entry_id=1 status=\"created\",entry_time=\"{entryTime}\" {timestamp}");
            content.AppendLine($"campaign_metadata,campaign_name={campaignName},entry_id=2 status=\"started\",entry_time=\"{entryTime}\" {timestamp + 1}");
            content.AppendLine($"campaign_metadata,campaign_name={campaignName},entry_id=3 status=\"stopped\",entry_time=\"{entryTime}\" {timestamp + 2}");

            try
            {
                File.WriteAllText(filePath, content.ToString());
                Debug.WriteLine($"File created successfully: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating file: {ex.Message}");
                MessageBox.Show($"Failed to create campaign file. Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void AppendToCampaignFile(string campaignName, int pacifierCount, string pacifierName, string sensorType, List<Dictionary<string, object>> parsedData, string entryTime)
        {

            MessageBox.Show(
                $"Campaign Name: {campaignName}\n" +
                $"Pacifier Count: {pacifierCount}\n" +
                $"Pacifier Name: {pacifierName}\n" +
                $"Sensor Type: {sensorType}\n" +
                $"Parsed Data: {string.Join(", ", parsedData.Select(d => string.Join(", ", d.Select(kv => $"{kv.Key}: {kv.Value}"))))}\n" +
                $"Entry Time: {entryTime}",
                "Debug Information",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );

            string filePath = Path.Combine(fullPath, $"{campaignName}.txt");
            if (!File.Exists(filePath))
            {
                Debug.WriteLine($"Campaign file does not exist: {filePath}");
                MessageBox.Show($"Campaign file does not exist: {campaignName}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int nextEntryId = GetNextEntryId(campaignName);

            if (!DateTime.TryParseExact(entryTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime parsedEntryTime))
            {
                Debug.WriteLine($"Invalid entryTime format: {entryTime}");
                MessageBox.Show($"Invalid entryTime format. Please use 'yyyy-MM-dd HH:mm:ss'.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            long baseTimestamp = ToUnixNanoseconds(parsedEntryTime);

            try
            {
                string sanitizedPacifierName = pacifierName.Replace(" ", "_");
                string sanitizedSensorType = sensorType.Replace(" ", "_");

                var contentBuilder = new StringBuilder();

                foreach (var sensorData in parsedData)
                {
                    var tagSet = new List<string>
                {
                    $"campaign_name={campaignName}",
                    $"pacifier_count={pacifierCount}",
                    $"pacifier_name={sanitizedPacifierName}",
                    $"sensor_type={sanitizedSensorType}",
                    $"entry_id={nextEntryId}"
                };
                    string tags = string.Join(",", tagSet);

                    var fieldSet = new List<string>();
                    foreach (var kvp in sensorData)
                    {
                        if (kvp.Key.Equals("sensorGroup", StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (kvp.Value is int intValue)
                        {
                            fieldSet.Add($"{kvp.Key}={intValue}i");
                        }
                        else if (kvp.Value is float || kvp.Value is double || kvp.Value is decimal)
                        {
                            string formattedValue = Convert.ToDouble(kvp.Value).ToString(CultureInfo.InvariantCulture);
                            fieldSet.Add($"{kvp.Key}={formattedValue}");
                        }
                        else
                        {
                            fieldSet.Add($"{kvp.Key}=\"{kvp.Value}\"");
                        }
                    }

                    fieldSet.Add($"entry_time=\"{entryTime}\"");

                    string fields = string.Join(",", fieldSet);
                    long timestamp = baseTimestamp + nextEntryId;

                    string lineProtocol = $"campaigns,{tags} {fields} {timestamp}";

                    contentBuilder.AppendLine(lineProtocol);
                    nextEntryId++;
                }

                File.AppendAllText(filePath, contentBuilder.ToString());
                Debug.WriteLine($"Data successfully appended to file: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error appending to campaign file: {ex.Message}");
                MessageBox.Show($"Failed to append data to campaign file. Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private long ToUnixNanoseconds(DateTime dateTime)
        {
            DateTimeOffset dto = dateTime.Kind switch
            {
                DateTimeKind.Utc => new DateTimeOffset(dateTime, TimeSpan.Zero),
                DateTimeKind.Local => new DateTimeOffset(dateTime),
                _ => new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc), TimeSpan.Zero),
            };

            long unixSeconds = dto.ToUnixTimeSeconds();
            long unixNanoseconds = unixSeconds * 1_000_000_000;
            long nanoseconds = (dto.Ticks % TimeSpan.TicksPerSecond) * 100;
            return unixNanoseconds + nanoseconds;
        }
    }

}
