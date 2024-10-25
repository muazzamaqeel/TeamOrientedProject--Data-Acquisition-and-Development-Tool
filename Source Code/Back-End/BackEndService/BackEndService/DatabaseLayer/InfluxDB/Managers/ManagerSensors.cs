using SmartPacifier.Interface.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartPacifier.BackEnd.DatabaseLayer.InfluxDB.Managers
{
    public partial class ManagerSensors : IManagerSensors
    {
        private readonly IDatabaseService _databaseService;

        public ManagerSensors(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task WriteDataAsync(string measurement, Dictionary<string, object> fields, Dictionary<string, string> tags)
        {
            await _databaseService.WriteDataAsync(measurement, fields, tags);
        }

        public Task<List<string>> ReadData(string query)
        {
            return _databaseService.ReadData(query); // Make this async to return Task
        }

        public async Task<List<string>> GetCampaignsAsync()
        {
            return await _databaseService.GetCampaignsAsync();
        }

        // Method to add PPG_IMU and IMU sensor data
        public async Task AddSensorDataAsync(string pacifierId, float ppgValue, float imuAccelX, float imuAccelY, float imuAccelZ)
        {
            // First, add the PPG_IMU sensor data
            var ppgTags = new Dictionary<string, string>
            {
                { "pacifier_id", pacifierId },
                { "sensor_type", "PPG_IMU" }
            };

            var ppgFields = new Dictionary<string, object>
            {
                { "ppg_value", ppgValue }
            };

            await _databaseService.WriteDataAsync("sensor_data", ppgFields, ppgTags);
            Console.WriteLine($"Added sensor: PPG_IMU to pacifier: {pacifierId}");

            // Then, add the IMU sensor data
            var imuTags = new Dictionary<string, string>
            {
                { "pacifier_id", pacifierId },
                { "sensor_type", "IMU_sensor" }
            };

            var imuFields = new Dictionary<string, object>
            {
                { "imu_accel_x", imuAccelX },
                { "imu_accel_y", imuAccelY },
                { "imu_accel_z", imuAccelZ }
            };

            await _databaseService.WriteDataAsync("sensor_data", imuFields, imuTags);
            Console.WriteLine($"Added sensor: IMU_sensor to pacifier: {pacifierId}");
        }
    }
}
