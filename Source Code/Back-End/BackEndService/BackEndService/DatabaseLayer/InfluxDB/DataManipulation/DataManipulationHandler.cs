using InfluxDB.Client;
using SmartPacifier.Interface.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace SmartPacifier.BackEnd.DatabaseLayer.InfluxDB.DataManipulation
{
    public class DataManipulationHandler : IDataManipulationHandler
    {
        private readonly IDatabaseService _databaseService;

        public DataManipulationHandler(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        /// <summary>
        /// Updates a row by creating a new entry with modified data and deleting the old entry.
        /// </summary>
        public async Task UpdateRowAsync(
            string measurement,
            Dictionary<string, string> originalTags,
            long originalTimestampNanoseconds,
            Dictionary<string, object> newFields,
            Dictionary<string, string> newTags)
        {
            // Step 1: Delete the original row
            await DeleteRowAsync(measurement, originalTags, originalTimestampNanoseconds);

            // Step 2: Insert the new data as a new row
            await _databaseService.WriteDataAsync(measurement, newFields, newTags);
        }

        /// <summary>
        /// Deletes a row based on measurement, tags, and timestamp.
        /// </summary>
        private async Task DeleteRowAsync(string measurement, Dictionary<string, string> tags, long timestampNanoseconds)
        {
            // Convert nanoseconds to ticks and create a DateTime with appropriate bounds
            long timestampTicks = timestampNanoseconds / 100; // 1 tick = 100 nanoseconds
            DateTime originalTimestamp;

            try
            {
                originalTimestamp = new DateTime(timestampTicks, DateTimeKind.Utc);
            }
            catch (ArgumentOutOfRangeException)
            {
                MessageBox.Show("Timestamp out of range for InfluxDB.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Define InfluxDB's minimum valid timestamp
            DateTime minValidTime = new DateTime(1677, 9, 21, 0, 12, 43, 145, DateTimeKind.Utc).AddTicks(2241940);

            // Ensure start time is within valid bounds for InfluxDB
            DateTime start = originalTimestamp.AddTicks(-1);
            if (start < minValidTime)
            {
                start = minValidTime;
            }

            // Set end time as 1 tick after the original timestamp for a safe range
            DateTime end = originalTimestamp.AddTicks(1);

            // Build the filter conditions and query
            string filterConditions = $"r[\"_measurement\"] == \"{measurement}\"";
            foreach (var tag in tags)
            {
                filterConditions += $" and r[\"{tag.Key}\"] == \"{tag.Value}\"";
            }

            string query = $@"
                from(bucket: ""{_databaseService.Bucket}"")
                |> range(start: {start:yyyy-MM-ddTHH:mm:ss.fffffffZ}, stop: {end:yyyy-MM-ddTHH:mm:ss.fffffffZ})
                |> filter(fn: (r) => {filterConditions})";

            try
            {
                await _databaseService.GetClient().GetDeleteApi().Delete(start, end, query, _databaseService.Bucket, _databaseService.Org);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}\n\n" +
                                $"Debug Information:\n" +
                                $"Original Timestamp (ns): {timestampNanoseconds}\n" +
                                $"Converted Timestamp (ticks): {timestampTicks}\n" +
                                $"Start Time: {start:yyyy-MM-ddTHH:mm:ss.fffffffZ}\n" +
                                $"End Time: {end:yyyy-MM-ddTHH:mm:ss.fffffffZ}\n\n" +
                                $"Query:\n{query}",
                                "Delete Operation Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
