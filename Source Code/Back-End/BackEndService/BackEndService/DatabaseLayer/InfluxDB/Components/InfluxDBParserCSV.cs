using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
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

            var campaignDataMap = new Dictionary<string, (string Status, string StartTime, string EndTime)>();

            foreach (var entry in influxData)
            {
                var data = JsonNode.Parse(entry);
                if (data != null)
                {
                    var campaignName = data["campaign_name"]?.ToString();
                    var status = data["status"]?.ToString();
                    var startTime = data["start_time"]?.ToString();
                    var endTime = data["end_time"]?.ToString();

                    if (!string.IsNullOrEmpty(campaignName))
                    {
                        if (!campaignDataMap.ContainsKey(campaignName))
                        {
                            campaignDataMap[campaignName] = (Status: "N/A", StartTime: "N/A", EndTime: "N/A");
                        }

                        // Update start or stop time based on status
                        if (status == "started" && campaignDataMap[campaignName].StartTime == "N/A")
                        {
                            campaignDataMap[campaignName] = (Status: status, StartTime: startTime, EndTime: campaignDataMap[campaignName].EndTime);
                        }
                        else if (status == "stopped" && campaignDataMap[campaignName].EndTime == "N/A")
                        {
                            campaignDataMap[campaignName] = (Status: status, StartTime: campaignDataMap[campaignName].StartTime, EndTime: endTime);
                        }
                    }
                }
            }

            // Build CSV rows
            foreach (var kvp in campaignDataMap)
            {
                var line = $"{kvp.Key},{kvp.Value.Status},{kvp.Value.StartTime},{kvp.Value.EndTime}";
                csv.AppendLine(line);
            }

            MessageBox.Show(csv.ToString(), "Load Campaign Data Output", MessageBoxButton.OK, MessageBoxImage.Information);

            return csv.ToString();
        }



    }
}
