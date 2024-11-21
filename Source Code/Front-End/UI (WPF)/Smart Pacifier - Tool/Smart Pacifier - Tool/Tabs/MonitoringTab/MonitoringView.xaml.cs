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
using DataPoint = OxyPlot.DataPoint;
using Microsoft.Extensions.DependencyInjection;
using OxyPlot.Axes;
using OxyPlot.Legends;
using Smart_Pacifier___Tool.Tabs.SettingsTab;
using SmartPacifier.BackEnd.DatabaseLayer.InfluxDB.LineProtocol;
using System.Windows.Documents;
using System.Windows.Shapes;

namespace Smart_Pacifier___Tool.Tabs.MonitoringTab
{
    /// <summary>
    /// 
    /// </summary>
    public partial class MonitoringView : UserControl, INotifyCollectionChanged
    {

        private readonly MonitoringViewModel _viewModel;
        public event NotifyCollectionChangedEventHandler? CollectionChanged;
        public const string DeveloperTabVisibleKey = "DeveloperTabVisible";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="selectedPacifiers"></param>
        public MonitoringView(ObservableCollection<PacifierItem> selectedPacifiers, ILineProtocol lineProtocol, string currentCampaignName)
        {
            InitializeComponent();

            // Initialize the ViewModel
            _viewModel = new MonitoringViewModel(lineProtocol, currentCampaignName);

            // Bind the ViewModel to the DataContext
            this.DataContext = _viewModel;

            // Clear items
            _viewModel.PacifierItems.Clear();

            headerTextBlock.Text = currentCampaignName;

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

                // Remove all entries in _lastUpdateTimestamps associated with the sensor
                var keysToUpdate = _viewModel._lastUpdateTimestamps.Keys
                    .Where(key => key.EndsWith(pacifierItem.PacifierId))
                    .ToList();

                foreach (var key in keysToUpdate)
                {
                    _viewModel._lastUpdateTimestamps[key] = DateTime.Now.AddMilliseconds(-10000);
                    Debug.WriteLine($"New timestamp entry: {key}");
                }
               
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


                // Remove all entries in _lastUpdateTimestamps associated with the sensor
                var keysToUpdate = _viewModel._lastUpdateTimestamps.Keys
                    .Where(key => key.StartsWith(sensorItem.SensorId))
                    .ToList();

                foreach (var key in keysToUpdate)
                {
                    _viewModel._lastUpdateTimestamps[key] = DateTime.Now.AddMilliseconds(-10000);
                    Debug.WriteLine($"New timestamp entry: {key}");
                }
                
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
            // Check if the grid already exists using the UID
            Grid? existingGrid = FindPacifierGrid(pacifierItem.PacifierId);

            if (existingGrid == null)
            {
                // Create a new grid for this pacifier item
                Grid newGrid = new Grid
                {
                    Uid = $"Grid_{pacifierItem.PacifierId}"
                };
                pacifierSectionsPanel.Children.Add(newGrid);

                AddPacifierRow(newGrid, pacifierItem);
                Debug.WriteLine($"Monitoring: Created Grid for Pacifier {pacifierItem.PacifierId}");
            }
        }


        // Add Row for PacifierItem (No Graphs)
        private void AddPacifierRow(Grid pacifierGrid, PacifierItem pacifierItem)
        {
            RowDefinition row = new RowDefinition();
            pacifierGrid.RowDefinitions.Add(row);

            // Create a Border for the row with rounded corners
            Border rowBorder = new Border
            {
                Background = (Brush)Application.Current.FindResource("AccentColor"), // Row background
                CornerRadius = new CornerRadius(8), // Rounded corners
                Margin = new Thickness(2), // Space around the row
                Height = 40,
            };

            // Create a Grid inside the Border for the row's content
            Grid rowGrid = new Grid
            {
                Background = Brushes.Transparent // Transparent to let the border's background show
            };

            // Set up the Grid with 3 columns
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Create a Border for the status panel with rounded corners
            Border statusBorder = new Border
            {
                CornerRadius = new CornerRadius(8), // Rounded corners
                Background = (Brush)Application.Current.Resources["MainViewSecondaryBackgroundColor"],
                Width = 150,
                Height = 30,
                Padding = new Thickness(2), // Space inside the border
                Margin = new Thickness(5) // Space around the status panel
            };

            // Create a StackPanel to go inside the Border
            StackPanel statusPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal, // Align items horizontally
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };

            // Create the status circle
            Ellipse statusCircle = new Ellipse
            {
                Width = 12, // Diameter of the circle
                Height = 12,
                Fill = Brushes.LawnGreen, // Initial color
                Margin = new Thickness(0, 0, 5, 0) // Add spacing between the circle and text
            };

            // Optional: Bind the Fill property to a property in the PacifierItem class
            Binding circleFillBinding = new Binding("StatusColor") // Assumes a `StatusColor` property exists
            {
                Source = pacifierItem,
                Mode = BindingMode.TwoWay // Allow updates from the UI or code
            };
            statusCircle.SetBinding(Ellipse.FillProperty, circleFillBinding);

            // Create the first TextBlock for the status
            TextBlock statusTextBox = new TextBlock
            {
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                FontSize = 12,
                FontWeight = System.Windows.FontWeights.Bold,
                Foreground = (Brush)Application.Current.Resources["MainViewForegroundColor"],
            };
            Binding statusBinding = new Binding("Status")
            {
                Source = pacifierItem
            };
            statusTextBox.SetBinding(TextBlock.TextProperty, statusBinding);

            // Add the circle and text to the StackPanel
            statusPanel.Children.Add(statusCircle);
            statusPanel.Children.Add(statusTextBox);

            // Add the StackPanel to the Border
            statusBorder.Child = statusPanel;

            // Add the Border to the Grid
            Grid.SetRow(statusBorder, 0);
            Grid.SetColumn(statusBorder, 0);
            rowGrid.Children.Add(statusBorder);


            // Create the TextBlock for pacifier name
            TextBlock pacifierNameTextBox = new()
            {
                Text = pacifierItem.ButtonText,
                Foreground = Brushes.White,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Margin = new Thickness(5),
                TextAlignment = TextAlignment.Center,
                FontSize = 16,
                Padding = new Thickness(8),
                FontWeight = System.Windows.FontWeights.Bold
            };
            Grid.SetRow(pacifierNameTextBox, 0);
            Grid.SetColumn(pacifierNameTextBox, 1);
            rowGrid.Children.Add(pacifierNameTextBox);

            // Create the "Debug" button
            Button debugButton = new()
            {
                Content = "Debug",
                Width = 50,
                Height = 30,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Margin = new Thickness(5),
                Style = (Style)Application.Current.FindResource("ModernButtonStyle"),
                Tag = pacifierItem.PacifierId
            };
            
            debugButton.Visibility = (Application.Current.Properties[DeveloperTabVisibleKey] is bool isVisible && isVisible)
                ? Visibility.Visible
                : Visibility.Collapsed;

            Grid.SetRow(debugButton, 0);
            Grid.SetColumn(debugButton, 2);
            debugButton.Click += OpenRawDataView_Click;
            rowGrid.Children.Add(debugButton);

            // Add the Grid to the Border
            rowBorder.Child = rowGrid;

            // Add the Border to the main Grid
            Grid.SetRow(rowBorder, pacifierGrid.RowDefinitions.Count - 1);
            pacifierGrid.Children.Add(rowBorder);
        }


        // Add Graph Rows for all Toggled SensorItems in this PacifierItem Grid
        private void AddGraphRowsForToggledSensors(PacifierItem pacifierItem)
        {
            // First, remove any existing sensor rows (this ensures we don't have duplicates)
            RemoveGraphRowsForSensorItemsForPacifier(pacifierItem);

            // Now, add the rows again for each checked sensor
            foreach (var sensorItem in _viewModel.CheckedSensorItems)
            {
                AddSensorRow(pacifierItem, sensorItem);
            }
        }

        // Add Graph Row for a Specific SensorItem in a PacifierItem Grid
        private void AddSensorRow(PacifierItem pacifierItem, SensorItem sensorItem)
        {
            Grid? pacifierGrid = FindPacifierGrid(pacifierItem.PacifierId);
            if (pacifierGrid == null) return;

            bool headerExists = false;
            foreach (UIElement child in pacifierGrid.Children)
            {
                if (child is TextBlock textBlock && textBlock.Text == $"Sensor: {sensorItem.SensorId}")
                {
                    headerExists = true;
                    break;
                }
            }

            if (!headerExists)
            {
                RowDefinition sensorRow = new RowDefinition();
                pacifierGrid.RowDefinitions.Add(sensorRow);
                pacifierGrid.Margin = new Thickness(15);

                // Create a Border for the sensor header with rounded corners
                Border sensorHeaderBorder = new Border
                {
                    CornerRadius = new CornerRadius(8), // Rounded corners
                    Background = (Brush)Application.Current.Resources["MainViewSecondaryBackgroundColor"],
                    Height = 30,
                    Margin = new Thickness(5), // Space around the border
                    Padding = new Thickness(2) // Space inside the border
                };

                // Create a TextBlock for the sensor header
                TextBlock sensorHeader = new TextBlock
                {
                    Text = $"Sensor: {sensorItem.SensorId}",
                    Foreground = (Brush)Application.Current.Resources["MainViewForegroundColor"],
                    TextAlignment = TextAlignment.Center, // Center the text
                    FontSize = 16, // Increase the text size
                    FontWeight = System.Windows.FontWeights.Bold,
                    Padding = new Thickness(2) // Add padding inside the TextBlock
                };

                // Add the TextBlock to the Border
                sensorHeaderBorder.Child = sensorHeader;

                // Add the Border to the Grid
                Grid.SetRow(sensorHeaderBorder, pacifierGrid.RowDefinitions.Count - 1);
                Grid.SetColumnSpan(sensorHeaderBorder, 3); // Span 3 columns
                pacifierGrid.Children.Add(sensorHeaderBorder);

                // Create a unique identifier for the graph
                string uniqueUid = $"{pacifierItem.PacifierId}_{sensorItem.SensorId}";

                // Ensure that the new row has the correct row index for the ScrollViewer
                WrapPanel graphScrollViewer = CreateGraphForSensor(pacifierItem, sensorItem);
                graphScrollViewer.Uid = uniqueUid;

                // Set the new row index for the ScrollViewer
                int newRowIndex = pacifierGrid.RowDefinitions.Count;
                Grid.SetRow(graphScrollViewer, newRowIndex);
                Grid.SetColumnSpan(graphScrollViewer, 3);

                pacifierGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                pacifierGrid.Children.Add(graphScrollViewer);
            }


            Debug.WriteLine($"Monitoring: Added SensorRow for Pacifier {pacifierItem.PacifierId}, Sensor {sensorItem.SensorId}");
        }



        private void AddGraphRowsForToggledSensorsForAllPacifiers(SensorItem sensorItem)
        {
            foreach (var pacifierItem in _viewModel.CheckedPacifierItems)
            {
                AddSensorRow(pacifierItem, sensorItem);
            }
        }

        // Remove Grid for PacifierItem
        private void RemovePacifierGrid(PacifierItem pacifierItem)
        {
            Grid? gridToRemove = FindPacifierGrid(pacifierItem.PacifierId);
            if (gridToRemove != null)
            {
                pacifierSectionsPanel.Children.Remove(gridToRemove);
                Debug.WriteLine($"Monitoring: Removed Grid for Pacifier {pacifierItem.PacifierId}");
            }
        }


        // Remove Graph Rows for all SensorItems for a Specific PacifierItem
        private void RemoveGraphRowsForSensorItemsForPacifier(PacifierItem pacifierItem)
        {
            Debug.WriteLine($"Monitoring: RemoveGraphRowsForSensorItemsForPacifier");

            foreach (var sensorItem in _viewModel.CheckedSensorItems)
            {
                RemoveSensorRow(pacifierItem, sensorItem);
            }
        }

        // Remove Graph Row for a Specific SensorItem in a PacifierItem Grid
        private void RemoveSensorRow(PacifierItem pacifierItem, SensorItem sensorItem)
        {
            Grid? pacifierGrid = FindPacifierGrid(pacifierItem.PacifierId);
            if (pacifierGrid == null) return;

            var sensorHeaderRemove = pacifierGrid.Children
                .OfType<TextBlock>().FirstOrDefault(wp => wp.Text == $"Sensor: {sensorItem.SensorId}");

            if (sensorHeaderRemove != null)
            {
                int rowIndex = Grid.GetRow(sensorHeaderRemove);
                pacifierGrid.Children.Remove(sensorHeaderRemove);

                // Optionally, remove the corresponding row definition
                if (rowIndex < pacifierGrid.RowDefinitions.Count)
                {
                    pacifierGrid.RowDefinitions.RemoveAt(rowIndex);
                }

                Debug.WriteLine($"Monitoring: Removed SensorRow for Pacifier {pacifierItem.PacifierId}, Sensor {sensorItem.SensorId}");
            }


            // Find the WrapPanel for this sensor using its Uid
            var wrapPanelToRemove = pacifierGrid.Children
                .OfType<WrapPanel>()
                .FirstOrDefault(wp => wp.Uid == $"{pacifierItem.PacifierId}_{sensorItem.SensorId}");

            if (wrapPanelToRemove != null)
            {
                int rowIndex = Grid.GetRow(wrapPanelToRemove);
                pacifierGrid.Children.Remove(wrapPanelToRemove);

                // Optionally, remove the corresponding row definition
                if (rowIndex < pacifierGrid.RowDefinitions.Count)
                {
                    pacifierGrid.RowDefinitions.RemoveAt(rowIndex);
                }

                Debug.WriteLine($"Monitoring: Removed SensorRow for Pacifier {pacifierItem.PacifierId}, Sensor {sensorItem.SensorId}");
            }
        }

        /// <summary>
        /// Removes the graph rows for the specified SensorItem from all PacifierItem grids.
        /// </summary>
        /// <param name="sensorItem">The SensorItem whose graph rows should be removed.</param>
        private void RemoveGraphRowsForSensorItem(SensorItem sensorItem)
        {
            foreach (var pacifierItem in _viewModel.CheckedPacifierItems)
            {
                var pacifierGrid = FindPacifierGrid(pacifierItem.PacifierId);
                if (pacifierGrid == null)
                {
                    Debug.WriteLine($"Monitoring: Grid not found for Pacifier {pacifierItem.PacifierId}.");
                    continue;
                }

                // Iterate through each SensorGroup in the sensor item's MeasurementGroup
                foreach (var sensorGroup in sensorItem.MeasurementGroup)
                {
                    var firstKvp = sensorGroup.FirstOrDefault();

                    if (firstKvp.Key != null && firstKvp.Key == "sensorGroup") // Ensure there's at least one key-value pair
                    {
                        var groupName = firstKvp.Value;

                        foreach (UIElement child in pacifierGrid.Children)
                        {
                            // Find the sensor text row by matching the SensorId in the TextBlock
                            if (child is Border border && border.Child is TextBlock textBlock && textBlock.Text == $"Sensor: {sensorItem.SensorId}")
                            {
                                pacifierGrid.Children.Remove(child);
                                Debug.WriteLine($"Monitoring: Removed Sensor Row for Pacifier {pacifierItem.PacifierId}, Sensor {sensorItem.SensorId}");
                                break; // Since we're only removing one row, break the loop after removal
                            }
                        }

                        // Remove the corresponding WrapPanel (graph row)
                        var wrapPanelToRemove = pacifierGrid.Children
                            .OfType<WrapPanel>()
                            .FirstOrDefault(wrapPanel => wrapPanel.Uid == $"{pacifierItem.PacifierId}_{sensorItem.SensorId}");

                        if (wrapPanelToRemove != null)
                        {
                            int rowIndex = Grid.GetRow(wrapPanelToRemove);
                            pacifierGrid.Children.Remove(wrapPanelToRemove);

                            // Optionally, remove the corresponding row definition
                            //if (rowIndex < pacifierGrid.RowDefinitions.Count)
                            //{
                            //    pacifierGrid.RowDefinitions.RemoveAt(rowIndex);
                            //}

                            Debug.WriteLine($"Monitoring: Removed graph row for {groupName} in Pacifier {pacifierItem.PacifierId}");
                        }
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
                .FirstOrDefault(g => g.Uid == $"Grid_{pacifierItem.PacifierId}");

            return gridToFind;
        }

        private Grid? FindPacifierGrid(string pacifierId)
        {
            return pacifierSectionsPanel.Children
                .OfType<Grid>()
                .FirstOrDefault(g => g.Uid == $"Grid_{pacifierId}");
        }



        // Create multiple Graphs for a SensorItem based on its SensorGroups
        private WrapPanel CreateGraphForSensor(PacifierItem pacifierItem,SensorItem sensorItem)
        {

            // Create a unique identifier for the graph
            string uniqueUid = $"{pacifierItem.PacifierId}_{sensorItem.SensorId}";

            WrapPanel graphPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                Uid = uniqueUid
            };

            Debug.WriteLine($"Monitoring: Created Container for graphs");

            // Iterate through each SensorGroup in the SensorItem
            foreach (var sensorGroup in sensorItem.SensorGroups)
            {

                // Retrieve the saved interval for this SensorItem and SensorGroup from the dictionary
                int interval = _viewModel.SensorIntervals[sensorGroup];
                Debug.WriteLine($"Monitoring: Interval is {interval}");

                // Create a unique identifier for each graph based on SensorId and groupName
                string uniquePlotId = $"{sensorItem.SensorId}_{sensorGroup}_{pacifierItem.PacifierId}";

                // Create an instance of the LineChartGraph with necessary properties
                LineChartGraph graph = new LineChartGraph(sensorItem, sensorGroup, interval)
                {
                    Height = 250,
                    Uid = uniquePlotId,
                    Name = sensorGroup,
                    PlotId = uniquePlotId,  // Ensure unique PlotId
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    Margin = new Thickness(5)
                };

                // Additional configurations for the LineChartGraph
                //graph.PlotModel.IsLegendVisible = true;

                // Optionally set specific properties (like disabling zoom) if handled in the class



                // Add the graph to the SensorItem's collection for future reference
                sensorItem.SensorGraphs.Add(graph);

                // Add the graph to the WrapPanel for UI display
                graphPanel.Children.Add(graph);

                Debug.WriteLine($"Monitoring: Created Graph for Sensor Group {sensorGroup} with unique PlotId {uniquePlotId}");
            }

            return graphPanel;
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
                var pacifierItem = _viewModel.PacifierItems.FirstOrDefault(p => p.PacifierId == pacifierName);

                Debug.WriteLine($"Monitoring: Pacifier Item is null {pacifierName}");

                // Create an instance of RawDataView with the properties and a reference to this view
                if (pacifierItem != null)
                {
                    Debug.WriteLine($"Monitoring: Passing {pacifierItem.PacifierId}");

                    var rawDataView = new RawDataView(pacifierItem, this, true);

                    // Replace the current view with RawDataView
                    var parent = this.Parent as ContentControl;
                    if (parent != null)
                    {
                        parent.Content = rawDataView;
                    }
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
            if (_viewModel.CheckedSensorItems.Count > 0)
            {

                var dialog = new IntervalSettingsDialog(new List<PacifierItem>(_viewModel.CheckedPacifierItems), new List<SensorItem>(_viewModel.CheckedSensorItems), _viewModel.SensorIntervals);
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
            else
            {
                MessageBox.Show("Please Select at least 1 Sensor.");
            }
        }

        private void Frequency_Button(object sender, RoutedEventArgs e)
        {
            if (_viewModel.PacifierItems != null)
            {
                var inputDialog = new InputDialog(_viewModel.PacifierItems);
                inputDialog.ShowDialog();
            }
            else
            {
                MessageBox.Show("No Pacifiers Available.");
            }
        }

        /// <summary>
        /// Handles the End Campaign button click.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="e"></param>
        private void EndCampaign_Button(object sender, RoutedEventArgs e)
        {
            // Show a confirmation dialog
            MessageBoxResult result = MessageBox.Show(
                "Are you sure you want to end the campaign? This action cannot be undone.",
                "End Campaign Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            // Check the user's response
            if (result == MessageBoxResult.Yes)
            {
                // Get the current time as the campaign end time
                string endTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // User confirmed, proceed with ending the campaign
                EndCampaign(endTime);
            }
            else
            {
                // User chose not to proceed
                MessageBox.Show("Campaign not ended.", "Operation Cancelled", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Ends the campaign.
        /// </summary>
        /// <param name="endTime">The end time of the campaign in "yyyy-MM-dd HH:mm:ss" format.</param>
        public async Task EndCampaign(string endTime)
        {
            try
            {
                _viewModel.LineProtocolService.UpdateStoppedEntryTime(_viewModel.CurrentCampaignName, endTime);

                // Show a success message
                MessageBox.Show("Campaign has been successfully ended.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Unsubscribe from real-time updates to stop file writing
                Broker.Instance.MessageReceived -= _viewModel.OnMessageReceived;

                // Store the campaign name before resetting
                var campaignName = _viewModel.CurrentCampaignName;

                // Reset the current campaign name
                _viewModel.CurrentCampaignName = string.Empty;

                // Get an instance of FileUpload from the DI container
                var fileUpload = ((App)Application.Current).ServiceProvider.GetRequiredService<FileUpload>();
                await fileUpload.UploadDataAsync(campaignName);

                // Retrieve PacifierSelectionView from the DI container and navigate to it
                var pacifierSelectionView = ((App)Application.Current).ServiceProvider.GetRequiredService<PacifierSelectionView>();
                ((MainWindow)Application.Current.MainWindow).NavigateTo(pacifierSelectionView);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error ending campaign: {ex.Message}");
                MessageBox.Show($"Failed to end campaign. Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }




    }
}

