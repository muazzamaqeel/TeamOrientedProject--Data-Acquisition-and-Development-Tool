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
        public async Task DeleteRowAsync(string measurement, Dictionary<string, string> tags, long timestampNanoseconds)
        {
            // Convert nanoseconds to ticks for DateTime
            long timestampTicks = timestampNanoseconds / 100;
            DateTime timestamp = new DateTime(timestampTicks, DateTimeKind.Utc);

            // Set start and end times around the original timestamp
            DateTime start = timestamp.AddTicks(-1);
            DateTime end = timestamp.AddTicks(1);

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
                MessageBox.Show($"Error during deletion: {ex.Message}", "Delete Operation Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        public async Task CreateNewEntryAsync(
            string measurement,
            Dictionary<string, object> fields,
            Dictionary<string, string> tags)
        {
            await _databaseService.WriteDataAsync(measurement, fields, tags);
        }



    }
}
