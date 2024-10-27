using System.Windows;
using System.Windows.Controls;
using SmartPacifier.Interface.Services;

namespace Smart_Pacifier___Tool.Tabs.SettingsTab
{
    public partial class SettingsView : UserControl
    {
        private const string UserModeKey = "UserMode";
        private const string DeveloperTabVisibleKey = "DeveloperTabVisible";
        private const string CorrectPin = "1234"; // Replace this with the actual PIN
        private readonly ILocalHost localHostService;

        public SettingsView(ILocalHost localHost)
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
                isUserMode = true;  // Default to User Mode if no state was saved
            }

            UpdateButtonStates();
        }

        private bool isUserMode = true;

        private void SwitchMode_Click(object sender, RoutedEventArgs e)
        {
            LocalHostPanel.Visibility = Visibility.Collapsed;
            PinEntryPanel.Visibility = Visibility.Collapsed;
            ModeButtonsPanel.Visibility = Visibility.Visible;
        }

        private void Theme_Click(object sender, RoutedEventArgs e)
        {
            ModeButtonsPanel.Visibility = Visibility.Collapsed;
            LocalHostPanel.Visibility = Visibility.Collapsed;
        }

        private void LocalHost_Click(object sender, RoutedEventArgs e)
        {
            ModeButtonsPanel.Visibility = Visibility.Collapsed;
            LocalHostPanel.Visibility = Visibility.Visible;
        }

        private void DockerStart_Click(object sender, RoutedEventArgs e)
        {
            // Start Docker functionality
            localHostService.StartDocker();
        }

        private void DockerStop_Click(object sender, RoutedEventArgs e)
        {
            // Stop Docker functionality
            localHostService.StopDocker();
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
            if (isUserMode)
            {
                UserModeStatus.Visibility = Visibility.Visible;
                DeveloperModeStatus.Visibility = Visibility.Collapsed;
            }
            else
            {
                UserModeStatus.Visibility = Visibility.Collapsed;
                DeveloperModeStatus.Visibility = Visibility.Visible;
            }
        }
    }
}
