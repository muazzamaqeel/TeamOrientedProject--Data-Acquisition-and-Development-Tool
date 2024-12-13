﻿using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using InfluxDB.Client.Api.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartPacifier.BackEnd.DatabaseLayer.InfluxDB.Connection;
using SmartPacifier.Interface.Services;
using Microsoft.Extensions.Configuration.Json;
using System.Windows.Media;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot;
using System.Windows.Threading;
using OxyPlot.Wpf;

namespace Smart_Pacifier___Tool.Tabs.SettingsTab
{
    public partial class SettingsView : UserControl
    {
        private const string UserModeKey = "UserMode";
        private const string DeveloperTabVisibleKey = "DeveloperTabVisible";
        private const string CorrectPin = "1234";
        private const string ThemeKey = "SelectedTheme";
        private readonly ILocalHost localHostService;
        private bool isUserMode = true;
        private readonly ServerHandler serverHandler;

        private readonly IConfiguration configuration;
        private readonly IBrokerHealthService brokerHealthService;
        private readonly string serverHost;
        private readonly string serverUsername;
        private readonly string serverApiKey;
        private readonly string serverPort;
        private PlotModel brokerHealthModel;


        public SettingsView(ILocalHost localHost, IBrokerHealthService brokerHealthService, string defaultView = "ModeButtons")
        {
            InitializeComponent();
            InitializeBrokerHealthChart();
            localHostService = localHost;
            this.brokerHealthService = brokerHealthService;
            serverHandler = new ServerHandler();
            serverHandler.TerminalOutputReceived += UpdateTerminalOutput;

            // Load configuration file path
            string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

            // Display the config file path in a message box
            //MessageBox.Show($"Config file path: {configFilePath}", "Configuration File Path", MessageBoxButton.OK, MessageBoxImage.Information);

            // Load configuration
            configuration = new ConfigurationBuilder()
                .AddJsonFile(configFilePath, optional: true, reloadOnChange: true)
                .Build();

            // Read JSON configuration and display contents for debugging
            try
            {
                var jsonContent = File.ReadAllText(configFilePath);
                //MessageBox.Show($"Config file contents:\n{jsonContent}", "Configuration File Contents", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to read configuration file: {ex.Message}", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Retrieve server configuration values
            serverHost = configuration["Server:Host"];
            serverPort = configuration["Server:Port"];
            serverUsername = configuration["Server:Username"];
            serverApiKey = configuration["Server:ApiKey"];

            // Display loaded configuration values in a message box
            //MessageBox.Show($"Loaded Configuration:\nHost: {serverHost}\nPort: {serverPort}\nUsername: {serverUsername}", "Loaded Server Configuration", MessageBoxButton.OK, MessageBoxImage.Information);

            // Set other properties and initialize UI
            if (Application.Current.Properties[UserModeKey] is bool userModeValue)
            {
                isUserMode = userModeValue;
            }
            else
            {
                isUserMode = true;
            }

            UpdateButtonStates();
            UpdateThemeStates();
            SetDefaultView(defaultView);

            // Initialize and start the timer for updating broker health
            var brokerHealthTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1) // Update every second
            };
            brokerHealthTimer.Tick += UpdateBrokerHealth; // Attach periodic update logic
            brokerHealthTimer.Start(); // Start the timer
        }


        private async void UpdateBrokerHealth(object sender, EventArgs e)
        {
            try
            {
                bool isReachable = await brokerHealthService.IsBrokerReachableAsync();
                //bool isReceiving = await brokerHealthService.IsReceivingDataAsync();

                double timestamp = DateTimeAxis.ToDouble(DateTime.Now);

                var reachableSeries = brokerHealthModel.Series[0] as LineSeries;
                //var receivingSeries = brokerHealthModel.Series[1] as LineSeries;

                if (reachableSeries != null)
                {
                    // Update "Broker Reachable" series
                    reachableSeries.Points.Add(new DataPoint(timestamp, isReachable ? 1 : 0));
                    reachableSeries.Color = isReachable ? OxyColors.Green : OxyColors.Red;

                    // Update "Receiving Data" series
                    //receivingSeries.Points.Add(new DataPoint(timestamp, isReceiving ? 1 : 0));
                    //receivingSeries.Color = isReceiving ? OxyColors.Green : OxyColors.Red;

                    // Trim points to keep chart manageable
                    if (reachableSeries.Points.Count > 50) reachableSeries.Points.RemoveAt(0);
                    //if (receivingSeries.Points.Count > 50) receivingSeries.Points.RemoveAt(0);
                }

                // Refresh the chart
                brokerHealthModel.InvalidatePlot(true);

                // Update status text
                BrokerHealthStatus.Text = $"Status: Broker Reachable - {isReachable}";
            }
            catch (Exception ex)
            {
                // Handle errors gracefully
                BrokerHealthStatus.Text = $"Error: {ex.Message}";
                MessageBox.Show($"An error occurred while updating broker health: {ex.Message}");
            }
        }



        private void SetDefaultView(string defaultView)
        {
            ModeButtonsPanel.Visibility = Visibility.Collapsed;
            LocalHostPanel.Visibility = Visibility.Collapsed;
            ThemeSelectionPanel.Visibility = Visibility.Collapsed;
            InfluxDbModePanel.Visibility = Visibility.Collapsed;
            TerminalPanel.Visibility = Visibility.Collapsed;

            switch (defaultView)
            {
                case "LocalHost":
                    LocalHostPanel.Visibility = Visibility.Visible;
                    break;
                case "ThemeSelection":
                    ThemeSelectionPanel.Visibility = Visibility.Visible;
                    break;
                case "ModeButtons":
                    ModeButtonsPanel.Visibility = Visibility.Visible;
                    break;
                case "InfluxDbModePanel":
                    InfluxDbModePanel.Visibility = Visibility.Visible;
                    break;
                default:
                    ModeButtonsPanel.Visibility = Visibility.Visible;
                    break;
            }
        }

        // Event handler for Panel Button clicks to set the visibility of various panels
        private void PanelButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string panelName)
            {
                SetPanelVisibility(panelName);
            }
        }

        private void SetPanelVisibility(string panelName)
        {
            LocalHostPanel.Visibility = Visibility.Collapsed;
            PinEntryPanel.Visibility = Visibility.Collapsed;
            ThemeSelectionPanel.Visibility = Visibility.Collapsed;
            ModeButtonsPanel.Visibility = Visibility.Collapsed;
            InfluxDbModePanel.Visibility = Visibility.Collapsed;
            TerminalPanel.Visibility = Visibility.Collapsed;
            BrokerHealthPanel.Visibility = Visibility.Collapsed;

            switch (panelName)
            {
                case "ModeButtonsPanel":
                    ModeButtonsPanel.Visibility = Visibility.Visible;
                    break;
                case "ThemeSelectionPanel":
                    ThemeSelectionPanel.Visibility = Visibility.Visible;
                    break;
                case "InfluxDbModePanel":
                    InfluxDbModePanel.Visibility = Visibility.Visible;
                    break;
                case "BrokerHealthPanel":
                    BrokerHealthPanel.Visibility = Visibility.Visible;
                    break;
                default:
                    break;
            }
        }


        private void DockerInitialize(object sender, RoutedEventArgs e)
        {
            localHostService.DockerInitialize();
        }

        private void DockerStart_Click(object sender, RoutedEventArgs e)
        {
            localHostService.StartDocker();
            Task.Delay(2000).ContinueWith(_ =>
            {
                Dispatcher.Invoke(() =>
                {
                    InfluxDbWebView.Source = new Uri("http://localhost:8086");
                    InfluxDbWebView.Visibility = Visibility.Visible;
                    ApiKeyInput.Visibility = Visibility.Visible;
                });
            });
        }

        private void DockerStop_Click(object sender, RoutedEventArgs e)
        {
            localHostService.StopDocker();
            Task.Delay(2000).ContinueWith(_ =>
            {
                Dispatcher.Invoke(() =>
                {
                    InfluxDbWebView.Visibility = Visibility.Collapsed;
                    ApiKeyInput.Visibility = Visibility.Collapsed;
                });
            });
        }

        private void UserMode_Click(object sender, RoutedEventArgs e)
        {
            isUserMode = true;
            Application.Current.Properties[UserModeKey] = isUserMode;
            Application.Current.Properties[DeveloperTabVisibleKey] = false;
            UpdateButtonStates();
            PinEntryPanel.Visibility = Visibility.Collapsed;
            ((MainWindow)Application.Current.MainWindow).UpdateDeveloperTabVisibility();
        }

        private void DeveloperMode_Click(object sender, RoutedEventArgs e)
        {
            PinEntryPanel.Visibility = Visibility.Visible;
        }

        private void PinSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (PinInput.Password == CorrectPin)
            {
                isUserMode = false;
                Application.Current.Properties[UserModeKey] = isUserMode;
                Application.Current.Properties[DeveloperTabVisibleKey] = true;
                UpdateButtonStates();
                PinEntryPanel.Visibility = Visibility.Collapsed;
                ((MainWindow)Application.Current.MainWindow).UpdateDeveloperTabVisibility();
            }
            else
            {
                MessageBox.Show("Incorrect PIN. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                PinInput.Clear();
            }
        }

        private void UpdateButtonStates()
        {
            UserModeStatus.Visibility = isUserMode ? Visibility.Visible : Visibility.Collapsed;
            DeveloperModeStatus.Visibility = !isUserMode ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateThemeStates()
        {
            if (System.Configuration.ConfigurationManager.AppSettings[ThemeKey] == "Resources/ColorsDark.xaml")
            {
                DarkThemeStatus.Visibility = Visibility.Visible;
                LightThemeStatus.Visibility = Visibility.Collapsed;
            }
            else
            {
                DarkThemeStatus.Visibility = Visibility.Collapsed;
                LightThemeStatus.Visibility = Visibility.Visible;
            }
        }

        private void DarkTheme_Click(object sender, RoutedEventArgs e)
        {
            SetTheme("Resources/ColorsDark.xaml");
            ReloadDatabaseConfiguration();
        }

        private void LightTheme_Click(object sender, RoutedEventArgs e)
        {
            SetTheme("Resources/ColorsLight.xaml");
            ReloadDatabaseConfiguration();
        }

        private void SetTheme(string themeUri)
        {
            var app = (App)Application.Current;
            app.ApplyTheme(themeUri);
            RefreshUI();
            UpdateThemeStates();
        }

        private void RefreshUI()
        {
            var settingsViewFactory = ((App)Application.Current).ServiceProvider.GetRequiredService<Func<string, SettingsView>>();
            var settingsView = settingsViewFactory("ThemeSelection");
            ((MainWindow)Application.Current.MainWindow).NavigateTo(settingsView);
        }

        private void LocalButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateUseLocalConfig(true); // Set UseLocal to true for local database
            ReloadDatabaseConfiguration();
            LocalHostPanel.Visibility = Visibility.Visible;
            InfluxDbModePanel.Visibility = Visibility.Collapsed;
            InfluxDbWebView.Visibility = Visibility.Visible;
        }
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            InfluxDbWebView.Reload();
        }

        /// <summary>
        /// Server Operations
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void ServerButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateUseLocalConfig(false); // Set UseLocal to false for server database
            ReloadDatabaseConfiguration();
            TerminalPanel.Visibility = Visibility.Visible;
            string privateKeyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TeamKey.pem");

            // Initialize SSH connection with the complete server URL from configuration
            string serverUrl = serverHost; // Use the host directly from configuration as a full URL
            serverHandler.InitializeSshConnection(serverUrl, serverUsername, privateKeyPath);
            OpenServerWebView(serverUrl);
;
        }



        private void TerminalOutput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                string command = TerminalOutput.Text.Split('\n').Last().Trim();
                if (!string.IsNullOrEmpty(command))
                {
                    serverHandler.ExecuteCommand(command);
                }
                e.Handled = true;
            }
        }

        private void UpdateTerminalOutput(string output)
        {
            Dispatcher.Invoke(() =>
            {
                TerminalOutput.AppendText(output);
                TerminalOutput.ScrollToEnd();
            });
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            serverHandler.DisconnectSsh();
        }

        private void CopyDockerFile_Click(object sender, RoutedEventArgs e)
        {
            serverHandler.Server_CopyDockerFiles();
        }

        private void Server_InitializeImageButton_Click(object sender, RoutedEventArgs e)
        {
            serverHandler.Server_InitializeDockerImage();
        }

        private void Server_StartDockerButton_Click(object sender, RoutedEventArgs e)
        {
            serverHandler.Server_StartDocker();

            // Use serverHost and serverPort for the URL
            string fullUrl = serverHost.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                             ? $"{serverHost}:{serverPort}"
                             : $"http://{serverHost}:{serverPort}";

            OpenServerWebView(fullUrl);
        }

        private void OpenServerWebView(string url)
        {
            try
            {
                // Ensure URL has a valid format, adding "http://" if needed
                if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    url = $"http://{url}";
                }
                ServerInfluxDbWebView.Source = new Uri(url);
                ServerInfluxDbWebView.Visibility = Visibility.Visible;
            }
            catch (UriFormatException ex)
            {
                MessageBox.Show($"Invalid URL format: {url}. Error: {ex.Message}", "URL Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void CloseServerWebView_Click(object sender, RoutedEventArgs e)
        {
            ServerInfluxDbWebView.Visibility = Visibility.Collapsed;
            TerminalPanel.Visibility = Visibility.Visible; // Show the terminal panel again
        }

        private void Server_StopDockerButton_Click(object sender, RoutedEventArgs e)
        {
            serverHandler.Server_StopDocker();
        }

        /// <summary>
        /// API Keys Submission For Local and Server Database
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SubmitApiKey_Click(object sender, RoutedEventArgs e)
        {
            string apiKey = ApiKeyInput.Text;

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                ((LocalHostSetup)localHostService).SaveApiKey(apiKey, isLocal: true); // Save for LocalDatabaseConfiguration
                MessageBox.Show("API Key submitted for Local Database successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Please enter a valid API Key.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ServerSubmitApiKey_Click(object sender, RoutedEventArgs e)
        {
            string apiKey = ServerApiKeyInput.Text;

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                ((LocalHostSetup)localHostService).SaveApiKey(apiKey, isLocal: false); // Save for ServerDatabaseConfiguration
                MessageBox.Show("API Key submitted for Server Database successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Please enter a valid API Key.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }





        // Add this inside SettingsView.xaml.cs, within the SettingsView class
        public void UpdateUseLocalConfig(bool useLocal)
        {
            // Navigate up from the bin directory to the project's root
            string projectDirectory = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 4; i++)  // Adjust if needed to reach the project root
            {
                projectDirectory = Directory.GetParent(projectDirectory)?.FullName;
            }

            // Define the path to the original config.json file
            string configFilePath = Path.Combine(projectDirectory, "Resources", "OutputResources", "config.json");

            if (!File.Exists(configFilePath))
            {
                //MessageBox.Show($"Configuration file not found at: {configFilePath}", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var configJson = JObject.Parse(File.ReadAllText(configFilePath));
                configJson["UseLocal"] = useLocal;

                // Write the updated JSON back to the original config file
                File.WriteAllText(configFilePath, configJson.ToString(Formatting.Indented));

                //MessageBox.Show($"Database configuration updated to {(useLocal ? "Local" : "Server")} in: {configFilePath}", "Configuration Updated", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update configuration: {ex.Message}", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            var app = (App)Application.Current;
            app.ReloadServices();

        }
        private void InitializeBrokerHealthChart()
        {
            var brush = (Brush)Application.Current.Resources["MainViewForegroundColor"];
            var oxyColor = brush.ToOxyColor();

            brokerHealthModel = new PlotModel
            {
                Title = "Broker Health Over Time",
                TitleColor = oxyColor
            };

            // Time Axis
            brokerHealthModel.Axes.Add(new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                StringFormat = "HH:mm:ss",
                Title = "Time",
                IntervalType = DateTimeIntervalType.Seconds,
                MinorIntervalType = DateTimeIntervalType.Seconds,
                IsZoomEnabled = true,
                IsPanEnabled = true,
                TextColor = oxyColor,
                TitleColor = oxyColor
            });

            // Status Axis
            brokerHealthModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Status",
                Minimum = -0.5,
                Maximum = 1.5,
                MajorStep = 1,
                MinorStep = 1,
                IsZoomEnabled = false,
                IsPanEnabled = false,
                LabelFormatter = value => value switch
                {
                    1 => "True",
                    0 => "False",
                    _ => string.Empty,
                },
                TextColor = oxyColor,
                TitleColor = oxyColor
            });

            // Line series for "Broker Reachable"
            var brokerReachableSeries = new LineSeries
            {
                Title = "Broker Reachable",
                MarkerType = MarkerType.Circle,
                Color = OxyColors.Green,
                TextColor = oxyColor,
            };

            // Line series for "Receiving Data"
            var receivingDataSeries = new LineSeries
            {
                Title = "Receiving Data",
                MarkerType = MarkerType.Circle,
                Color = OxyColors.Green,
                TextColor = oxyColor
            };

            brokerHealthModel.Series.Add(brokerReachableSeries);
            brokerHealthModel.Series.Add(receivingDataSeries);

            BrokerHealthPlot.Model = brokerHealthModel;
        }

        private async void CheckBrokerHealth_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Check broker health statuses
                bool isReachable = await brokerHealthService.IsBrokerReachableAsync();
                //bool isReceiving = await brokerHealthService.IsReceivingDataAsync();

                // Add data points to OxyPlot series
                double timestamp = DateTimeAxis.ToDouble(DateTime.Now);

                var reachableSeries = brokerHealthModel.Series[0] as LineSeries;
                var receivingSeries = brokerHealthModel.Series[1] as LineSeries;

                if (reachableSeries != null && receivingSeries != null)
                {
                    reachableSeries.Points.Add(new DataPoint(timestamp, isReachable ? 1 : 0));
                    //receivingSeries.Points.Add(new DataPoint(timestamp, isReceiving ? 1 : 0));

                    // Trim points to the last 50
                    if (reachableSeries.Points.Count > 50) reachableSeries.Points.RemoveAt(0);
                    if (receivingSeries.Points.Count > 50) receivingSeries.Points.RemoveAt(0);
                }

                // Refresh plot
                brokerHealthModel.InvalidatePlot(true);

                // Update status
                BrokerHealthStatus.Text = $"Status: Broker Reachable - {isReachable}";
            }
            catch (Exception ex)
            {
                BrokerHealthStatus.Text = $"Error: {ex.Message}";
                MessageBox.Show($"An error occurred while checking broker health: {ex.Message}");
            }
        }





        public string CheckBrokerHealth()
        {
            // Replace with actual logic to check the broker's health
            // Example: Ping the broker or check a health endpoint
            return "Healthy"; // Or "Unhealthy" based on the response
        }



        private void ReloadDatabaseConfiguration()
        {
            var app = (App)Application.Current;

            // Reconfigure services with the new configuration
            app.ConfigureServices(new ServiceCollection());


            //MessageBox.Show("Database configuration reloaded.", "Reloaded", MessageBoxButton.OK, MessageBoxImage.Information);
        }



    }
}
