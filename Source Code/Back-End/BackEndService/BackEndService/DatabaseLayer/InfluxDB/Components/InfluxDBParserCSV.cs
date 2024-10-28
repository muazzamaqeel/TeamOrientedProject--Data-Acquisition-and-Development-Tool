using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;
using SmartPacifier.Interface.Services;

namespace SmartPacifier.BackEnd.DatabaseLayer.InfluxDB.Components
{
    public class InfluxDBParserCSV : IInfluxDBParser
    {
        public string ConvertToCSV(List<string> influxData)
        {
            var csv = new StringBuilder();
            csv.AppendLine("CampaignName,Status,StartDate,EndDate");

            foreach (var entry in influxData)
            {
                var data = JsonNode.Parse(entry);
                if (data != null)
                {
                    var line = $"{data["campaign_name"]},{data["status"]},{data["start_time"]},{data["end_time"]}";
                    csv.AppendLine(line);

                    // Show each line for debugging
                    MessageBox.Show(line, "CSV Line Generated", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }

            return csv.ToString();
        }

    }
}