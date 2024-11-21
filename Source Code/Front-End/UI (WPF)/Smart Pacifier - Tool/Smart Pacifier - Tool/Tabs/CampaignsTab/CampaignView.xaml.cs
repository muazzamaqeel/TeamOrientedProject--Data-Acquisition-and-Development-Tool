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
using System.Diagnostics;


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
        public const string DeveloperTabVisibleKey = "DeveloperTabVisible";

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
            var sensorGroupData = new Dictionary<string, List<Dictionary<string, double>>>();

            var groupedData = jsonDataEntries
                .Select(jsonData => JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData))
                .GroupBy(entry => entry.TryGetValue("pacifier_name", out var pacifierName) && pacifierName != null ? pacifierName : string.Empty)
                .Select(pacifierGroup => new
                {
                    PacifierName = pacifierGroup.Key,
                    SensorGroups = pacifierGroup
                        .GroupBy(entry => entry.TryGetValue("sensor_type", out var sensorType) && sensorType != null ? sensorType : string.Empty)
                        .Select(sensorGroup => new
                        {
                            SensorType = sensorGroup.Key,
                            FieldGroups = sensorGroup
                                .GroupBy(entry => entry.TryGetValue("sensorGroup", out var field) && field != null ? field : string.Empty)
                                .Select(fieldGroup => new
                                {
                                    FieldName = fieldGroup.Key,
                                    OriginalFieldGroups = fieldGroup
                                        .GroupBy(entry => entry.TryGetValue("_field", out var field) && field != null ? field : string.Empty)
                                        .Select(originalFieldGroup => new
                                        {
                                            OriginalFieldName = originalFieldGroup.Key,
                                            LineSeries = new LineSeries
                                            {
                                                ItemsSource = originalFieldGroup
                                                .Select(entry =>
                                                {
                                                    double time = entry.TryGetValue("_time", out var timeObj) && timeObj != null ? DateTimeConverter(timeObj.ToString()) : 0;
                                                    double value = entry.TryGetValue("_value", out var valueObj) && valueObj != null && double.TryParse(valueObj.ToString(), out var parsedValue) ? parsedValue : 0;
                                                    return new DataPoint(time, value);
                                                })
                                                .ToList()
                                            }
                                        })
                                        .ToList(),
                                    KeyValuePairs = fieldGroup
                                    .GroupBy(entry => entry.TryGetValue("_time", out var timeObj) && timeObj != null ? timeObj : 0)
                                    .Select(timeGroup => new
                                    {
                                        Time = timeGroup.Key,
                                        Fields = timeGroup
                                            .GroupBy(entry => entry.TryGetValue("_field", out var fieldObj) && fieldObj != null ? fieldObj.ToString() : string.Empty)
                                            .ToDictionary(
                                                g => g.Key,
                                                g => g.First().TryGetValue("_value", out var valueObj) && double.TryParse(valueObj?.ToString(), out var parsedValue) ? parsedValue : 0
                                            )
                                    })
                                    .ToDictionary(
                                        tg => tg.Time,
                                        tg => tg.Fields
                                    )
                                })
                                .ToList()
                        })
                        .ToList()
                })
                .ToList();

            _groupedData = groupedData;
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

            // Set up the first row with 3 columns
            pacifierGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pacifierGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pacifierGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        private void AddPacifierHeader(Grid pacifierGrid, PacifierItem pacifierItem)
        {
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
            Grid.SetColumnSpan(rowBorder, 3);
            pacifierGrid.Children.Add(rowBorder);

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
      
                pacifierItem.CampaignData.Add(sensorGroup);

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

            // Retrieve the color from application resources
            var brush = (Brush)Application.Current.Resources["MainViewForegroundColor"];
            var oxyColor = brush.ToOxyColor();

            // Create a plot for each field name
            var plotModel = new PlotModel
            {
                Title = fieldName,
                TitleColor = oxyColor,
            };

            // Add a legend to the plot model
            plotModel.Legends.Add(new Legend
            {
                LegendPosition = LegendPosition.TopRight,
                LegendPlacement = LegendPlacement.Outside,
                LegendOrientation = LegendOrientation.Horizontal,
                LegendBorderThickness = 0,
                TextColor = oxyColor,
                LegendTextColor = oxyColor
            });

            var dateTimeAxis = new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                StringFormat = "HH:mm:ss",
                IntervalType = DateTimeIntervalType.Auto,
                IntervalLength = 80, // Adjust this value to control the spacing of labels
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                TextColor = oxyColor
            };
            plotModel.Axes.Add(dateTimeAxis);

            var valueAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                TextColor = oxyColor
            };
            plotModel.Axes.Add(valueAxis);

            OxyColor[] blueShades = new OxyColor[]
            {
        OxyColor.FromRgb(0, 0, 255),    // Pure Blue
        OxyColor.FromRgb(90, 90, 255),  // Even lighter Blue
        OxyColor.FromRgb(255, 255, 255),  // Light Blue
        OxyColor.FromRgb(120, 120, 255), // Very light Blue
        OxyColor.FromRgb(60, 60, 255)  // Lighter Blue
            };

            int colorIndex = 0;

            foreach (var originalFieldGroup in fieldGroup.OriginalFieldGroups)
            {
                var lineSeries = originalFieldGroup.LineSeries;

                // Ensure the line series has a title for the legend
                lineSeries.Title = originalFieldGroup.OriginalFieldName;

                // Add markers to the line series
                lineSeries.MarkerType = MarkerType.Square;
                lineSeries.MarkerSize = 2;

                // Set the color of the line series
                lineSeries.Color = blueShades[colorIndex % blueShades.Length];
                colorIndex++;

                // Check if the lineSeries is already part of another plot model
                if (lineSeries.PlotModel != null)
                {
                    // Remove the lineSeries from its current plot model
                    lineSeries.PlotModel.Series.Remove(lineSeries);
                }

                // Add the lineSeries to the new plot model
                plotModel.Series.Add(lineSeries);
            }

            // Create a PlotView with the rounded corner Border
            var plotView = new PlotView
            {
                Model = plotModel,
                Height = 250,
                Margin = new Thickness(5),
                Tag = sensorType,
                Background = (Brush)Application.Current.Resources["MainViewSecondaryBackgroundColor"]
            };

            // Wrap the PlotView in a Border to apply rounded corners
            var border = new Border
            {
                Margin = new Thickness(5),
                CornerRadius = new CornerRadius(8), // Set corner radius for rounded corners
                Background = (Brush)Application.Current.Resources["MainViewSecondaryBackgroundColor"],
                Padding = new Thickness(5), // Add padding for inner spacing
                Child = plotView // Place the PlotView inside the Border
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

            Grid.SetRow(border, sensorGroupRowIndex); // Set the row for the Border
            Grid.SetColumn(border, columnIndex); // Set the column for the Border
            pacifierGrid.Children.Add(border); // Add the Border (with PlotView) to the grid

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
        private void AddSensorRow(Grid pacifierGrid, SensorItem sensorItem, PacifierItem pacifierItem)
        {
            // Check if the sensor header already exists
            bool headerExists = false;
            foreach (UIElement child in pacifierGrid.Children)
            {
                if (child is Border border && border.Child is TextBlock textBlock && textBlock.Text == $"Sensor: {sensorItem.SensorId}")
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

                // Add the Graph for the Sensor
                ScrollViewer graphScrollViewer = new ScrollViewer
                {
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                    VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
                    Uid = sensorItem.SensorId
                };

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
                    // Remove the graph rows for the matching sensor item
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

                // Iterate through the grid's children to find the border and graph row
                foreach (UIElement child in pacifierGrid.Children)
                {
                    // Check if the child is a Border containing the TextBlock
                    if (child is Border border && border.Child is TextBlock textBlock && textBlock.Text == $"Sensor: {sensorItem.SensorId}")
                    {
                        elementsToRemove.Add(child); // Add the Border (containing TextBlock) for removal
                    }
                    // Check if the child is a PlotView associated with this sensor
                    else if (child is PlotView plotView && plotView.Tag?.ToString() == sensorItem.SensorId)
                    {
                        elementsToRemove.Add(child); // Add PlotView for removal
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
                var pacifierItem = _viewModel.PacifierItems.FirstOrDefault(p => p.PacifierId == pacifierName);
                // Create an instance of RawDataView with the properties and a reference to this view
                if (pacifierItem != null)
                {
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

