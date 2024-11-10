using OxyPlot.Series;
using OxyPlot.Wpf;
using OxyPlot;
using Smart_Pacifier___Tool.Components;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SmartPacifier.Interface.Services;
using SmartPacifier.BackEnd.Database.InfluxDB.Managers;
using SmartPacifier.BackEnd.CommunicationLayer.Protobuf;
using System.Diagnostics;
using Smart_Pacifier___Tool.Tabs.MonitoringTab.MonitoringExtra;
using Smart_Pacifier___Tool.Resources;
using Google.Protobuf;
using System.Windows.Data;
using SmartPacifier.BackEnd.CommunicationLayer.MQTT;
using System.Collections.Specialized;
using static Smart_Pacifier___Tool.Components.PacifierItem;

namespace Smart_Pacifier___Tool.Tabs.MonitoringTab
{
    /// <summary>
    /// 
    /// </summary>
    public partial class MonitoringView : UserControl
    {
        private readonly MonitoringViewModel _viewModel;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="selectedPacifiers"></param>
        public MonitoringView(List<PacifierItem> selectedPacifiers)
        {
            InitializeComponent();

            // Initialize the ViewModel
            _viewModel = new MonitoringViewModel();

            // Bind the ViewModel to the DataContext
            this.DataContext = _viewModel;

            // Clear items
            _viewModel.SensorTypes.Clear();
            _viewModel.PacifierItems.Clear();

            // Initialize the UI
            AddPacifierItems(selectedPacifiers);

            pacifierFilterPanel.ItemsSource = _viewModel.PacifierItems;
            sensorFilterPanel.ItemsSource = _viewModel.SensorItems;

            foreach (var pacifier in _viewModel.PacifierItems) // Loop through each sensor
            {
                pacifier.Sensors.CollectionChanged += OnSensorTypesCollectionChanged;
            }

        }

        // ================ Real-Time ===============

        private void OnSensorTypesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Debug.WriteLine($"Monitoring: SensorTypes CollectionChanged Called");

            // Only call AddSensorItems if there are new items added to the collection
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                // Convert e.NewItems (which is an IList) to an ObservableCollection<Sensor>
                ObservableCollection<Sensor> newSensors = new ObservableCollection<Sensor>(e.NewItems.Cast<Sensor>());

                // Now you can pass the ObservableCollection<Sensor> to AddSensorItems
                AddSensorItems(newSensors);
            }
        }



        // ================ Pacifier Items - DONE ===============

        /// <summary>
        /// Method that adds the selected pacifier items to the interface.
        /// </summary>
        /// <param name="selectedPacifiers"></param>
        private void AddPacifierItems(List<PacifierItem> selectedPacifiers)
        {
            foreach (var pacifierItem in selectedPacifiers)
            {
                {
                    pacifierItem.IsChecked = false;
                    //Debug.WriteLine($"Monitoring: Pacifier ID: {pacifierItem.PacifierId}");

                    pacifierItem.ButtonText = $"Pacifier {pacifierItem.ItemId}";
                    pacifierItem.CircleText = " ";

                    pacifierItem.ToggleChanged += (s, e) => UpdateCircleText(pacifierItem);
                    _viewModel.PacifierItems.Add(pacifierItem);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        private void UpdateCircleText(PacifierItem pacifierItem)
        {

            if (pacifierItem.IsChecked)
            {
                if (!_viewModel.checkedPacifiers.Contains(pacifierItem))
                {
                    _viewModel.checkedPacifiers.Add(pacifierItem);
                    AddPacifierGrid(pacifierItem);

                    // Ensure all checked sensors are added to this new pacifier grid
                    var pacifierGrid = FindPacifierGridForSensor(pacifierItem);
                    foreach (var sensor in _viewModel.checkedSensors)
                    {
                        if (pacifierGrid != null)
                        {
                            AddSensorRow(pacifierGrid, sensor);
                        }
                    }

                    if (_viewModel.SensorTypes != null)
                    {
                        AddSensorItems(pacifierItem.Sensors);
                        //Debug.WriteLine("Monitoring: SensorTypes is used");
                    }
                    else
                    {
                        //Debug.WriteLine("Monitoring: SensorTypes is empty");
                    }
                }
            }
            else
            {
                _viewModel.checkedPacifiers.Remove(pacifierItem);
                RemovePacifierGrid(pacifierItem);
                pacifierItem.CircleText = " ";
            }

            // Update the CircleText for all checked pacifiers based on their order
            for (int i = 0; i < _viewModel.checkedPacifiers.Count; i++)
            {
                _viewModel.checkedPacifiers[i].CircleText = (i + 1).ToString();
            }

            _viewModel.TogglePacifierVisibility(pacifierItem);

        }

        // ================ Sensor Items - DONE ===============

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sensorTypes"></param>
        private void AddSensorItems(ObservableCollection<Sensor> sensors)
        {
            foreach (var sensor in sensors)
            {
                // Check if an item with the same ButtonText already exists in SensorItems
                if (!_viewModel.SensorItems.Any(sensorItem => sensorItem.ButtonText == $"{sensor.SensorId}"))
                {
                    var sensorItem = new PacifierItem(PacifierItem.ItemType.Sensor)
                    {
                        ButtonText = $"{sensor.SensorId}",
                        CircleText = " ",
                        ItemId = sensor.SensorId
                    };

                    // Bind the toggle change event
                    sensorItem.ToggleChanged += (s, e) => UpdateSensorCircleText(sensorItem);

                    _viewModel.SensorItems.Add(sensorItem);  // Notify UI of change

                    // Debug statement to track when a sensor item is added
                    //Debug.WriteLine($"Added Sensor Item: {sensorItem.ButtonText}");

                    //sensorItem.Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// Updates the circle text for a given sensor item based on whether it is checked or not.
        /// This method ensures the UI displays the correct order of checked sensors and handles their visibility in the UI.
        /// </summary>
        /// <param name="item"></param>
        private void UpdateSensorCircleText(PacifierItem sensorItem)
        {
            if (sensorItem.IsChecked)
            {
                if (!_viewModel.checkedSensors.Contains(sensorItem))
                {
                    _viewModel.checkedSensors.Add(sensorItem);
                }
            }
            else
            {
                _viewModel.checkedSensors.Remove(sensorItem);
                sensorItem.CircleText = " ";
            }

            // Update the circle text for all checked sensors based on their order
            for (int i = 0; i < _viewModel.checkedSensors.Count; i++)
            {
                _viewModel.checkedSensors[i].CircleText = (i + 1).ToString();
            }

            //Update all active pacifier grids to reflect the global sensor state
            foreach (var pacifierItem in _viewModel.checkedPacifiers)
            {
                var pacifierGrid = FindPacifierGridForSensor(sensorItem);
                if (pacifierGrid != null)
                {
                    if (sensorItem.IsChecked)
                    {
                        AddSensorRow(pacifierGrid, sensorItem);
                    }
                    else
                    {
                        RemoveSensorRow(pacifierGrid, sensorItem);
                    }
                }
            }
        }

        // ================ Grid Sections ===============

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pacifierItem"></param>
        private void AddPacifierGrid(PacifierItem pacifierItem)
        {
            Grid pacifierGrid = new()
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pacifierGrid"></param>
        /// <param name="pacifierItem"></param>
        private void AddPacifierRow(Grid pacifierGrid, PacifierItem pacifierItem)
        {
            pacifierGrid.Background = (Brush)Application.Current.FindResource("MainViewBackgroundColor");
            pacifierGrid.Margin = new Thickness(5);
            // Set up the first row with 4 columns
            pacifierGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pacifierGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pacifierGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Create the first TextBlock for pacifier name
            TextBlock pacifierNameTextBox = new()
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
            TextBlock additionalTextBox = new()
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
            Button debugButton = new()
            {
                Content = "Debug",
                Width = 50,
                Height = 25,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Margin = new Thickness(5),
                Style = (Style)Application.Current.FindResource("ModernButtonStyle"),
                Tag = pacifierItem.ButtonText // Use Tag to hold the pacifier name
            };
            Grid.SetRow(debugButton, 0);
            Grid.SetColumn(debugButton, 2);
            debugButton.Click += OpenRawDataView_Click; // Directly attach the event handler
            pacifierGrid.Children.Add(debugButton);

        }

        /// <summary>
        /// Method to remove the grid corresponding to the pacifierItem
        /// </summary>
        /// <param name="pacifierItem"></param>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sensorItem"></param>
        /// <returns></returns>
        private Grid? FindPacifierGridForSensor(PacifierItem sensorItem)
        {
            // Iterate through all children of pacifierSectionsPanel
            foreach (var child in pacifierSectionsPanel.Children)
            {
                // Check if the child is a Grid
                if (child is Grid pacifierGrid)
                {
                    // Check the first child of the grid to match the pacifier item (assuming the first child is a TextBlock with ButtonText)
                    TextBlock? label = pacifierGrid.Children[0] as TextBlock;
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

        // ================ Sensor Rows ===============

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pacifierGrid"></param>
        /// <param name="sensorItem"></param>
        private void AddSensorRow(Grid pacifierGrid, PacifierItem sensorItem)
        {
            ScrollViewer scrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                VerticalAlignment = System.Windows.VerticalAlignment.Stretch
            };

            WrapPanel graphPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                Width = Double.NaN
            };

            scrollViewer.Content = graphPanel;

            int rowIndex = pacifierGrid.RowDefinitions.Count;
            pacifierGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            Grid.SetRow(scrollViewer, rowIndex);
            Grid.SetColumnSpan(scrollViewer, 3);
            pacifierGrid.Children.Add(scrollViewer);



            // Iterate to create multiple graphs
            foreach (var sensor in sensorItem.Sensors)
            {
                foreach (var sensorGroup in sensor.SensorGroups)
                {
                    CreateGraphs(graphPanel, sensor, sensorGroup);
                }
                //// Get the sensorMeasurement keys that start with the groupMeasurement prefix
                //var matchingKeys = _viewModel._sensorDataDictionary.Keys
                //    .Where(key => key.StartsWith(groupMeasurement))
                //    .ToList();

                //// Now, for each matching key, create a graph
                //foreach (var matchingKey in matchingKeys)
                //{
                //    var sensorMeasurement = matchingKey.Split('_')[0];  // Prefix is the sensor measurement type
                //    CreateGraphs(graphPanel, sensorItem.ItemId, groupMeasurement, sensorMeasurement);
                //}
            }
        }

        private void CreateGraphs(WrapPanel graphPanel, Sensor sensorType, SensorGroup sensorGroup)
        {
            //Debug.WriteLine($"============== GRAPH ==============");
            //Debug.WriteLine($"Monitoring: Sensor Type: {ItemId}");
            //Debug.WriteLine($"Monitoring: Sensor Group: {GroupMeasurement}");
            //Debug.WriteLine($"Monitoring: Sensor Measurement: {sensorMeasurement}");

            var graph = new LineChartGraph (sensorType, sensorGroup, sensorGroup.GroupName, 15)
            {
                Width = 300,
                Height = 200,
                Name = sensorGroup.GroupName,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                Margin = new Thickness(5)
            };

            // Add the graph to the UI
            graphPanel.Children.Add(graph);
        }




        /// <summary>
        /// 
        /// </summary>
        /// <param name="pacifierGrid"></param>
        /// <param name="sensorItem"></param>
        private static void RemoveSensorRow(Grid pacifierGrid, PacifierItem sensorItem)
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

        // ================ Extra ===============

        /// <summary>
        /// Helper method to validate the name for WPF
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        //private bool IsNameValid(string name)
        //{
        //    return System.Text.RegularExpressions.Regex.IsMatch(name, @"^[A-Za-z_][A-Za-z0-9_]*$");
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pacifierGrid"></param>
        /// <param name="sensorName"></param>
        /// <returns></returns>
        //private bool DoesSensorRowExist(Grid pacifierGrid, string sensorName)
        //{
        //    foreach (var child in pacifierGrid.Children)
        //    {
        //        if (child is ScrollViewer scrollViewer &&
        //            scrollViewer.Content is StackPanel panel &&
        //            panel.Children.Count > 0 &&
        //            panel.Children[0] is Border graphPlaceholder &&
        //            graphPlaceholder.Name.StartsWith(sensorName))
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}

        // ================ Graphs ===============



        // ================ Buttons ===============

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Intervals_Button(object sender, RoutedEventArgs e)
        {
            var dialog = new IntervalSettingsDialog(new List<PacifierItem>(_viewModel.checkedSensors));
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddPacifier_Button(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EndCampaign_Button(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SensorTypes != null && _viewModel.SensorTypes.Any())
            {
                // Combine all sensor types into a single string with line breaks
                string sensorTypesText = string.Join("\n", _viewModel.GroupedSensorMeasurements);

                // Display the message box
                MessageBox.Show(sensorTypesText, "Sensor Types");
            }
            else
            {
                MessageBox.Show("No sensor types available.", "Sensor Types");
            }
        }
    }
}

