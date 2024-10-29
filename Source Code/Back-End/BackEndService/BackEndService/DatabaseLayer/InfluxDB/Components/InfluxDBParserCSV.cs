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
                    var startTime = data["start_time"]?.ToString()?.Replace("Z", ""); // Remove "Z" from start time
                    var endTime = data["end_time"]?.ToString()?.Replace("Z", ""); // Remove "Z" from end time

                    if (!string.IsNullOrEmpty(campaignName))
                    {
                        if (!campaignDataMap.ContainsKey(campaignName))
                        {
                            campaignDataMap[campaignName] = (Status: "N/A", StartTime: "N/A", EndTime: "N/A");
                        }

                        // Update start or stop time based on status
                        var currentCampaign = campaignDataMap[campaignName];

                        if (status == "started")
                        {
                            currentCampaign = (Status: status, StartTime: startTime ?? currentCampaign.StartTime, EndTime: currentCampaign.EndTime);
                        }
                        else if (status == "stopped")
                        {
                            currentCampaign = (Status: status, StartTime: currentCampaign.StartTime, EndTime: endTime ?? currentCampaign.EndTime);
                        }

                        campaignDataMap[campaignName] = currentCampaign;
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
