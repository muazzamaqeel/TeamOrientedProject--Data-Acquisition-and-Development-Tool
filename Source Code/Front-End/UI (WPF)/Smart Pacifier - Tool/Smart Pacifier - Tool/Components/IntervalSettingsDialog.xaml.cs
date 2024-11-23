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
        public Dictionary<string, int> SensorIntervals { get; private set; } = new Dictionary<string, int>();
        public List<PacifierItem> PacifierItems { get; private set; }
        public List<SensorItem> SensorItems { get; private set; }

        public IntervalSettingsDialog(List<PacifierItem> pacifierItems, List<SensorItem> sensorItems, Dictionary<string, int> sensorIntervals)
        {
            PacifierItems = pacifierItems;  // Store the list of sensors
            SensorItems = sensorItems;
            SensorIntervals = sensorIntervals;
            InitializeComponent();
            PopulateSensorSettings();
        }

        private void PopulateSensorSettings()
        {

            foreach (var sensorItem in SensorItems)
            {
                // Create a WrapPanel for each sensor
                var sensorPanel = new WrapPanel
                {
                    Margin = new Thickness(5),
                    Background = (Brush)Application.Current.FindResource("MainViewBackgroundColor")
                };

                // Create a Grid for each sensor
                var sensorGrid = new Grid();
                sensorGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 1 for SensorId
                sensorGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 2 for SensorGroups and MeasurementGroups


                // Row 1: SensorId
                var sensorIdTextBlock = new TextBlock
                {
                    Text = $"Sensor: {sensorItem.SensorId}",
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left,  // Center horizontally
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,     // Center vertically
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(10),
                    Foreground = (Brush)Application.Current.FindResource("MainViewForegroundColor")
                };

                // Set the position of the sensorIdTextBlock in the grid (row 0, column 0)
                Grid.SetRow(sensorIdTextBlock, 0);
                sensorGrid.Children.Add(sensorIdTextBlock);


                // Row 2: MeasurementGroup and TextBox
                var measurementGroupStackPanel = new StackPanel()
                {

                };

                foreach (var measurementGroup in sensorItem.SensorGroups)
                {
                    // Create a StackPanel for each KeyValuePair in the MeasurementGroup
                    var measurementStackPanel = new StackPanel {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Stretch, 
                        VerticalAlignment = VerticalAlignment.Stretch
                    };

                    // Create a TextBlock for the measurement label (value)
                    var valueTextBlock = new TextBlock
                    {
                        Text = measurementGroup,
                        Margin = new Thickness(10),
                        FontSize = 18,
                        VerticalAlignment = VerticalAlignment.Center,
                        Foreground = (Brush)Application.Current.FindResource("MainViewForegroundColor")
                    };

                    // Create a TextBox for the interval input
                    var textBox = new TextBox
                    {
                        Style = (Style)Application.Current.FindResource("TextBoxStyle"),
                        Uid = measurementGroup,
                        Text = SensorIntervals[measurementGroup].ToString() ?? "0", // Default to 0 if no graph exists
                        Width = 100,
                        Margin = new Thickness(0),
                        FontSize = 10,
                        Height = 35
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

                }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var panel in SensorSettingsPanel.Children.OfType<WrapPanel>())
            {
                var sensorIdTextBlock = panel.Children.OfType<Grid>()
                    .FirstOrDefault()?.Children.OfType<TextBlock>().FirstOrDefault();

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

                                // Update all linked pacifiers for this sensor

                                //Debug.WriteLine($"Pacifier {pacifier.PacifierId}");
                                foreach (var pacifierItem in PacifierItems)
                                {
                                    foreach (var sensorItem in SensorItems)
                                    {
                                        // Find the matching SensorGraph by TextBox.Uid
                                        var matchingGraph = sensorItem.SensorGraphs
                                            .FirstOrDefault(g => g.Uid == $"{sensorItem.SensorId}_{textBox.Uid}_{pacifierItem.PacifierId}");

                                        if (matchingGraph != null)
                                        {

                                            Debug.WriteLine($"Update Interval for: Uid: {matchingGraph.Uid}, Name: {matchingGraph.Name}");
                                            // Update the interval for the current SensorItem's SensorGraph
                                            matchingGraph.Interval = interval;

                                            SensorIntervals[matchingGraph.Name] = interval;
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
            this.DialogResult = true; // Indicate success
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;  // Close the dialog and signal cancellation
        }


        private SensorItem FindSensorById(string sensorId)
        {
            // This method should return the corresponding SensorItem based on the SensorId
            return SensorItems.FirstOrDefault(sensor => sensor.SensorId == sensorId);
        }

    }
}
