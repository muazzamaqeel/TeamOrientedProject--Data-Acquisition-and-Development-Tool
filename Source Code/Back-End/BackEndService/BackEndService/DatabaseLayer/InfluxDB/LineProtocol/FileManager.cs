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
        /// Retrieves the next entry ID for a given campaign by reading the last entry in the file.
        /// </summary>
        /// <param name="campaignName">The name of the campaign.</param>
        /// <returns>The next entry ID.</returns>
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

                // Extract entry_id from the last line's tags
                var tagPart = lastLine.Split(' ')[0]; // Get the tag part before the first space
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
        /// Creates a new campaign file with initial metadata entries.
        /// </summary>
        /// <param name="campaignName">The name of the campaign.</param>
        /// <param name="entryTime">The entry time for the campaign in "yyyy-MM-dd HH:mm:ss" format.</param>
        public void CreateFileCamp(string campaignName, string entryTime)
        {
            string filePath = Path.Combine(fullPath, $"{campaignName}.txt");

            // Convert entryTime to Unix nanoseconds timestamp
            if (!DateTime.TryParseExact(entryTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime parsedEntryTime))
            {
                Debug.WriteLine($"Invalid entryTime format: {entryTime}");
                MessageBox.Show($"Invalid entryTime format. Please use 'yyyy-MM-dd HH:mm:ss'.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            long timestamp = ToUnixNanoseconds(parsedEntryTime);

            // Prepare the initial content
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

        /// <summary>
        /// Appends sensor data to an existing campaign file.
        /// </summary>
        /// <param name="campaignName">The name of the campaign.</param>
        /// <param name="pacifierName">The name of the pacifier.</param>
        /// <param name="sensorType">The type of sensor.</param>
        /// <param name="parsedData">A list of dictionaries containing sensor data fields.</param>
        /// <param name="entryTime">The entry time for the sensor data in "yyyy-MM-dd HH:mm:ss" format.</param>
        public void AppendToCampaignFile(string campaignName, string pacifierName, string sensorType, List<Dictionary<string, object>> parsedData, string entryTime)
        {
            string filePath = Path.Combine(fullPath, $"{campaignName}.txt");
            if (!File.Exists(filePath))
            {
                Debug.WriteLine($"Campaign file does not exist: {filePath}");
                MessageBox.Show($"Campaign file does not exist: {campaignName}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int nextEntryId = GetNextEntryId(campaignName);

            // Convert entryTime to Unix nanoseconds timestamp
            if (!DateTime.TryParseExact(entryTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime parsedEntryTime))
            {
                Debug.WriteLine($"Invalid entryTime format: {entryTime}");
                MessageBox.Show($"Invalid entryTime format. Please use 'yyyy-MM-dd HH:mm:ss'.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            long baseTimestamp = ToUnixNanoseconds(parsedEntryTime);

            try
            {
                // Sanitize tag values
                string sanitizedPacifierName = pacifierName.Replace(" ", "_");
                string sanitizedSensorType = sensorType.Replace(" ", "_");

                var contentBuilder = new StringBuilder();

                foreach (var sensorData in parsedData)
                {
                    // Build the tag set
                    var tagSet = new List<string>
                    {
                        $"campaign_name={campaignName}",
                        $"pacifier_name={sanitizedPacifierName}",
                        $"sensor_type={sanitizedSensorType}",
                        $"entry_id={nextEntryId}"
                    };
                    string tags = string.Join(",", tagSet);

                    // Build the field set
                    var fieldSet = new List<string>();
                    foreach (var kvp in sensorData)
                    {
                        if (kvp.Key.Equals("sensorGroup", StringComparison.OrdinalIgnoreCase))
                            continue; // Skip if needed

                        if (kvp.Value is int intValue)
                        {
                            fieldSet.Add($"{kvp.Key}={intValue}i");
                        }
                        else if (kvp.Value is float || kvp.Value is double || kvp.Value is decimal)
                        {
                            // Format to ensure dot as decimal separator
                            string formattedValue = Convert.ToDouble(kvp.Value).ToString(CultureInfo.InvariantCulture);
                            fieldSet.Add($"{kvp.Key}={formattedValue}");
                        }
                        else
                        {
                            // Assume string
                            fieldSet.Add($"{kvp.Key}=\"{kvp.Value}\"");
                        }
                    }

                    // Always include entry_time as a field for readability
                    fieldSet.Add($"entry_time=\"{entryTime}\"");

                    string fields = string.Join(",", fieldSet);

                    // Compute timestamp for this entry
                    long timestamp = baseTimestamp + nextEntryId; // Increment timestamp if needed

                    // Build the full Line Protocol entry
                    string lineProtocol = $"campaigns,{tags} {fields} {timestamp}";

                    // Append to content
                    contentBuilder.AppendLine(lineProtocol);

                    // Increment entry ID for next sensor data
                    nextEntryId++;
                }

                // Append all entries at once
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
        /// Converts a DateTime to Unix timestamp in nanoseconds.
        /// </summary>
        /// <param name="dateTime">The DateTime to convert. Assumed to be in UTC.</param>
        /// <returns>Unix timestamp in nanoseconds.</returns>
        private long ToUnixNanoseconds(DateTime dateTime)
        {
            DateTimeOffset dto;

            // Handle different DateTime kinds to prevent ArgumentException
            switch (dateTime.Kind)
            {
                case DateTimeKind.Utc:
                    dto = new DateTimeOffset(dateTime, TimeSpan.Zero);
                    break;
                case DateTimeKind.Local:
                    dto = new DateTimeOffset(dateTime);
                    break;
                case DateTimeKind.Unspecified:
                default:
                    // Assume it's UTC if unspecified
                    dto = new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc), TimeSpan.Zero);
                    break;
            }

            long unixSeconds = dto.ToUnixTimeSeconds();
            long unixNanoseconds = unixSeconds * 1_000_000_000;

            // Convert ticks to nanoseconds (1 tick = 100 nanoseconds)
            long nanoseconds = (dto.Ticks % TimeSpan.TicksPerSecond) * 100;
            unixNanoseconds += nanoseconds;

            return unixNanoseconds;
        }
    }
}
