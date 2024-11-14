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
    public partial class MonitoringView : UserControl, INotifyCollectionChanged
    {
        private readonly MonitoringViewModel _viewModel;

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="selectedPacifiers"></param>
        public MonitoringView(ObservableCollection<PacifierItem> selectedPacifiers)
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

        // ================ Extra ===============



        // ================ Real-Time ===============

        private void OnSensorTypesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Debug.WriteLine($"Monitoring: SensorTypes CollectionChanged Called");

            // Only call AddSensorItems if there are new items added to the collection
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                // Convert e.NewItems (which is an IList) to an ObservableCollection<Sensor>
                ObservableCollection<SensorItem> newSensors = new ObservableCollection<SensorItem>(e.NewItems.Cast<SensorItem>());

                // Now you can pass the ObservableCollection<Sensor> to AddSensorItems
                AddSensorItems(newSensors);
            }
        }



        // ================ Pacifier Items - DONE ===============

        /// <summary>
        /// Method that adds the selected pacifier items to the interface.
        /// </summary>
        /// <param name="selectedPacifiers"></param>
        private void AddPacifierItems(ObservableCollection<PacifierItem> selectedPacifiers)
        {
            foreach (var pacifierItem in selectedPacifiers)
            {
                    pacifierItem.IsChecked = false;
                    //Debug.WriteLine($"Monitoring: Pacifier ID: {pacifierItem.PacifierId}");

                    pacifierItem.ButtonText = $"Pacifier {pacifierItem.PacifierId}";
                    pacifierItem.CircleText = " ";

                    pacifierItem.ToggleChanged += (s, e) => UpdateCircleText(pacifierItem);
                    _viewModel.PacifierItems.Add(pacifierItem);
            }
        }

        /// <summary>
        /// Updates the order text and creates Sensor Items when the Pacifier Item toggle is changed.
        /// Changes the visibility of the Sensor Filter Tab when there are no Checked Pacifiers.
        /// </summary>
        /// <param name="pacifierItem"></param>
        private void UpdateCircleText(PacifierItem pacifierItem)
        {
            OnPacifierItemToggled(pacifierItem, pacifierItem.IsChecked);

            // Update the CircleText for all checked pacifiers based on their order
            for (int i = 0; i < _viewModel.CheckedPacifierItems.Count; i++)
            {
                _viewModel.CheckedPacifierItems[i].CircleText = (i + 1).ToString();
            }

            // Call the method to update the visibility of the pacifier
            _viewModel.TogglePacifierVisibility(pacifierItem);
        }



        // ================ Sensor Items - DONE ===============

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sensorTypes"></param>
        private void AddSensorItems(ObservableCollection<SensorItem> sensorItems)
        {

            foreach (var sensorItem in sensorItems)
            {

                //Debug.WriteLine($"Monitoring: Pacifier ID: {pacifierItem.PacifierId}");
                if (!_viewModel.SensorItems.Any(p => p.SensorId == sensorItem.SensorId))
                {
                    
                    sensorItem.SensorButtonText = $"{sensorItem.SensorId}";
                    sensorItem.SensorCircleText = " ";
                    sensorItem.ToggleChanged += (s, e) => UpdateSensorCircleText(sensorItem);
                    _viewModel.SensorItems.Add(sensorItem);

                }
                
            }
        }

        /// <summary>
        /// Updates the circle text for a given sensor item based on whether it is checked or not.
        /// This method ensures the UI displays the correct order of checked sensors and handles their visibility in the UI.
        /// </summary>
        /// <param name="item"></param>
        private void UpdateSensorCircleText(SensorItem sensorItem)
        {
            OnSensorItemToggled(sensorItem, sensorItem.SensorIsChecked);

            // Now, update the SensorCircleText for all checked sensors based on their order
            for (int i = 0; i < _viewModel.CheckedSensorItems.Count; i++)
            {
                _viewModel.CheckedSensorItems[i].SensorCircleText = (i + 1).ToString();
            }
        }

        //// ================ Grid Sections ===============

        // Event Handler for PacifierItem Toggle
        private void OnPacifierItemToggled(PacifierItem pacifierItem, bool isChecked)
        {
            if (isChecked)
            {
                _viewModel.CheckedPacifierItems.Add(pacifierItem);
                AddPacifierGrid(pacifierItem);
                AddGraphRowsForToggledSensors(pacifierItem);
            }
            else
            {
                pacifierItem.CircleText = " ";
                _viewModel.CheckedPacifierItems.Remove(pacifierItem);
                RemovePacifierGrid(pacifierItem);
            }
        }

        // Event Handler for SensorItem Toggle
        private void OnSensorItemToggled(SensorItem sensorItem, bool isChecked)
        {
            if (isChecked)
            {
                _viewModel.CheckedSensorItems.Add(sensorItem);
                AddGraphRowsForToggledSensorsForAllPacifiers(sensorItem);
            }
            else
            {
                sensorItem.SensorCircleText = " ";
                _viewModel.CheckedSensorItems.Remove(sensorItem);
                RemoveGraphRowsForSensorItem(sensorItem);
            }
        }

        // Add Grid for PacifierItem
        private void AddPacifierGrid(PacifierItem pacifierItem)
        {
            if (!_viewModel.PacifierGridMap.ContainsKey(pacifierItem))
            {
                // Create a new grid for this pacifier item
                Grid grid = new Grid();
                pacifierSectionsPanel.Children.Add(grid);

                // Save the grid reference in the map
                _viewModel.PacifierGridMap[pacifierItem] = grid;

                // Add a row for the pacifier item (no graphs initially)
                AddPacifierRow(grid, pacifierItem);

                //Grid pacifierGrid = new()
                //{
                //    HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch, // Ensure the grid stretches
                //    Uid = $"Grid{pacifierItem.ItemId}",

                //};

                //Debug.WriteLine($"Adding grid with Uid: {pacifierGrid.Uid}");  // Verify Uid

                //pacifierGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) }); // First row for UI controls

                //// Add the grid directly to the StackPanel
                //pacifierSectionsPanel.Children.Add(pacifierGrid);

                //return pacifierGrid;
            }
        }

        // Add Row for PacifierItem (No Graphs)
        private void AddPacifierRow(Grid pacifierGrid, PacifierItem pacifierItem)
        {
            RowDefinition row = new RowDefinition();
            pacifierGrid.RowDefinitions.Add(row);

            pacifierGrid.Background = (Brush)Application.Current.FindResource("MainViewBackgroundColor");
            pacifierGrid.Margin = new Thickness(5);

            // Set up the first row with 3 columns
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
                Foreground = Brushes.White
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

        // Add Graph Rows for all Toggled SensorItems in this PacifierItem Grid
        private void AddGraphRowsForToggledSensors(PacifierItem pacifierItem)
        {
            Grid grid = _viewModel.PacifierGridMap[pacifierItem];

            foreach (var sensorItem in _viewModel.CheckedSensorItems)
            {
                AddSensorRow(grid, sensorItem);
            }
        }

        // Add Graph Row for a Specific SensorItem in a PacifierItem Grid
        private void AddSensorRow(Grid pacifierGrid, SensorItem sensorItem)
        {
            if (!_viewModel.SensorRowMap.ContainsKey(new Tuple<PacifierItem, SensorItem>(sensorItem.ParentPacifierItem.GetPacifierItem(), sensorItem)))
            {
                RowDefinition sensorRow = new RowDefinition();
                pacifierGrid.RowDefinitions.Add(sensorRow);

                // Create the Graph or ScrollViewer for this SensorItem here
                // You can create a row with a ScrollViewer to display the graphs

                // Example of adding a graph (adjust according to your graphing logic)
                ScrollViewer graphScrollViewer = new ScrollViewer
                {
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                    VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
                    Uid = sensorItem.SensorId
                };

                graphScrollViewer.Content = CreateGraphForSensor(sensorItem);

                pacifierGrid.Children.Add(graphScrollViewer);

                // Save the row reference in the map
                _viewModel.SensorRowMap[new Tuple<PacifierItem, SensorItem>(sensorItem.ParentPacifierItem.GetPacifierItem(), sensorItem)] = sensorRow;

                //    // Find the pacifier grid for the given sensor based on PacifierId
                //    var pacifierGrid = FindPacifierGridForSensor(pacifierItem);

                //    if (pacifierGrid == null && sensorItem.HasGraphs)
                //    {
                //        Debug.WriteLine($"Monitoring: Grid is NULL");
                //        return;
                //    }

                //    sensorItem.HasGraphs = true;

                //    ScrollViewer scrollViewer = new ScrollViewer
                //    {
                //        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                //        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                //        HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                //        VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
                //        Uid = sensorItem.SensorId
                //    };

                //    WrapPanel graphPanel = new WrapPanel
                //    {
                //        Orientation = Orientation.Horizontal,
                //    };

                //    scrollViewer.Content = graphPanel;

                //    // Safely get the row index (check if RowDefinitions exists)
                //    int rowIndex = pacifierGrid.RowDefinitions.Count;

                //    pacifierGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                //    Grid.SetRow(scrollViewer, rowIndex);
                //    Grid.SetColumnSpan(scrollViewer, 3);
                //    pacifierGrid.Children.Add(scrollViewer);

                //    Debug.WriteLine($"Monitoring: Added Row for {sensorItem.SensorId} Pacifier {pacifierItem.ItemId}");

                //    // Iterate to create multiple graphs
                //    foreach (var sensorGroup in sensorItem.SensorGroups)
                //    {
                //        CreateGraphs(graphPanel, sensorGroup);
                //    }

            }
        }

        // Add Graph Rows for all Toggled SensorItems for all Pacifiers
        private void AddGraphRowsForToggledSensorsForAllPacifiers(SensorItem sensorItem)
        {
            foreach (var pacifierItem in _viewModel.CheckedPacifierItems)
            {
                AddGraphRowsForToggledSensors(pacifierItem);
            }
        }

        // Remove Grid for PacifierItem
        private void RemovePacifierGrid(PacifierItem pacifierItem)
        {
            if (_viewModel.PacifierGridMap.ContainsKey(pacifierItem))
            {
                Grid grid = _viewModel.PacifierGridMap[pacifierItem];
                pacifierSectionsPanel.Children.Remove(grid);

                // Remove all rows associated with this pacifier
                foreach (var row in grid.RowDefinitions)
                {
                    // Logic to remove row content, if necessary
                }

                // Remove this entry from the map
                _viewModel.PacifierGridMap.Remove(pacifierItem);

                // Remove associated sensor rows
                RemoveGraphRowsForSensorItemsForPacifier(pacifierItem);
            }
        }

        // Remove Graph Rows for all SensorItems for a Specific PacifierItem
        private void RemoveGraphRowsForSensorItemsForPacifier(PacifierItem pacifierItem)
        {
            if (_viewModel.PacifierGridMap.ContainsKey(pacifierItem))
            {
                Grid grid = _viewModel.PacifierGridMap[pacifierItem];

                foreach (var sensorItem in _viewModel.CheckedSensorItems)
                {
                    RemoveSensorRow(grid, sensorItem);
                }
            }
        }

        // Remove Graph Row for a Specific SensorItem in a PacifierItem Grid
        private void RemoveSensorRow(Grid grid, SensorItem sensorItem)
        {
            if (_viewModel.SensorRowMap.ContainsKey(new Tuple<PacifierItem, SensorItem>(sensorItem.ParentPacifierItem.GetPacifierItem(), sensorItem)))
            {
                RowDefinition row = _viewModel.SensorRowMap[new Tuple<PacifierItem, SensorItem>(sensorItem.ParentPacifierItem.GetPacifierItem(), sensorItem)];
                grid.RowDefinitions.Remove(row);

                // Logic to remove the row content (graph)
                // Example: remove the ScrollViewer and its content
                var scrollViewer = grid.Children.OfType<ScrollViewer>().FirstOrDefault(s => s.Content == CreateGraphForSensor(sensorItem));
                if (scrollViewer != null)
                {
                    grid.Children.Remove(scrollViewer);
                }

                // Remove from map
                _viewModel.SensorRowMap.Remove(new Tuple<PacifierItem, SensorItem>(sensorItem.ParentPacifierItem.GetPacifierItem(), sensorItem));
            }
        }

        /// <summary>
        /// Removes the graph rows for the specified SensorItem from the PacifierItem grid.
        /// </summary>
        /// <param name="pacifierItem">The PacifierItem that contains the grid to remove rows from.</param>
        /// <param name="sensorItem">The SensorItem whose graph rows should be removed.</param>
        private void RemoveGraphRowsForSensorItem(SensorItem sensorItem)
        {
            var pacifierItem = sensorItem.ParentPacifierItem.GetPacifierItem();
            // Find the grid associated with the pacifier item
            var pacifierGrid = FindPacifierGridForSensor(pacifierItem);

            if (pacifierGrid == null)
            {
                Debug.WriteLine("Monitoring: Pacifier grid not found.");
                return;
            }

            // Iterate through each sensor group in the sensor item
            foreach (var sensorGroup in sensorItem.MeasurementGroup)
            {
                var firstKvp = sensorGroup.FirstOrDefault();

                if (firstKvp.Key != null && firstKvp.Key == "sensorGroup") // Ensure there's at least one key-value pair
                {
                    var groupName = firstKvp.Value;

                    // Find the ScrollViewer that contains the graph for this sensor group
                    var scrollViewerToRemove = pacifierGrid.Children
                    .OfType<ScrollViewer>()
                    .FirstOrDefault(scrollViewer => scrollViewer.Uid == $"{sensorItem.SensorId}_{groupName}");

                    if (scrollViewerToRemove != null)
                    {
                        // Get the row index of the ScrollViewer to remove
                        int rowIndex = Grid.GetRow(scrollViewerToRemove);

                        // Remove the ScrollViewer from the grid
                        pacifierGrid.Children.Remove(scrollViewerToRemove);

                        // Optionally, remove the corresponding row definition
                        if (rowIndex < pacifierGrid.RowDefinitions.Count)
                        {
                            pacifierGrid.RowDefinitions.RemoveAt(rowIndex);
                        }

                        Debug.WriteLine($"Monitoring: Removed graph row for {groupName} in Pacifier {pacifierItem.PacifierId}");
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sensorItem"></param>
        /// <returns></returns>
        private Grid? FindPacifierGridForSensor(PacifierItem pacifierItem)
        {

            var gridToFind = pacifierSectionsPanel.Children
                .OfType<Grid>()
                .FirstOrDefault(g => g.Uid == $"Grid{pacifierItem.PacifierId}");  // Adjust the Uid format if needed

            return gridToFind;
        }


        // Create multiple Graphs for a SensorItem based on its SensorGroups
        private UIElement CreateGraphForSensor(SensorItem sensorItem)
        {
            // Create a container for the graphs (could be a StackPanel, a Grid, etc.)
            StackPanel graphContainer = new StackPanel();

            // Iterate through each SensorGroup in the SensorItem
            foreach (var sensorGroup in sensorItem.MeasurementGroup)
            {
                var firstKvp = sensorGroup.FirstOrDefault();

                if (firstKvp.Key != null && firstKvp.Key == "sensorGroup") // Ensure there's at least one key-value pair
                {
                    var groupName = firstKvp.Value;

                    // Create a graph for this SensorGroup and pass the required parameters
                    LineChartGraph graph = new LineChartGraph(sensorItem, 15)
                    {
                        Width = 350,
                        Height = 200,
                        Name = groupName.ToString(),
                        PlotId = $"{sensorItem.SensorId}_{groupName.ToString()}",
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                        Margin = new Thickness(5)
                    };

                    graphContainer.Children.Add(graph);  // Add the graph to the container
                }

                // Optionally, add some customization to each graph based on sensorGroup data

            }

            // Return the container with all graphs for the SensorItem
            return graphContainer;
        }



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
            //var dialog = new IntervalSettingsDialog(new List<SensorItem>(_viewModel.checkedSensors));
            //if (dialog.ShowDialog() == true)
            //{
            //    var intervals = dialog.SensorIntervals;
            //    // Now you can use intervals for further processing
            //    foreach (var sensor in intervals)
            //    {
            //        Console.WriteLine($"Sensor: {sensor.Key}, Intervals: {string.Join(", ", sensor.Value)}");
            //    }
            //}
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

