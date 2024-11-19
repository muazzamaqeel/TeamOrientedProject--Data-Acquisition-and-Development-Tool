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

        /// <summary>
        /// Retrieves the next available entry ID for a given campaign.
        /// </summary>
        /// <param name="campaignName">Name of the campaign.</param>
        /// <returns>Next entry ID as an integer.</returns>
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
        /// Creates a new campaign metadata file with initial entries following InfluxDB Line Protocol.
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

            var content = new StringBuilder();
            // Measurement: campaign_metadata
            // Tags: campaign_name, entry_id, pacifier_count
            // Fields: status, entry_time
            content.AppendLine($"campaign_metadata,campaign_name={SanitizeTagValue(campaignName)},entry_id=1,pacifier_count={pacifierCount} status=\"created\",entry_time=\"{entryTime}\"");
            content.AppendLine($"campaign_metadata,campaign_name={SanitizeTagValue(campaignName)},entry_id=2,pacifier_count={pacifierCount} status=\"started\",entry_time=\"{entryTime}\"");
            content.AppendLine($"campaign_metadata,campaign_name={SanitizeTagValue(campaignName)},entry_id=3,pacifier_count={pacifierCount} status=\"stopped\",entry_time=\"{entryTime}\"");

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
        /// Appends sensor data to the campaign file following InfluxDB Line Protocol.
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

                var contentBuilder = new StringBuilder();

                foreach (var sensorData in parsedData)
                {
                    // Generate tags
                    var tagSet = new List<string>
                    {
                        $"campaign_name={SanitizeTagValue(campaignName)}",
                        $"pacifier_name={SanitizeTagValue(pacifierName)}",
                        $"sensor_type={SanitizeTagValue(sensorType)}",
                        $"entry_id={nextEntryId}"
                    };
                    string tags = string.Join(",", tagSet);

                    // Generate fields
                    var fieldSet = new List<string>();
                    foreach (var kvp in sensorData)
                    {
                        string key = kvp.Key;
                        object value = kvp.Value;

                        // Escape field keys if necessary
                        key = EscapeFieldKey(key);

                        if (value is int intValue)
                        {
                            fieldSet.Add($"{key}={intValue}i"); // Integer fields with 'i' suffix
                        }
                        else if (value is float floatValue)
                        {
                            string formattedValue = Math.Round(floatValue, 3).ToString(CultureInfo.InvariantCulture);
                            fieldSet.Add($"{key}={formattedValue}");
                        }
                        else if (value is double doubleValue)
                        {
                            string formattedValue = Math.Round(doubleValue, 3).ToString(CultureInfo.InvariantCulture);
                            fieldSet.Add($"{key}={formattedValue}");
                        }
                        else if (value is decimal decimalValue)
                        {
                            string formattedValue = Math.Round((double)decimalValue, 3).ToString(CultureInfo.InvariantCulture);
                            fieldSet.Add($"{key}={formattedValue}");
                        }
                        else if (value is string stringValue)
                        {
                            // Try to parse the string to a number
                            if (int.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intParsed))
                            {
                                fieldSet.Add($"{key}={intParsed}i"); // Integer fields with 'i' suffix
                            }
                            else if (double.TryParse(stringValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double doubleParsed))
                            {
                                string formattedValue = Math.Round(doubleParsed, 3).ToString(CultureInfo.InvariantCulture);
                                fieldSet.Add($"{key}={formattedValue}");
                            }
                            else
                            {
                                // Escape double quotes and backslashes in string values
                                string escapedValue = EscapeFieldStringValue(stringValue);
                                fieldSet.Add($"{key}=\"{escapedValue}\"");
                            }
                        }
                        else
                        {
                            // For any other types, treat as string
                            string valueAsString = value.ToString();
                            string escapedValue = EscapeFieldStringValue(valueAsString);
                            fieldSet.Add($"{key}=\"{escapedValue}\"");
                        }
                    }

                    // Add the entry_time field as a string
                    fieldSet.Add($"entry_time=\"{entryTime}\"");

                    string fields = string.Join(",", fieldSet);

                    // Construct line protocol entry without timestamp
                    string lineProtocol = $"campaigns,{tags} {fields}";

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
        /// Updates the 'stopped' entry's entry_time in the campaign metadata following InfluxDB Line Protocol.
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

            try
            {
                var lines = File.ReadAllLines(filePath).ToList();
                bool entryUpdated = false;

                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].Contains("status=\"stopped\""))
                    {
                        var parts = lines[i].Split(' ');
                        if (parts.Length >= 2)
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

                            // Reconstruct the line without timestamp
                            lines[i] = $"{parts[0]} {updatedFieldPart}";

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
        /// Escapes special characters in field keys as per InfluxDB line protocol.
        /// </summary>
        /// <param name="key">The field key to escape.</param>
        /// <returns>The escaped field key.</returns>
        private string EscapeFieldKey(string key)
        {
            return key.Replace("\\", "\\\\")
                      .Replace(" ", "\\ ")
                      .Replace(",", "\\,")
                      .Replace("=", "\\=");
        }

        /// <summary>
        /// Escapes special characters in field string values as per InfluxDB line protocol.
        /// </summary>
        /// <param name="value">The field string value to escape.</param>
        /// <returns>The escaped field string value.</returns>
        private string EscapeFieldStringValue(string value)
        {
            return value.Replace("\\", "\\\\")
                        .Replace("\"", "\\\"")
                        .Replace("\n", "\\n")
                        .Replace("\r", "\\r");
        }

        /// <summary>
        /// Removes spaces from tag values.
        /// </summary>
        /// <param name="value">The tag value to sanitize.</param>
        /// <returns>The sanitized tag value.</returns>
        private string SanitizeTagValue(string value)
        {
            return value.Replace(" ", "");
        }
    }
}
