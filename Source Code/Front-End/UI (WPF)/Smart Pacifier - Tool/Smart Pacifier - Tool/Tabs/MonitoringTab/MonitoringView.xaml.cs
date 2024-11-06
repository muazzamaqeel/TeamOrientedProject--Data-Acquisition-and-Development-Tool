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
            DataContext = _viewModel;

            // Add Pacifier and Sensor Items Dynamically
            foreach (var pacifierItem in selectedPacifiers)
            {
                {
                    pacifierItem.IsChecked = false;
                }
            }
            AddPacifierItems(selectedPacifiers);

            pacifierFilterPanel.ItemsSource = _viewModel.PacifierItems;
            sensorFilterPanel.ItemsSource = _viewModel.SensorItems;

        }

        // ================ TBA ===============




        // ================ Pacifier Items ===============

        /// <summary>
        /// Method that adds the selected pacifier items to the interface.
        /// </summary>
        /// <param name="selectedPacifiers"></param>
        private void AddPacifierItems(List<PacifierItem> selectedPacifiers)
        {
            foreach (var pacifierItem in selectedPacifiers)
            {
                {
                    Debug.WriteLine($"Pacifier ID: {pacifierItem.PacifierId}");

                    pacifierItem.ButtonText = $"Pacifier {pacifierItem.PacifierId}";
                    pacifierItem.CircleText = " ";

                    pacifierItem.ToggleChanged += (s, e) => UpdateCircleText(pacifierItem);
                    _viewModel.PacifierItems.Add(pacifierItem);

                    FetchSensorsList(pacifierItem.PacifierId);
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

            UpdateSensorVisibility();
        }

        // ================ Sensor Items ===============

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pacifierId"></param>
        private void FetchSensorsList(string pacifierId)
        {
            /*
            var sensorDataList = ExposeSensorDataManager.Instance.GetAllSensorData();

            // Clear previous data
            _viewModel.SensorItems.Clear();

            // HashSet to get all unique sensor types
            var uniqueSensorTypes = new HashSet<string>();

            // Loop through the sensor data and collect unique sensor types
            foreach (var sensorData in sensorDataList)
            {
                foreach (var sensorType in sensorData.SensorDataMap.Keys)
                {
                    Debug.WriteLine($"Pacifier: {pacifierId}, Sensor: {sensorType}");
                    uniqueSensorTypes.Add(sensorType);
                }
            }

            // Add unique sensor types to SensorItems
            AddSensorItems(uniqueSensorTypes);
        
            */
            
            }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sensorTypes"></param>
        private void AddSensorItems(HashSet<string> sensorTypes)
        {
            foreach (var sensorType in sensorTypes)
            {
                // Check if an item with the same ButtonText already exists in SensorItems
                if (!_viewModel.SensorItems.Any(sensorItem => sensorItem.ButtonText == $"{sensorType}"))
                {
                    var sensorItem = new PacifierItem(PacifierItem.ItemType.Sensor)
                    {
                        ButtonText = $"{sensorType}",
                        CircleText = " ",
                        PacifierId = sensorType
                    };
                    sensorItem.ToggleChanged += (s, e) => UpdateSensorCircleText(sensorItem);
                    _viewModel.SensorItems.Add(sensorItem);
                    sensorItem.Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void UpdateSensorVisibility()
        {
            // Check if there are any checked pacifiers
            bool hasToggledPacifier = _viewModel.checkedPacifiers.Count > 0;

            // Toggle visibility of each sensor item
            foreach (var sensorItem in _viewModel.SensorItems)
            {
                sensorItem.Visibility = hasToggledPacifier ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        private void UpdateSensorCircleText(PacifierItem item)
        {
            if (item.IsChecked)
            {
                if (!_viewModel.checkedSensors.Contains(item))
                {
                    _viewModel.checkedSensors.Add(item);
                    //CreateGraphForSensor(sensorItem);
                }
            }
            else
            {
                _viewModel.checkedSensors.Remove(item);
                item.CircleText = " ";
                //RemoveGraphForSensor(sensorItem);
            }

            // Update the circle text for all checked sensors based on their order
            for (int i = 0; i < _viewModel.checkedSensors.Count; i++)
            {
                _viewModel.checkedSensors[i].CircleText = (i + 1).ToString();
            }

            // Update all active pacifier grids to reflect the global sensor state
            foreach (var pacifierItem in _viewModel.checkedPacifiers)
            {
                var pacifierGrid = FindPacifierGridForSensor(pacifierItem);
                if (pacifierGrid != null)
                {
                    if (item.IsChecked)
                    {
                        AddSensorRow(pacifierGrid, item);
                    }
                    else
                    {
                        RemoveSensorRow(pacifierGrid, item);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pacifierGrid"></param>
        /// <param name="sensorItem"></param>
        private void AddSensorRow(Grid pacifierGrid, PacifierItem sensorItem)
        {
            // Create a new ScrollViewer for the sensor row
            ScrollViewer scrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                VerticalAlignment = System.Windows.VerticalAlignment.Stretch
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
            for (int i = 1; i <= 3; i++)
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

            //FetchSensorData(pacifierId);

        }

        /// <summary>
        /// Helper method to validate the name for WPF
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private bool IsNameValid(string name)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(name, @"^[A-Za-z_][A-Za-z0-9_]*$");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pacifierGrid"></param>
        /// <param name="sensorItem"></param>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pacifierGrid"></param>
        /// <param name="sensorName"></param>
        /// <returns></returns>
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

        }
    }
}

