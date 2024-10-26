using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;

namespace SmartPacifier.BackEnd.DatabaseLayer.InfluxDB.LoadFiles.Code
{
    public class LoadFilesExcel
    {
        private readonly string _filePath = "CampaignData.xlsx";

        public void SaveToExcel(string measurement, Dictionary<string, object> fields, Dictionary<string, string> tags)
        {
            // Check if the file exists, if not create a new one
            using (var workbook = new XLWorkbook(_filePath))
            {
                var worksheet = workbook.Worksheets.Contains(measurement)
                    ? workbook.Worksheet(measurement)
                    : workbook.AddWorksheet(measurement);

                var row = worksheet.LastRowUsed()?.RowNumber() + 1 ?? 1;

                // Write tags
                int col = 1;
                foreach (var tag in tags)
                {
                    worksheet.Cell(row, col++).Value = tag.Value;
                }

                // Write fields with explicit conversion
                foreach (var field in fields)
                {
                    worksheet.Cell(row, col++).Value = ConvertToXLCellValue(field.Value);
                }

                worksheet.Cell(row, col).Value = DateTime.UtcNow;

                workbook.Save();
            }
        }

        private XLCellValue ConvertToXLCellValue(object value)
        {
            return value switch
            {
                string stringValue => stringValue,
                int intValue => intValue,
                double doubleValue => doubleValue,
                float floatValue => floatValue,
                bool boolValue => boolValue,
                DateTime dateTimeValue => dateTimeValue,
                _ => value?.ToString() // Default to string if type is unknown
            };
        }

        public IEnumerable<Dictionary<string, object>> LoadFromExcel(string measurement)
        {
            var data = new List<Dictionary<string, object>>();

            if (!File.Exists(_filePath))
                return data;

            using (var workbook = new XLWorkbook(_filePath))
            {
                if (!workbook.Worksheets.Contains(measurement))
                    return data;

                var worksheet = workbook.Worksheet(measurement);

                foreach (var row in worksheet.RowsUsed())
                {
                    var rowData = new Dictionary<string, object>();
                    int col = 1;

                    foreach (var cell in row.Cells())
                    {
                        rowData[$"Column{col++}"] = cell.Value;
                    }

                    data.Add(rowData);
                }
            }

            return data;
        }
    }
}
