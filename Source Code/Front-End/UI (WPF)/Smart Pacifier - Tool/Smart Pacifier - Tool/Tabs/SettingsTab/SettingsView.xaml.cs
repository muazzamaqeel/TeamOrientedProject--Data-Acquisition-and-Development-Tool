using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using InfluxDB.Client.Api.Domain;
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

            // Retrieve persisted state when the view is loaded
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

            // Ensure the User Mode and Developer Mode buttons are visible by default
            SetDefaultView(defaultView);
        }

        private void SetDefaultView(string defaultView)
        {
            ModeButtonsPanel.Visibility = Visibility.Collapsed;
            LocalHostPanel.Visibility = Visibility.Collapsed;
            ThemeSelectionPanel.Visibility = Visibility.Collapsed;

            switch (defaultView)
            {
                case "LocalHost":
                    LocalHostPanel.Visibility = Visibility.Visible;
                    break;
                case "ThemeSelection":
                    ThemeSelectionPanel.Visibility = Visibility.Visible;
                    break;
                case "ModeButtons":
                default:
                    ModeButtonsPanel.Visibility = Visibility.Visible;
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

            switch (panelName)
            {
                case "ModeButtonsPanel":
                    ModeButtonsPanel.Visibility = Visibility.Visible;
                    break;
                case "ThemeSelectionPanel":
                    ThemeSelectionPanel.Visibility = Visibility.Visible;
                    break;
                case "LocalHostPanel":
                    LocalHostPanel.Visibility = Visibility.Visible;
                    break;
                case "None":
                default:
                    // No panel to show
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
                // Save the API key using the localHostService instance
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

            // Force the UI to refresh, to apply new theme
            RefreshUI();
            UpdateThemeStates();
        }

        private void RefreshUI()
        {
            var settingsViewFactory = ((App)Application.Current).ServiceProvider.GetRequiredService<Func<string, SettingsView>>();
            var settingsView = settingsViewFactory("ThemeSelection");
            ((MainWindow)Application.Current.MainWindow).NavigateTo(settingsView);

        }
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            InfluxDbWebView.Reload();
        }
    }
}