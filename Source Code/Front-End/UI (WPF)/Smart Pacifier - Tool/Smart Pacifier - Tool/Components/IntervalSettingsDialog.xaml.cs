using Smart_Pacifier___Tool.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Smart_Pacifier___Tool.Components
{
    public partial class IntervalSettingsDialog : Window
    {
        public Dictionary<string, List<int>> SensorIntervals { get; private set; } = new Dictionary<string, List<int>>();
        public List<PacifierItem> PacifierItems { get; private set; }
        public List<SensorItem> SensorItems { get; private set; }

        public IntervalSettingsDialog(List<PacifierItem> pacifierItems, List<SensorItem> sensorItems)
        {
            PacifierItems = pacifierItems;  // Store the list of sensors
            SensorItems = sensorItems;
            InitializeComponent();
            PopulateSensorSettings(pacifierItems, sensorItems);
        }

        private void PopulateSensorSettings(List<PacifierItem> pacifierItems, List<SensorItem> sensorItems)
        {

                foreach (var sensorItem in sensorItems)
                {
                    // Create a WrapPanel for each sensor
                    var sensorPanel = new WrapPanel
                    {
                        Margin = new Thickness(5),
                        Background = (Brush)Application.Current.FindResource("MainViewSecondaryBackgroundColor")
                    };

                    // Create a Grid for each sensor
                    var sensorGrid = new Grid();
                    sensorGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 1 for SensorId
                    sensorGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 2 for SensorGroups and MeasurementGroups

                    // Row 1: SensorId
                    var sensorIdTextBlock = new TextBlock
                    {
                        Text = $"Sensor: {sensorItem.SensorId}",
                        HorizontalAlignment = HorizontalAlignment.Center,
                        FontSize = 20,
                        FontWeight = FontWeights.Bold,
                        Foreground = (Brush)Application.Current.FindResource("MainViewForegroundColor")

                    };
                    Grid.SetRow(sensorIdTextBlock, 0);
                    sensorGrid.Children.Add(sensorIdTextBlock);

                    // Row 2: MeasurementGroup and TextBox
                    var measurementGroupStackPanel = new StackPanel()
                    {

                    };

                    foreach (var measurementGroup in sensorItem.SensorGroups)
                    {
                        // Create a StackPanel for each KeyValuePair in the MeasurementGroup
                        var measurementStackPanel = new StackPanel { Orientation = Orientation.Horizontal };

                        // Create a TextBlock for the measurement label (value)
                        var valueTextBlock = new TextBlock
                        {
                            Text = measurementGroup,
                            Margin = new Thickness(5, 0, 5, 0),
                            VerticalAlignment = VerticalAlignment.Center,
                            Foreground = (Brush)Application.Current.FindResource("MainViewForegroundColor")
                        };

                        // Create a TextBox for the interval input
                        var textBox = new TextBox
                        {
                            Uid = measurementGroup,
                            Text = sensorItem.SensorGraphs.FirstOrDefault(g => g.Name == $"{sensorItem.SensorId}_{measurementGroup}")?.Interval.ToString() ?? "0", // Default to 0 if no graph exists
                            Width = 50
                        };

                        // Optionally, add input validation for numeric values
                        textBox.PreviewTextInput += (sender, e) =>
                        {
                            // Allow only numeric input (digits, minus, and decimal point)
                            e.Handled = !char.IsDigit(e.Text, 0) && e.Text != "-" && e.Text != ".";
                        };

                        // Add the TextBlock and TextBox to the StackPanel
                        measurementStackPanel.Children.Add(valueTextBlock);
                        measurementStackPanel.Children.Add(textBox);

                        // Add the StackPanel to the main measurement group panel
                        measurementGroupStackPanel.Children.Add(measurementStackPanel);
                    }


                    // Add the measurement group section to the grid
                    Grid.SetRow(measurementGroupStackPanel, 1);
                    sensorGrid.Children.Add(measurementGroupStackPanel);

                    // Add the grid to the sensor panel
                    sensorPanel.Children.Add(sensorGrid);

                    // Add the sensor panel to the main container
                    SensorSettingsPanel.Children.Add(sensorPanel);

                    // Initialize the dictionary with empty values
                    SensorIntervals[sensorItem.SensorId] = new List<int>(new int[sensorItem.SensorGraphs.Count]);
                }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var panel in SensorSettingsPanel.Children.OfType<WrapPanel>())
            {
                var sensorIdTextBlock = panel.Children.OfType<Grid>()
                    .FirstOrDefault()?.Children.OfType<TextBlock>().FirstOrDefault();

                if (sensorIdTextBlock != null)
                {
                    var sensorId = sensorIdTextBlock.Text.Replace("Sensor: ", string.Empty);
                    var sensorItem = FindSensorById(sensorId);

                    if (sensorItem != null)
                    {
                        var measurementGroupStackPanel = panel.Children.OfType<Grid>()
                            .FirstOrDefault()?.Children.OfType<StackPanel>().FirstOrDefault();

                        if (measurementGroupStackPanel != null)
                        {
                            // Iterate through all StackPanels inside the measurementGroupStackPanel
                            foreach (var measurementPanel in measurementGroupStackPanel.Children.OfType<StackPanel>())
                            {
                                // Find all TextBox controls within each measurementPanel (StackPanel)
                                foreach (var textBox in measurementPanel.Children.OfType<TextBox>())
                                {
                                    // Extract the content from the TextBox
                                    string intervalText = textBox.Text;

                                    if (int.TryParse(intervalText, out int interval))
                                    {
                                        Debug.WriteLine($"Interval: {textBox.Uid}: {interval}");

                                        // Find the matching SensorGraph by TextBox.Uid
                                        var matchingGraph = sensorItem.SensorGraphs
                                            .FirstOrDefault(g => g.Name == $"{sensorItem.SensorId}_{textBox.Uid}");

                                        if (matchingGraph != null)
                                        {
                                            // Update the interval for the current SensorItem's SensorGraph
                                            matchingGraph.Interval = interval;

                                            // Update all linked pacifiers for this sensor
                                            foreach (var pacifier in sensorItem.LinkedPacifiers)
                                            {
                                                Debug.WriteLine($"Pacifier {pacifier.PacifierId}");
                                                foreach (var sensor in pacifier.Sensors)
                                                {
                                                    Debug.WriteLine($"Sensor {sensor.SensorId}");
                                                    var linkedGraph = sensor.SensorGraphs.FirstOrDefault(g => g.Name == matchingGraph.Name);

                                                    if (linkedGraph != null)
                                                    {
                                                        linkedGraph.Interval = interval;

                                                        // Debug output to confirm propagation
                                                        Debug.WriteLine($"Updated Interval: {interval} for SensorGraph {linkedGraph.Name} in Pacifier {pacifier.PacifierId}");
                                                    }
                                                }
                                            }
                                            
                                        }
                                    }
                                    else
                                    {
                                        // Handle invalid input (optional)
                                        MessageBox.Show("Please enter a valid numeric value.");
                                    }
                                }
                            }
                        }
                    }
                }
            }

            this.DialogResult = true; // Indicate success
        }



        private SensorItem FindSensorById(string sensorId)
        {
            // This method should return the corresponding SensorItem based on the SensorId
            return SensorItems.FirstOrDefault(sensor => sensor.SensorId == sensorId);
        }

    }
}
