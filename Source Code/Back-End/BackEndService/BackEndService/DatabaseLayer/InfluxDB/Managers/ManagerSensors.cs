using SmartPacifier.Interface.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartPacifier.BackEnd.DatabaseLayer.InfluxDB.Managers
{
    public class ManagerSensors : IManagerSensors
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

        public async Task<List<string>> ReadData(string query)
        {
            return await _databaseService.ReadData(query);
        }

        public async Task<List<string>> GetCampaignsAsync()
        {
            return await _databaseService.GetCampaignsAsync();
        }

        public async Task AddSensorDataAsync(string pacifierId, float ppgValue, float imuAccelX, float imuAccelY, float imuAccelZ)
        {
            var ppgTags = new Dictionary<string, string>
            {
                { "pacifier_id", pacifierId },
                { "sensor_type", "PPG_IMU" }
            };
            var ppgFields = new Dictionary<string, object> { { "ppg_value", ppgValue } };

            await _databaseService.WriteDataAsync("sensor_data", ppgFields, ppgTags);
            Console.WriteLine($"Added sensor: PPG_IMU to pacifier: {pacifierId}");

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
