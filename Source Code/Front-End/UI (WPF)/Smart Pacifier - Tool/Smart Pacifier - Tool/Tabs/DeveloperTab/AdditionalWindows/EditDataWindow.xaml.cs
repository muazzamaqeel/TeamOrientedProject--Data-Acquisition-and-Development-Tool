using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
                    Foreground = (Brush)Application.Current.Resources["MainViewForegroundColor"],
                    Margin = new Thickness(0, 10, 0, 5)
                };
                DynamicFieldsPanel.Children.Add(label);

                // Create a TextBox for each field
                var textBox = new TextBox
                {
                    Text = kvp.Value?.ToString() ?? string.Empty,
                    Name = kvp.Key.Replace(" ", "_") + "TextBox",
                    Margin = new Thickness(0, 0, 0, 10),
                    Style = (Style)Application.Current.Resources["TextBoxStyle"],
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
            string debugInfo = "Debug Information:\n";

            foreach (var kvp in _textBoxes)
            {
                string key = kvp.Key;
                string value = kvp.Value.Text;

                // Ignore timestamp fields as they are managed by InfluxDB
                if (key.ToLower().Contains("timestamp"))
                {
                    continue;
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

            debugInfo += $"New Tags: {string.Join(", ", newTags)}\n";
            debugInfo += $"New Fields: {string.Join(", ", newFields)}\n";
            debugInfo += $"Original Tags: {string.Join(", ", new Dictionary<string, string>() { { "campaign_name", newTags.GetValueOrDefault("Campaign Name") }, { "pacifier_name", newTags.GetValueOrDefault("Pacifier Name") }, { "sensor_type", newTags.GetValueOrDefault("Sensor Type") } })}\n";

            // List all keys in _originalData for debugging
            debugInfo += $"Available keys in _originalData: {string.Join(", ", _originalData.Keys)}\n";

            // Attempt to retrieve the original timestamp with multiple key names
            if (_originalData.TryGetValue("timestamp", out var originalTimestampValue) ||
                _originalData.TryGetValue("Timestamp", out originalTimestampValue) ||
                _originalData.TryGetValue("time", out originalTimestampValue) ||
                _originalData.TryGetValue("_time", out originalTimestampValue))
            {
                // Try to parse the timestamp
                if (DateTime.TryParse(originalTimestampValue.ToString(), out DateTime originalTimestamp))
                {
                    // Convert DateTime to Unix timestamp in nanoseconds for deletion
                    long originalTimestampNanoseconds = originalTimestamp.ToUniversalTime().Ticks * 100; // 1 tick = 100 nanoseconds

                    debugInfo += $"Original Timestamp (ns): {originalTimestampNanoseconds}\n";
                    debugInfo += $"Original Timestamp (DateTime): {originalTimestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")}\n";

                    try
                    {
                        // Step 1: Delete the original entry from the database
                        await _dataManipulationHandler.DeleteRowAsync("pacifiers", new Dictionary<string, string>(), originalTimestampNanoseconds);

                        // Step 2: Create a new entry with the modified data
                        await _dataManipulationHandler.CreateNewEntryAsync("pacifiers", newFields, newTags);

                        MessageBox.Show("Data saved successfully.\n" + debugInfo, "Save Success", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Close the window after saving
                        DialogResult = true;
                        Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error during save operation:\n{ex.Message}\n\n{debugInfo}", "Save Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    debugInfo += "Failed to parse the original timestamp.\n";
                    MessageBox.Show(debugInfo, "Debug Information", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                debugInfo += "Failed to retrieve the original timestamp for deletion.\n";
                MessageBox.Show(debugInfo, "Debug Information", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
