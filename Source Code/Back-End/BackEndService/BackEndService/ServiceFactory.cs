using SmartPacifier.BackEnd.Database.InfluxDB.Connection;
using SmartPacifier.Interface.Services;
using InfluxDB.Client;

namespace SmartPacifier.BackEnd
{
    public class ServiceFactory : IServiceFactory
    {
        public IDatabaseService CreateDatabaseService(string url, string token, string bucket, string org)
        {
            // Create the InfluxDBClient using the provided URL and token
            var influxClient = new InfluxDBClient(url, token);

            // Return an instance of InfluxDatabaseService with all required parameters
            return new InfluxDatabaseService(influxClient, token, url, org);
        }
    }
}
