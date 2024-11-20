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
        public MonitoringView(ObservableCollection<PacifierItem> selectedPacifiers, ILineProtocol lineProtocol, string currentCampaignName)
        {
            InitializeComponent();

            // Initialize the ViewModel
            _viewModel = new MonitoringViewModel(lineProtocol, currentCampaignName);

            // Bind the ViewModel to the DataContext
            this.DataContext = _viewModel;

            // Clear items
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
            Grid.SetColumn(pacifierNameTextBox, 1);
            pacifierGrid.Children.Add(pacifierNameTextBox);

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
                Tag = pacifierItem.PacifierId // Use Tag to hold the pacifier id
            };

            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                var sidebar = mainWindow.Sidebar; // Ensure SidebarInstance is accessible in MainWindow

                if (sidebar != null && Application.Current.Properties.Contains(Sidebar.DeveloperTabVisibleKey))
                {
                    bool isDeveloperTabVisible = (bool)Application.Current.Properties[Sidebar.DeveloperTabVisibleKey];

                    if (isDeveloperTabVisible)
                    {
                        debugButton.Visibility = Visibility.Visible;
                        Debug.WriteLine("Monitoring: Developer Mode");
                    }
                    else
                    {
                        debugButton.Visibility = Visibility.Collapsed;
                        Debug.WriteLine("Monitoring: User Mode");
                    }
                }
                else
                {
                    debugButton.Visibility = Visibility.Collapsed;
                    Debug.WriteLine("Monitoring: UserMode is null");
                }
            }
            else
            {
                debugButton.Visibility = Visibility.Collapsed;
                Debug.WriteLine("Monitoring: MainWindow is null");
            }

            Grid.SetRow(debugButton, 0);
            Grid.SetColumn(debugButton, 2);
            debugButton.Click += OpenRawDataView_Click; // Directly attach the event handler
            pacifierGrid.Children.Add(debugButton);

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

            // Create a new row definition for this sensor's graphs
            RowDefinition sensorRow = new RowDefinition();
            pacifierGrid.RowDefinitions.Add(sensorRow);

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

            Debug.WriteLine($"Monitoring: Added SensorRow for Pacifier {pacifierItem.PacifierId}, Sensor {sensorItem.SensorId} at Row {newRowIndex}");
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
        // Remove Graph Row for a Specific SensorItem in a PacifierItem Grid
        private void RemoveSensorRow(PacifierItem pacifierItem, SensorItem sensorItem)
        {
            Grid? pacifierGrid = FindPacifierGrid(pacifierItem.PacifierId);
            if (pacifierGrid == null) return;

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

                // Iterate through each SensorGroup in the sensor item
                foreach (var sensorGroup in sensorItem.MeasurementGroup)
                {
                    var firstKvp = sensorGroup.FirstOrDefault();

                    if (firstKvp.Key != null && firstKvp.Key == "sensorGroup") // Ensure there's at least one key-value pair
                    {
                        var groupName = firstKvp.Value;

                        // Find the WrapPanel that contains the graph for this sensor group
                        var wrapPanelToRemove = pacifierGrid.Children
                            .OfType<WrapPanel>()
                            .FirstOrDefault(wrapPanel => wrapPanel.Uid == $"{pacifierItem.PacifierId}_{sensorItem.SensorId}");

                        if (wrapPanelToRemove != null)
                        {
                            int rowIndex = Grid.GetRow(wrapPanelToRemove);
                            pacifierGrid.Children.Remove(wrapPanelToRemove);

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
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
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
                    Height = 300,
                    Uid = uniquePlotId,
                    Name = sensorGroup,
                    PlotId = uniquePlotId,  // Ensure unique PlotId
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                    Margin = new Thickness(5)
                };

                // Additional configurations for the LineChartGraph
                //graph.PlotModel.IsLegendVisible = true;

                // Optionally set specific properties (like disabling zoom) if handled in the class
                graph.PlotModel.Axes.Add(new DateTimeAxis
                {
                    Position = AxisPosition.Bottom,
                    StringFormat = "HH:mm:ss",
                    Title = "Time",
                    IsZoomEnabled = false,
                    IsPanEnabled = false,
                    IntervalLength = interval
                });

                graph.PlotModel.Axes.Add(new LinearAxis
                {
                    Position = AxisPosition.Left,
                    IsZoomEnabled = false,
                    IsPanEnabled = false
                });

                // Add a legend to the plot model
                graph.PlotModel.Legends.Add(new Legend
                {
                    LegendPosition = LegendPosition.TopRight,
                    LegendPlacement = LegendPlacement.Outside,
                    LegendOrientation = LegendOrientation.Horizontal,
                    LegendBorderThickness = 0
                });


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
                var pacifierItem = _viewModel.CheckedPacifierItems.FirstOrDefault(p => p.PacifierId == pacifierName);

                Debug.WriteLine($"Monitoring: Pacifier Item is null {pacifierName}");

                // Create an instance of RawDataView with the properties and a reference to this view
                if (pacifierItem != null)
                {
                    Debug.WriteLine($"Monitoring: Passing {pacifierItem.PacifierId}");

                    var rawDataView = new RawDataView(pacifierItem, this, false);

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

