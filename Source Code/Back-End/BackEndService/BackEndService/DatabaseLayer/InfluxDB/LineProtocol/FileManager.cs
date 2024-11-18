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

        /// <summary>
        /// Creates a new campaign metadata file with initial entries.
        /// </summary>
        /// <param name="campaignName">Name of the campaign.</param>
        /// <param name="pacifierCount">Number of pacifiers in the campaign.</param>
        /// <param name="entryTime">Entry time in "yyyy-MM-dd HH:mm:ss" format.</param>
        public void CreateFileCamp(string campaignName, int pacifierCount, string entryTime)
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
            content.AppendLine($"campaign_metadata,campaign_name={campaignName},pacifier_count={pacifierCount},entry_id=1 status=\"created\",entry_time=\"{entryTime}\" {timestamp}");
            content.AppendLine($"campaign_metadata,campaign_name={campaignName},pacifier_count={pacifierCount},entry_id=2 status=\"started\",entry_time=\"{entryTime}\" {timestamp + 1}");
            content.AppendLine($"campaign_metadata,campaign_name={campaignName},pacifier_count={pacifierCount},entry_id=3 status=\"stopped\",entry_time=\"{entryTime}\" {timestamp + 2}");

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


        /// <summary>
        /// Appends sensor data to the campaign file.
        /// </summary>
        /// <param name="campaignName">Name of the campaign.</param>
        /// <param name="pacifierCount">Number of pacifiers (not used here).</param>
        /// <param name="pacifierName">Name of the pacifier.</param>
        /// <param name="sensorType">Type of the sensor.</param>
        /// <param name="parsedData">List of dictionaries containing sensor data.</param>
        /// <param name="entryTime">Entry time in "yyyy-MM-dd HH:mm:ss" format.</param>
        public void AppendToCampaignFile(string campaignName, int pacifierCount, string pacifierName, string sensorType, List<Dictionary<string, object>> parsedData, string entryTime)
        {
            try
            {
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

                string sanitizedPacifierName = pacifierName.Replace(" ", "_");
                string sanitizedSensorType = sensorType.Replace(" ", "_");

                var contentBuilder = new StringBuilder();

                foreach (var sensorData in parsedData)
                {
                    // Generate tags
                    var tagSet = new List<string>
            {
                $"campaign_name={campaignName}",
                $"pacifier_name={sanitizedPacifierName}",
                $"sensor_type={sanitizedSensorType}",
                $"entry_id={nextEntryId}"
            };
                    string tags = string.Join(",", tagSet);

                    // Generate fields
                    var fieldSet = new List<string>();
                    foreach (var kvp in sensorData)
                    {
                        if (kvp.Value is int intValue)
                        {
                            fieldSet.Add($"{kvp.Key}={intValue}"); // Removed the 'i' suffix
                        }
                        else if (kvp.Value is float || kvp.Value is double || kvp.Value is decimal)
                        {
                            string formattedValue = Math.Round(Convert.ToDouble(kvp.Value), 3).ToString(CultureInfo.InvariantCulture);
                            fieldSet.Add($"{kvp.Key}={formattedValue}");
                        }
                        else if (kvp.Value is string stringValue)
                        {
                            fieldSet.Add($"{kvp.Key}=\"{stringValue}\"");
                        }
                    }

                    fieldSet.Add($"entry_time=\"{entryTime}\"");
                    string fields = string.Join(",", fieldSet);

                    // Construct line protocol entry
                    long timestamp = baseTimestamp + nextEntryId;
                    string lineProtocol = $"campaigns,{tags} {fields} {timestamp}";

                    contentBuilder.AppendLine(lineProtocol);
                    nextEntryId++;
                }

                // Append to the file
                File.AppendAllText(filePath, contentBuilder.ToString());
                Debug.WriteLine($"Data successfully appended to file: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error appending to campaign file: {ex.Message}");
                MessageBox.Show($"Failed to append data to campaign file. Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Updates the 'stopped' entry's end time in the campaign metadata.
        /// </summary>
        /// <param name="campaignName">Name of the campaign.</param>
        /// <param name="newEndTime">New end time in "yyyy-MM-dd HH:mm:ss" format.</param>
        public void UpdateStoppedEntryTime(string campaignName, string newEndTime)
        {
            string filePath = Path.Combine(fullPath, $"{campaignName}.txt");

            if (!File.Exists(filePath))
            {
                Debug.WriteLine($"Campaign file does not exist: {filePath}");
                MessageBox.Show($"Campaign file does not exist: {campaignName}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!DateTime.TryParseExact(newEndTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime parsedEndTime))
            {
                Debug.WriteLine($"Invalid endTime format: {newEndTime}");
                MessageBox.Show($"Invalid endTime format. Please use 'yyyy-MM-dd HH:mm:ss'.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            long updatedTimestamp = ToUnixNanoseconds(parsedEndTime);

            try
            {
                var lines = File.ReadAllLines(filePath).ToList();
                bool entryUpdated = false;

                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].Contains("status=\"stopped\""))
                    {
                        var parts = lines[i].Split(' ');
                        if (parts.Length >= 3)
                        {
                            // Update entry_time field
                            string fieldPart = parts[1];
                            var fields = fieldPart.Split(',');
                            for (int j = 0; j < fields.Length; j++)
                            {
                                if (fields[j].StartsWith("entry_time=", StringComparison.OrdinalIgnoreCase))
                                {
                                    fields[j] = $"entry_time=\"{newEndTime}\"";
                                    break;
                                }
                            }
                            string updatedFieldPart = string.Join(",", fields);

                            // Replace the primary timestamp (third part)
                            parts[2] = updatedTimestamp.ToString();

                            // Reconstruct the line with updated fields and timestamp
                            lines[i] = $"{parts[0]} {updatedFieldPart} {parts[2]}";

                            entryUpdated = true;
                            break; // Assuming only one 'stopped' entry exists
                        }
                    }
                }

                if (entryUpdated)
                {
                    File.WriteAllLines(filePath, lines);
                    Debug.WriteLine($"Successfully updated 'stopped' entry in file: {filePath}");
                    MessageBox.Show("Campaign 'stopped' entry has been successfully updated.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    Debug.WriteLine($"No 'stopped' entry found in file: {filePath}");
                    MessageBox.Show($"No 'stopped' entry found to update in campaign: {campaignName}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating 'stopped' entry: {ex.Message}");
                MessageBox.Show($"Failed to update 'stopped' entry. Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        /// <summary>
        /// Converts a DateTime to Unix timestamp in nanoseconds.
        /// </summary>
        /// <param name="dateTime">The DateTime to convert.</param>
        /// <returns>Unix timestamp in nanoseconds.</returns>
        /// 


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
