using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.Wpf;
using Smart_Pacifier___Tool.Tabs.DeveloperTab;
using SmartPacifier.BackEnd.DatabaseLayer.InfluxDB.Connection;
using SmartPacifier.Interface.Services;

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

        public SettingsView(ILocalHost localHost, string defaultView = "ModeButtons")
        {
            InitializeComponent();
            localHostService = localHost;

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
        }

        private void SetDefaultView(string defaultView)
        {
            ModeButtonsPanel.Visibility = Visibility.Collapsed;
            LocalHostPanel.Visibility = Visibility.Collapsed;
            ThemeSelectionPanel.Visibility = Visibility.Collapsed;
            InfluxDbModePanel.Visibility = Visibility.Collapsed;

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
            }
        }

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
                case "None":
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
                    SubmitApiButton.Visibility = Visibility.Visible;
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
                    SubmitApiButton.Visibility = Visibility.Collapsed;
                });
            });
        }

        private void SubmitApiKey_Click(object sender, RoutedEventArgs e)
        {
            string apiKey = ApiKeyInput.Text;

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                ((LocalHostSetup)localHostService).SaveApiKey(apiKey);
                MessageBox.Show("API Key submitted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Please enter a valid API Key.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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
            if (ConfigurationManager.AppSettings[ThemeKey] is "Resources/ColorsDark.xaml")
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
        }

        private void LightTheme_Click(object sender, RoutedEventArgs e)
        {
            SetTheme("Resources/ColorsLight.xaml");
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

        // Updated event handlers for Local and Server buttons
        private void LocalButton_Click(object sender, RoutedEventArgs e)
        {
            // Show the original Local Host panel
            LocalHostPanel.Visibility = Visibility.Visible;
            InfluxDbModePanel.Visibility = Visibility.Collapsed;
            // Remove the Local Host button from the top completely
            // InfluxDBButton is kept visible
        }

        private void ServerButton_Click(object sender, RoutedEventArgs e)
        {
            // Show a simple message
            MessageBox.Show("Hello World", "Server", MessageBoxButton.OK, MessageBoxImage.Information);
            // Optionally hide the InfluxDB panel if needed
            InfluxDbModePanel.Visibility = Visibility.Collapsed;
        }
    }
}
