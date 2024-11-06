using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using SmartPacifier.Interface.Services;

namespace Smart_Pacifier___Tool.Tabs.DeveloperTab
{
    public partial class EditDataWindow : Window
    {
        private readonly IDataManipulationHandler _dataManipulationHandler; // Use interface
        private readonly Dictionary<string, object> _originalData;
        private readonly Dictionary<string, TextBox> _textBoxes = new Dictionary<string, TextBox>();

        public EditDataWindow(IDataManipulationHandler dataManipulationHandler, Dictionary<string, object> originalData)
        {
            InitializeComponent();
            _dataManipulationHandler = dataManipulationHandler;
            _originalData = originalData;

            // Dynamically create TextBoxes based on original data keys
            CreateDynamicFields();
        }

        private void CreateDynamicFields()
        {
            foreach (var kvp in _originalData)
            {
                // Create a label for each field
                var label = new TextBlock
                {
                    Text = kvp.Key,
                    Margin = new Thickness(0, 10, 0, 5)
                };
                DynamicFieldsPanel.Children.Add(label);

                // Create a TextBox for each field
                var textBox = new TextBox
                {
                    Text = kvp.Value?.ToString() ?? string.Empty,
                    Name = kvp.Key.Replace(" ", "_") + "TextBox",
                    Margin = new Thickness(0, 0, 0, 10)
                };

                // Store the TextBox for later access
                _textBoxes[kvp.Key] = textBox;
                DynamicFieldsPanel.Children.Add(textBox);
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var newTags = new Dictionary<string, string>();
            var newFields = new Dictionary<string, object>();
            string timestampText = null;
            string timestampKey = null;

            foreach (var kvp in _textBoxes)
            {
                string key = kvp.Key;
                string value = kvp.Value.Text;

                // Check if the current key might be the timestamp field
                if (key.ToLower().Contains("timestamp"))
                {
                    timestampText = value;
                    timestampKey = key;
                    continue;  // Skip adding to fields or tags
                }

                // Determine if this key should be a tag or a field based on its original usage
                if (_originalData.ContainsKey(key) && (_originalData[key] is string))
                {
                    newTags[key] = value;
                }
                else
                {
                    // Convert to appropriate data types if needed
                    if (double.TryParse(value, out double numericValue))
                    {
                        newFields[key] = numericValue;
                    }
                    else
                    {
                        newFields[key] = value;
                    }
                }
            }

            // Retrieve original tags if they exist in _originalData
            var originalTags = new Dictionary<string, string>();
            if (_originalData.TryGetValue("campaign_name", out var campaignName))
                originalTags["campaign_name"] = campaignName.ToString();
            if (_originalData.TryGetValue("pacifier_name", out var pacifierName))
                originalTags["pacifier_name"] = pacifierName.ToString();
            if (_originalData.TryGetValue("sensor_type", out var sensorType))
                originalTags["sensor_type"] = sensorType.ToString();

            // Validate and parse the timestamp
            if (!string.IsNullOrEmpty(timestampText) && DateTime.TryParseExact(timestampText, "yyyy-MM-ddTHH:mm:ss.fffffffZ",
                                          System.Globalization.CultureInfo.InvariantCulture,
                                          System.Globalization.DateTimeStyles.AssumeUniversal,
                                          out DateTime originalTimestamp))
            {
                // Convert DateTime to Unix timestamp in nanoseconds
                long timestampInNanoseconds = originalTimestamp.ToUniversalTime().Ticks * 100; // 1 tick = 100 nanoseconds

                // Check if the timestamp is within the valid InfluxDB range
                if (timestampInNanoseconds >= -9223372036854775806 && timestampInNanoseconds <= 9223372036854775806)
                {
                    // Call the update method in DataManipulationHandler with the valid nanosecond timestamp
                    await _dataManipulationHandler.UpdateRowAsync("pacifiers", originalTags, timestampInNanoseconds, newFields, newTags);

                    // Close the window
                    DialogResult = true;
                    Close();
                }
                else
                {
                    ErrorMessage.Text = "Timestamp is out of the valid range for InfluxDB.";
                    ErrorMessage.Visibility = Visibility.Visible;
                }
            }
            else
            {
                ErrorMessage.Text = $"Timestamp data is missing or invalid. Timestamp Text: {timestampText ?? "Not found"}";
                ErrorMessage.Visibility = Visibility.Visible;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
