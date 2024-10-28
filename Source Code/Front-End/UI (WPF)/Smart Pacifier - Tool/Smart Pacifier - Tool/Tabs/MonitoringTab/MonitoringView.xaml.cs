using OxyPlot.Series;
using OxyPlot.Wpf;
using OxyPlot;
using Smart_Pacifier___Tool.Components;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Smart_Pacifier___Tool.Tabs.MonitoringTab
{
    public partial class MonitoringView : UserControl
    {
        public ObservableCollection<PacifierItem> PacifierItems { get; set; }
        public ObservableCollection<PacifierItem> SensorItems { get; set; }

        private int pacifierCounter = 1;
        private int sensorCounter = 1;

        private List<PacifierItem> checkedPacifiers = new List<PacifierItem>();
        private List<PacifierItem> checkedSensors = new List<PacifierItem>();

        public MonitoringView()
        {
            InitializeComponent();
             
            PacifierItems = new ObservableCollection<PacifierItem>();
            SensorItems = new ObservableCollection<PacifierItem>();

            // add dynamically
            AddPacifierItems(15); // temp
            AddSensorItems(15); // temp

            pacifierFilterPanel.ItemsSource = PacifierItems;
            sensorFilterPanel.ItemsSource = SensorItems;
        }

        private void AddPacifierItems(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var pacifierItem = new PacifierItem
                {
                    ButtonText = $"Pacifier {pacifierCounter}",
                    CircleText = " "
                };

                pacifierItem.ToggleChanged += (s, e) => UpdateCircleText(pacifierItem);
                pacifierCounter++;
                PacifierItems.Add(pacifierItem);
            }
        }

        private void AddSensorItems(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var sensorItem = new PacifierItem
                {
                    ButtonText = $"Sensor {sensorCounter}",
                    CircleText = " "
                };

                sensorItem.ToggleChanged += (s, e) => UpdateSensorCircleText(sensorItem);
                sensorCounter++;
                SensorItems.Add(sensorItem);
                sensorItem.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateSensorVisibility()
        {
            // Check if there are any checked pacifiers
            bool hasToggledPacifier = checkedPacifiers.Count > 0;

            // Toggle visibility of each sensor item
            foreach (var sensorItem in SensorItems)
            {
                sensorItem.Visibility = hasToggledPacifier ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void UpdateSensorCircleText(PacifierItem item)
        {
            if (item.IsChecked)
            {
                if (!checkedSensors.Contains(item))
                {
                    checkedSensors.Add(item);
                }
            }
            else
            {
                checkedSensors.Remove(item);
                item.CircleText = " ";
            }

            // Update the circle text for all checked sensors based on their order
            for (int i = 0; i < checkedSensors.Count; i++)
            {
                checkedSensors[i].CircleText = (i + 1).ToString();
            }

            // Update all active pacifier grids to reflect the global sensor state
            foreach (var pacifierItem in checkedPacifiers)
            {
                var pacifierGrid = FindPacifierGridForSensor(pacifierItem);
                if (pacifierGrid != null)
                {
                    if (item.IsChecked)
                    {
                        AddSensorRow(pacifierGrid, item, GetGraphCountForSensor(item));
                    }
                    else
                    {
                        RemoveSensorRow(pacifierGrid, item);
                    }
                }
            }
        }


        private int GetGraphCountForSensor(PacifierItem sensorItem)
        {
            // Return the number of graphs for each sensor based on its type
            return sensorItem.ButtonText == "Sensor 1" ? 9 : sensorItem.ButtonText == "Sensor 2" ? 3 : 5;
        }

        private void UpdateCircleText(PacifierItem item)
        {
            if (item.IsChecked)
            {
                if (!checkedPacifiers.Contains(item))
                {
                    checkedPacifiers.Add(item);
                    AddPacifierGrid(item);

                    // Ensure all checked sensors are added to this new pacifier grid
                    var pacifierGrid = FindPacifierGridForSensor(item);
                    foreach (var sensor in checkedSensors)
                    {
                        if (pacifierGrid != null)
                        {
                            AddSensorRow(pacifierGrid, sensor, GetGraphCountForSensor(sensor));
                        }
                    }
                }
            }
            else
            {
                checkedPacifiers.Remove(item);
                RemovePacifierGrid(item);
                item.CircleText = " ";
            }

            // Update the CircleText for all checked pacifiers based on their order
            for (int i = 0; i < checkedPacifiers.Count; i++)
            {
                checkedPacifiers[i].CircleText = (i + 1).ToString();
            }

            UpdateSensorVisibility();
        }


        private void OpenRawDataView_Click(object sender, RoutedEventArgs e)
        {
            // Cast the sender back to a Button to access the Tag or Content
            var button = sender as Button;
            if (button != null && button.Tag is string pacifierName)
            {
                // Create an instance of RawDataView with the properties and a reference to this view
                var rawDataView = new RawDataView(pacifierName, this, true);

                // Replace the current view with RawDataView
                var parent = this.Parent as ContentControl;
                if (parent != null)
                {
                    parent.Content = rawDataView;
                }
            }
        }


        private void AddPacifierGrid(PacifierItem pacifierItem)
        {
            Grid pacifierGrid = new Grid
            {
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch // Ensure the grid stretches
            };

            pacifierGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) }); // First row for UI controls
            pacifierGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Second row for graphs or other content

            // First row with TextBox, Buttons, and other elements
            AddPacifierRow(pacifierGrid, pacifierItem);

            // Add the grid directly to the StackPanel
            pacifierSectionsPanel.Children.Add(pacifierGrid);
        }


        private void AddPacifierRow(Grid pacifierGrid, PacifierItem pacifierItem)
        {
            pacifierGrid.Background = Brushes.DarkGray;
            pacifierGrid.Margin = new Thickness(5);
            // Set up the first row with 4 columns
            pacifierGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pacifierGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pacifierGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Create the first TextBlock for pacifier name
            TextBlock pacifierNameTextBox = new TextBlock
            {
                Text = pacifierItem.ButtonText,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                FontSize = 16,
                Foreground = Brushes.White,
            };
            Grid.SetRow(pacifierNameTextBox, 0);
            Grid.SetColumn(pacifierNameTextBox, 0);
            pacifierGrid.Children.Add(pacifierNameTextBox);

            // Create the second TextBlock for additional input
            TextBlock additionalTextBox = new TextBlock
            {
                Text = "Status",
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                FontSize = 16,
                Foreground = Brushes.White,
            };
            Grid.SetRow(additionalTextBox, 0);
            Grid.SetColumn(additionalTextBox, 1);
            pacifierGrid.Children.Add(additionalTextBox);

            // Create the "Debug" button
            Button debugButton = new Button
            {
                Content = "Debug",
                Width = 50,
                Height = 25,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Margin = new Thickness(5),
                Tag = pacifierItem.ButtonText // Use Tag to hold the pacifier name
            };
            Grid.SetRow(debugButton, 0);
            Grid.SetColumn(debugButton, 2);
            debugButton.Click += OpenRawDataView_Click; // Directly attach the event handler
            pacifierGrid.Children.Add(debugButton);

        }



        // Method to remove the grid corresponding to the pacifierItem
        private void RemovePacifierGrid(PacifierItem pacifierItem)
        {
            // Iterate through all children in the pacifierSectionsPanel
            foreach (var child in pacifierSectionsPanel.Children)
            {
                // Check if the child is a Grid
                if (child is Grid pacifierGrid)
                {
                    // Assuming the first child of the Grid is the TextBlock with ButtonText as pacifier's label
                    if (pacifierGrid.Children[0] is TextBlock pacifierLabel && pacifierLabel.Text == pacifierItem.ButtonText)
                    {
                        // Remove the pacifierGrid from the panel
                        pacifierSectionsPanel.Children.Remove(pacifierGrid);
                        break; // Exit once the matching grid is found and removed
                    }
                }
            }
        }

        // Method to create an OxyPlot model for sensor data
        private PlotModel CreatePlotModelForSensor(PacifierItem sensorItem)
        {
            var plotModel = new PlotModel { Title = $"Sensor Data for {sensorItem.ButtonText}" };
            // Add data to the plot model here
            return plotModel;
        }

        private Grid FindPacifierGridForSensor(PacifierItem sensorItem)
        {
            // Iterate through all children of pacifierSectionsPanel
            foreach (var child in pacifierSectionsPanel.Children)
            {
                // Check if the child is a Grid
                if (child is Grid pacifierGrid)
                {
                    // Check the first child of the grid to match the pacifier item (assuming the first child is a TextBlock with ButtonText)
                    TextBlock label = pacifierGrid.Children[0] as TextBlock;
                    if (label != null && label.Text == sensorItem.ButtonText)
                    {
                        // Found the corresponding grid
                        return pacifierGrid;
                    }
                }
            }

            // Return null if no matching grid was found
            return null;
        }

        private void AddSensorRow(Grid pacifierGrid, PacifierItem sensorItem, int graphCount)
        {
            // Create a new ScrollViewer for the sensor row
            ScrollViewer scrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch, // Use System
                VerticalAlignment = System.Windows.VerticalAlignment.Stretch // Use System
            };

            // Create a WrapPanel to hold the graphs
            WrapPanel graphPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                Width = Double.NaN // Allow it to stretch
            };

            scrollViewer.Content = graphPanel;

            // Add the ScrollViewer to the pacifier grid
            int rowIndex = pacifierGrid.RowDefinitions.Count;
            pacifierGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            Grid.SetRow(scrollViewer, rowIndex);
            Grid.SetColumnSpan(scrollViewer, 3); // Span across 3 columns
            pacifierGrid.Children.Add(scrollViewer);

            // Add graphs to the WrapPanel
            for (int i = 1; i <= graphCount; i++)
            {
                var graph = new LineChartGraph
                {
                    Width = 300,
                    Height = 200,
                    Name = $"Sensor_{sensorItem.ButtonText.Replace(" ", "_")}_Graph{i}",
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch, // Use System
                    Margin = new Thickness(5)
                };

                // Add the graph to the WrapPanel
                graphPanel.Children.Add(graph);
            }
        }

        // Helper method to validate the name for WPF
        private bool IsNameValid(string name)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(name, @"^[A-Za-z_][A-Za-z0-9_]*$");
        }

        private void RemoveSensorRow(Grid pacifierGrid, PacifierItem sensorItem)
        {
            if (pacifierGrid == null) return;

            // Find the scroll viewer that contains the graphs for the sensor item
            foreach (var child in pacifierGrid.Children)
            {
                if (child is ScrollViewer scrollViewer)
                {
                    // Check if the scrollViewer contains the graphs related to the sensor
                    bool containsSensorGraphs = false;

                    if (scrollViewer.Content is WrapPanel graphPanel)
                    {
                        foreach (var graph in graphPanel.Children)
                        {
                            // Assuming the graphs are named consistently with the sensor item
                            if (graph is LineChartGraph lineChartGraph &&
                                lineChartGraph.Name.Contains(sensorItem.ButtonText.Replace(" ", "_")))
                            {
                                containsSensorGraphs = true;
                                break;
                            }
                        }
                    }

                    // If the scrollViewer contains graphs for the sensor, remove it
                    if (containsSensorGraphs)
                    {
                        pacifierGrid.Children.Remove(scrollViewer);
                        break; // Exit after removing the correct scroll viewer
                    }
                }
            }
        }


        private bool DoesSensorRowExist(Grid pacifierGrid, string sensorName)
        {
            foreach (var child in pacifierGrid.Children)
            {
                if (child is ScrollViewer scrollViewer &&
                    scrollViewer.Content is StackPanel panel &&
                    panel.Children.Count > 0 &&
                    panel.Children[0] is Border graphPlaceholder &&
                    graphPlaceholder.Name.StartsWith(sensorName))
                {
                    return true;
                }
            }
            return false;
        }

        private void Intervals_Button(object sender, RoutedEventArgs e)
        {
            var dialog = new IntervalSettingsDialog(new List<PacifierItem>(checkedSensors));
            if (dialog.ShowDialog() == true)
            {
                var intervals = dialog.SensorIntervals;
                // Now you can use intervals for further processing
                foreach (var sensor in intervals)
                {
                    Console.WriteLine($"Sensor: {sensor.Key}, Intervals: {string.Join(", ", sensor.Value)}");
                }
            }
        }

        private void AddPacifier_Button(object sender, RoutedEventArgs e)
        {

        }

        private void EndCampaign_Button(object sender, RoutedEventArgs e)
        {

        }
    }
}

