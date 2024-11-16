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
            ScrollViewer graphScrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
                Uid = uniqueUid
            };

            // Set the new row index for the ScrollViewer
            int newRowIndex = pacifierGrid.RowDefinitions.Count;
            Grid.SetRow(graphScrollViewer, newRowIndex);
            Grid.SetColumnSpan(graphScrollViewer, 3);

            pacifierGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            graphScrollViewer.Content = CreateGraphForSensor(sensorItem);
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
        private void RemoveSensorRow(PacifierItem pacifierItem, SensorItem sensorItem)
        {
            Grid? pacifierGrid = FindPacifierGrid(pacifierItem.PacifierId);
            if (pacifierGrid == null) return;

            var scrollViewerToRemove = pacifierGrid.Children
                .OfType<ScrollViewer>()
                .FirstOrDefault(s => s.Uid == $"{pacifierItem.PacifierId}_{sensorItem.SensorId}");

            if (scrollViewerToRemove != null)
            {
                int rowIndex = Grid.GetRow(scrollViewerToRemove);
                pacifierGrid.Children.Remove(scrollViewerToRemove);

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

                        // Find the ScrollViewer that contains the graph for this sensor group
                        var scrollViewerToRemove = pacifierGrid.Children
                            .OfType<ScrollViewer>()
                            .FirstOrDefault(scrollViewer => scrollViewer.Uid == $"{pacifierItem.PacifierId}_{sensorItem.SensorId}");

                        if (scrollViewerToRemove != null)
                        {
                            int rowIndex = Grid.GetRow(scrollViewerToRemove);
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
        private UIElement CreateGraphForSensor(SensorItem sensorItem)
        {
            WrapPanel graphPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
            };

            Debug.WriteLine($"Monitoring: Created Container for graphs");

            // Iterate through each SensorGroup in the SensorItem
            foreach (var sensorGroup in sensorItem.SensorGroups)
            {
                // Create a unique identifier for each graph based on SensorId and groupName
                string uniquePlotId = $"{sensorItem.SensorId}_{sensorGroup}";

                LineChartGraph graph = new LineChartGraph(sensorItem, sensorGroup)
                {
                    Width = 350,
                    Height = 200,
                    Name = uniquePlotId,
                    PlotId = uniquePlotId,  // Ensure unique PlotId
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                    Margin = new Thickness(5)
                };

                sensorItem.SensorGraphs.Add(graph);

                graphPanel.Children.Add(graph);
                Debug.WriteLine($"Monitoring: Created Graph for Sensor Group {sensorGroup} with unique PlotId {uniquePlotId}");
            }

            return graphPanel;
        }

        // ================ Graph Data Bind ===============



        //private LineChartGraph FindGraphByPlotId(string plotId)
        //{
        //    // Search for the graph with the specific PlotId
        //    foreach (var child in graphPanel.Children)
        //    {
        //        if (child is LineChartGraph graph && graph.PlotId == plotId)
        //        {
        //            return graph;
        //        }
        //    }
        //    return null;
        //}

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
            if (_viewModel.CheckedSensorItems.Count > 0)
            {

                var dialog = new IntervalSettingsDialog(new List<PacifierItem>(_viewModel.CheckedPacifierItems), new List<SensorItem>(_viewModel.CheckedSensorItems));
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
                // User confirmed, proceed with ending the campaign
                EndCampaign();
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
        private void EndCampaign()
        {
            // Logic to end the campaign goes here
            MessageBox.Show("Campaign has been successfully ended.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

            // Retrieve PacifierSelectionView from the DI container
            var pacifierSelectionView = ((App)Application.Current).ServiceProvider.GetRequiredService<PacifierSelectionView>();
            ((MainWindow)Application.Current.MainWindow).NavigateTo(pacifierSelectionView);
        }

    }
}

