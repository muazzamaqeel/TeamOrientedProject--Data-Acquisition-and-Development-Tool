using System.Windows;
using System.Windows.Controls;

namespace Smart_Pacifier___Tool.Tabs.SettingsTab
{
    public partial class SettingsView : UserControl
    {
        private const string UserModeKey = "UserMode";
        private const string DeveloperTabVisibleKey = "DeveloperTabVisible";
        private const string CorrectPin = "1234"; // Replace this with the actual PIN

        public SettingsView()
        {
            InitializeComponent();

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

            // Ensure the User Mode and Developer Mode buttons are visible by default
            ModeButtonsPanel.Visibility = Visibility.Visible;
        }

        private bool isUserMode = true;

        private void SwitchMode_Click(object sender, RoutedEventArgs e)
        {
            // Show User Mode and Developer Mode buttons
            ModeButtonsPanel.Visibility = Visibility.Visible;
            PinEntryPanel.Visibility = Visibility.Collapsed; // Ensure PIN entry is hidden
        }

        private void Theme_Click(object sender, RoutedEventArgs e)
        {
            // Hide User Mode and Developer Mode buttons when Theme is pressed
            ModeButtonsPanel.Visibility = Visibility.Collapsed;
        }

        private void UserMode_Click(object sender, RoutedEventArgs e)
        {
            // Switch to User Mode and hide the developer tab
            isUserMode = true;
            Application.Current.Properties[UserModeKey] = isUserMode;
            Application.Current.Properties[DeveloperTabVisibleKey] = false;
            UpdateButtonStates();

            // Hide the PIN entry panel
            PinEntryPanel.Visibility = Visibility.Collapsed;

            // Force the Sidebar to update its visibility
            ((MainWindow)Application.Current.MainWindow).UpdateDeveloperTabVisibility();
        }

        private void DeveloperMode_Click(object sender, RoutedEventArgs e)
        {
            // Display the PIN entry field when switching to Developer Mode
            PinEntryPanel.Visibility = Visibility.Visible;
        }

        private void PinSubmit_Click(object sender, RoutedEventArgs e)
        {
            // Verify the entered PIN
            if (PinInput.Password == CorrectPin)
            {
                isUserMode = false;
                Application.Current.Properties[UserModeKey] = isUserMode;
                Application.Current.Properties[DeveloperTabVisibleKey] = true; // Set Developer mode visible
                UpdateButtonStates();
                PinEntryPanel.Visibility = Visibility.Collapsed;

                // Optional: Force the Sidebar to update its visibility
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