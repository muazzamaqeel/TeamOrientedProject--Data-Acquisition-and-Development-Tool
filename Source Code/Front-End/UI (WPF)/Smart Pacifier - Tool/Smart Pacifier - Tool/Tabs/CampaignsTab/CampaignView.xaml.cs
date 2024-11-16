using Smart_Pacifier___Tool.Components;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SmartPacifier.Interface.Services;
using System.Collections.Specialized;
using Smart_Pacifier___Tool.Tabs.MonitoringTab;
using Microsoft.Scripting.Utils;
using Newtonsoft.Json;
using OxyPlot.Series;
using OxyPlot.Wpf;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;


namespace Smart_Pacifier___Tool.Tabs.CampaignsTab
{
    /// <summary>
    /// 
    /// </summary>
    public partial class CampaignView : UserControl
    {
        private readonly CampaignViewModel _viewModel;
        private readonly IManagerCampaign _managerCampaign;
        private readonly string? _campaignName;
        private List<CampaignDataEntry> _campaignDataEntries = new List<CampaignDataEntry>();
        private object _groupedData;
        private List<Grid> _pacifierGrids = new List<Grid>(); // List to store pacifier grids


        /// <summary>
        /// 
        /// </summary>
        /// <param name="selectedPacifiers"></param>
        public CampaignView(IManagerCampaign managerCampaign, string campaignName)
        {
            InitializeComponent();
            _managerCampaign = managerCampaign;
            _campaignName = campaignName;

            // Initialize the ViewModel
            _viewModel = new CampaignViewModel
            {
                CampaignName = campaignName
            };

            // Bind the ViewModel to the DataContext
            this.DataContext = _viewModel;

            // Clear items
            _viewModel.SensorTypes.Clear();
            _viewModel.PacifierItems.Clear();

            LoadPacifiersForCampaign();

            pacifierFilterPanel.ItemsSource = _viewModel.PacifierItems;
            sensorFilterPanel.ItemsSource = _viewModel.SensorItems;
            LoadCampaignData();

            foreach (var pacifier in _viewModel.PacifierItems) // Loop through each sensor
            {
                pacifier.Sensors.CollectionChanged += OnSensorTypesCollectionChanged;
            }

        }

        // Load pacifiers associated with the campaign
        private async void LoadPacifiersForCampaign()
        {
            var pacifierNames = await _managerCampaign.GetPacifiersByCampaignNameAsync(_campaignName);
            ObservableCollection<PacifierItem> pacifierItems = new ObservableCollection<PacifierItem>();

            foreach (var name in pacifierNames)
            {
                PacifierItem pacifierItem = new(name)
                {
                    IsChecked = true
                };
                pacifierItems.Add(pacifierItem);

                var sensors = await _managerCampaign.GetSensorsByPacifierNameAsync(name, _campaignName);
                ObservableCollection<SensorItem> sensorItems = new ObservableCollection<SensorItem>();
                foreach (var sensor in sensors)
                {
                    SensorItem sensorItem = new(sensor, pacifierItem);
                    sensorItems.Add(sensorItem);
                }
                AddSensorItems(sensorItems);

            }
            AddPacifierItems(pacifierItems);
        }

        private async void LoadCampaignData()
        {
            var campaignData = await _managerCampaign.GetCampaignDataEntriesAsync(_campaignName);
            GroupAndProcessData(campaignData);
        }

        private void GroupAndProcessData(List<string> jsonDataEntries)
        {
            var groupedData = jsonDataEntries
                .Select(jsonData => JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData))
                .GroupBy(entry => entry.TryGetValue("pacifier_name", out var pacifierName) ? pacifierName : null)
                .Select(pacifierGroup => new
                {
                    PacifierName = pacifierGroup.Key,
                    SensorGroups = pacifierGroup
                        .GroupBy(entry => entry.TryGetValue("sensor_type", out var sensorType) ? sensorType : null)
                        .Select(sensorGroup => new
                        {
                            SensorType = sensorGroup.Key,
                            FieldGroups = sensorGroup
                                .GroupBy(entry => entry.TryGetValue("_field", out var field) ? GetModifiedFieldName(field?.ToString() ?? string.Empty) : null)
                                .Select(fieldGroup => new
                                {
                                    FieldName = fieldGroup.Key,
                                    OriginalFieldGroups = fieldGroup
                                        .GroupBy(entry => entry.TryGetValue("_field", out var field) ? field : null)
                                        .Select(originalFieldGroup => new
                                        {
                                            OriginalFieldName = originalFieldGroup.Key,
                                            LineSeries = new LineSeries
                                            {
                                                ItemsSource = originalFieldGroup
                                                .Select(entry =>
                                                {
                                                    double time = DateTimeConverter(entry["_time"].ToString());
                                                    double value = entry.TryGetValue("_value", out var valueObj) && double.TryParse(valueObj.ToString(), out var parsedValue) ? parsedValue : 0;
                                                    return new DataPoint(time, value);
                                                })
                                                .ToList()
                                            }
                                        })
                                        .ToList()
                                })
                                .ToList()
                        })
                        .ToList()
                })
                .ToList();

            _groupedData = groupedData;
        }
        private string GetModifiedFieldName(string field)
        {
            var underscoreIndex = field.IndexOf('_');
            return underscoreIndex >= 0 ? field.Substring(0, underscoreIndex) : field;
        }

      
        private double DateTimeConverter (string dateTimeString)
        {
            DateTime dateTime = DateTime.Parse(dateTimeString);
            return DateTimeAxis.ToDouble(dateTime);
        }

        private void OnSensorTypesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Debug.WriteLine($"Monitoring: SensorTypes CollectionChanged Called");

            // Only call AddSensorItems if there are new items added to the collection
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                // Convert e.NewItems (which is an IList) to an ObservableCollection<Sensor>
                ObservableCollection<SensorItem> newSensors = new ObservableCollection<SensorItem>(e.NewItems.Cast<SensorItem>());

                AddSensorItems(newSensors);
            }
        }

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

                pacifierItem.ButtonText = $"{pacifierItem.PacifierId}";
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sensorTypes"></param>
        private void AddSensorItems(ObservableCollection<SensorItem> sensorItems)
        {
            foreach (var sensorItem in sensorItems)
            {
                if (!_viewModel.SensorItems.Any(p => p.SensorId == sensorItem.SensorId))
                {
                    sensorItem.LinkedPacifiers = _viewModel.PacifierItems;
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
            }
        }

        private void AddPacifierRow(Grid pacifierGrid, PacifierItem pacifierItem)
        {
            InitializePacifierGrid(pacifierGrid);
            AddPacifierHeader(pacifierGrid, pacifierItem);
            _pacifierGrids.Add(pacifierGrid);
        }

        private void InitializePacifierGrid(Grid pacifierGrid)
        {
            RowDefinition row = new RowDefinition();
            pacifierGrid.RowDefinitions.Add(row);

            pacifierGrid.Background = (Brush)Application.Current.FindResource("MainViewBackgroundColor");
            pacifierGrid.Margin = new Thickness(5);

            // Set up the first row with 3 columns
            pacifierGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pacifierGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pacifierGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        private void AddPacifierHeader(Grid pacifierGrid, PacifierItem pacifierItem)
        {
            // Create the first TextBlock for pacifier name
            TextBlock pacifierNameTextBox = new()
            {
                Text = pacifierItem.ButtonText,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                FontSize = 20,
                Foreground = (Brush)Application.Current.Resources["MainViewForegroundColor"]
            };
            Grid.SetRow(pacifierNameTextBox, 0);
            Grid.SetColumn(pacifierNameTextBox, 0);
            pacifierGrid.Children.Add(pacifierNameTextBox);

            // Create the "Debug" button
            /*
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
              */
        }

        private void AddSensorGroups(Grid pacifierGrid, PacifierItem pacifierItem, String sensorID)
        {
            var groupedData = (dynamic)_groupedData;
            var matchingPacifierGroup = ((IEnumerable<dynamic>)groupedData).FirstOrDefault(p => p.PacifierName == pacifierItem.PacifierId);

            // Loop through all the sensor groups
            foreach (var sensorGroup in matchingPacifierGroup.SensorGroups)
            {
                var sensorType = sensorGroup.SensorType;
                if(sensorType != sensorID)
                {
                    continue;
                }

                // Add a new row for the sensor group
                var sensorGroupRowIndex = pacifierGrid.RowDefinitions.Count;
                pacifierGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                // Access other nested groups if needed
                var fieldGroups = sensorGroup.FieldGroups;
                int columnIndex = 0; // Initialize column index for each sensor group
                foreach (var fieldGroup in fieldGroups)
                {
                    AddFieldGroup(pacifierGrid, sensorGroupRowIndex, ref columnIndex, sensorType, fieldGroup);
                }
            }
        }

        private void AddFieldGroup(Grid pacifierGrid, int sensorGroupRowIndex, ref int columnIndex, string sensorType, dynamic fieldGroup)
        {
            var fieldName = fieldGroup.FieldName;

            // Create a plot for each field name
            var plotModel = new PlotModel { Title = $"{fieldName}" };

            // Add a legend to the plot model
            plotModel.Legends.Add(new Legend
            {
                LegendPosition = LegendPosition.TopRight,
                LegendPlacement = LegendPlacement.Outside,
                LegendOrientation = LegendOrientation.Horizontal,
                LegendBorderThickness = 0
            });

            foreach (var originalFieldGroup in fieldGroup.OriginalFieldGroups)
            {
                var lineSeries = originalFieldGroup.LineSeries;

                // Ensure the line series has a title for the legend
                lineSeries.Title = originalFieldGroup.OriginalFieldName;

                // Check if the lineSeries is already part of another plot model
                if (lineSeries.PlotModel != null)
                {
                    // Remove the lineSeries from its current plot model
                    lineSeries.PlotModel.Series.Remove(lineSeries);
                }

                // Add the lineSeries to the new plot model
                plotModel.Series.Add(lineSeries);
            }

            var plotView = new PlotView
            {
                Model = plotModel,
                Height = 300,
                Margin = new Thickness(5),
                Tag = $"{sensorType}"
            };

            // Add a new column for each plot if necessary
            if (columnIndex >= 3)
            {
                columnIndex = 0;
                sensorGroupRowIndex = pacifierGrid.RowDefinitions.Count;
                pacifierGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            if (pacifierGrid.ColumnDefinitions.Count <= columnIndex)
            {
                pacifierGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            }

            Grid.SetRow(plotView, sensorGroupRowIndex);
            Grid.SetColumn(plotView, columnIndex);
            pacifierGrid.Children.Add(plotView);

            columnIndex++; // Increment column index for the next plot
        }


        // Add Graph Rows for all Toggled SensorItems in this PacifierItem Grid
        private void AddGraphRowsForToggledSensors(PacifierItem pacifierItem)
        {
            Grid grid = _viewModel.PacifierGridMap[pacifierItem];

            foreach (var sensorItem in _viewModel.CheckedSensorItems)
            {
                AddSensorRow(grid, sensorItem, pacifierItem);
            }
        }

        // Add Graph Row for a Specific SensorItem in a PacifierItem Grid
        private void AddSensorRow(Grid pacifierGrid, SensorItem sensorItem,PacifierItem pacifierItem)
        {
            // Check if the sensor header already exists
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

                // Add a header for the sensor item
                TextBlock sensorHeader = new TextBlock
                {
                    Text = $"Sensor: {sensorItem.SensorId}",
                    Margin = new Thickness(15),
                    Foreground = (Brush)Application.Current.Resources["MainViewForegroundColor"],
                    Background = (Brush)Application.Current.Resources["MainViewSecondaryBackgroundColor"],
                    TextAlignment = TextAlignment.Center, // Center the text
                    FontSize = 16, // Increase the text size
                    Padding = new Thickness(8), // Add padding
                };
                Grid.SetRow(sensorHeader, pacifierGrid.RowDefinitions.Count - 1);
                Grid.SetColumnSpan(sensorHeader, 3); // Span 3 columns
                pacifierGrid.Children.Add(sensorHeader);

                ScrollViewer graphScrollViewer = new ScrollViewer
                {
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                    VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
                    Uid = sensorItem.SensorId
                };

                //graphScrollViewer.Content = CreateGraphForSensor(sensorItem);

                pacifierGrid.Children.Add(graphScrollViewer);

                AddSensorGroups(pacifierGrid, pacifierItem, sensorItem.SensorId);

                // Save the row reference in the map
                _viewModel.SensorRowMap[new Tuple<PacifierItem, SensorItem>(sensorItem.ParentPacifierItem.GetPacifierItem(), sensorItem)] = sensorRow;
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
            // List to store elements to be removed
            var elementsToRemove = new List<UIElement>();

            // Iterate through the grid's children to find the header and graph row
            foreach (UIElement child in grid.Children)
            {
                if (child is TextBlock textBlock && textBlock.Text == $"Sensor: {sensorItem.SensorId}")
                {
                    elementsToRemove.Add(child);
                }
                else if (child is PlotView plotView && plotView.Tag?.ToString() == sensorItem.SensorId)
                {
                    elementsToRemove.Add(child);
                }
            }

            // Remove the identified elements from the grid
            foreach (var element in elementsToRemove)
            {
                grid.Children.Remove(element);
            }

            // Optionally, remove the corresponding row definitions if they are empty
            for (int i = grid.RowDefinitions.Count - 1; i >= 0; i--)
            {
                bool isRowEmpty = true;
                foreach (UIElement child in grid.Children)
                {
                    if (Grid.GetRow(child) == i)
                    {
                        isRowEmpty = false;
                        break;
                    }
                }

                if (isRowEmpty)
                {
                    grid.RowDefinitions.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Removes the graph rows for the specified SensorItem from the PacifierItem grid.
        /// </summary>
        /// <param name="sensorItem">The SensorItem whose graph rows should be removed.</param>
        private void RemoveGraphRowsForSensorItem(SensorItem sensorItem)
        {
            foreach (var pacifierGrid in _pacifierGrids)
            {
                var elementsToRemove = new List<UIElement>();

                // Iterate through the grid's children to find the header and graph row
                foreach (UIElement child in pacifierGrid.Children)
                {
                    if (child is TextBlock textBlock && textBlock.Text == $"Sensor: {sensorItem.SensorId}")
                    {
                        elementsToRemove.Add(child);
                    }
                    else if (child is PlotView plotView && plotView.Tag?.ToString() == sensorItem.SensorId)
                    {
                        elementsToRemove.Add(child);
                    }
                }

                // Remove the identified elements from the grid
                foreach (var element in elementsToRemove)
                {
                    pacifierGrid.Children.Remove(element);
                }

            }
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
                var rawDataView = new RawDataView(pacifierName, this, false);

                // Replace the current view with RawDataView
                var parent = this.Parent as ContentControl;
                if (parent != null)
                {
                    parent.Content = rawDataView;
                }
            }
        }

        public class CampaignDataEntry
        {
            [JsonProperty("_time")]
            public object Time { get; set; }

            [JsonProperty("_value")]
            public double Value { get; set; }

            [JsonProperty("_field")]
            public string Field { get; set; }

            [JsonProperty("_measurement")]
            public string Measurement { get; set; }

            [JsonProperty("pacifier_name")]
            public string PacifierName { get; set; }

            [JsonProperty("sensor_type")]
            public string SensorType { get; set; }
        }
    }
}

