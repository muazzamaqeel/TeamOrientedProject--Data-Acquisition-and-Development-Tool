using System.Windows;
using System.Windows.Controls;
using Smart_Pacifier___Tool.Tabs.CampaignsTab;
using Smart_Pacifier___Tool.Tabs.MonitoringTab;
using Smart_Pacifier___Tool.Tabs.SettingsTab;
using Smart_Pacifier___Tool.Tabs.DeveloperTab;
using Microsoft.Extensions.DependencyInjection;

namespace Smart_Pacifier___Tool
{
    /// <summary>
    /// Interaction logic for Sidebar.xaml
    /// </summary>
    public partial class Sidebar : UserControl
    {
        private const string DeveloperTabVisibleKey = "DeveloperTabVisible";

        /// <summary>
        /// Initializes a new instance of the <see cref="Sidebar"/> class.
        /// </summary>
        public Sidebar()
        {
            InitializeComponent();
            UpdateDeveloperTabVisibility();
        }

        /// <summary>
        /// Updates the visibility of the Developer Tab based on application properties.
        /// </summary>
        public void UpdateDeveloperTabVisibility()
        {
            // Check if the Developer Tab should be visible
            if (Application.Current.Properties[DeveloperTabVisibleKey] is bool isVisible && isVisible)
            {
                DeveloperButton.Visibility = Visibility.Visible;
            }
            else
            {
                DeveloperButton.Visibility = Visibility.Collapsed;
            }
        }

        private void CampaignsButton_Click(object sender, RoutedEventArgs e)
        {
            var campaignsView = ((App)Application.Current).ServiceProvider.GetRequiredService<CampaignsView>();
            ((MainWindow)Application.Current.MainWindow).NavigateTo(campaignsView);
        }

        private void MonitoringButton_Click(object sender, RoutedEventArgs e)
        {
            // Retrieve PacifierSelectionView from the DI container
            var pacifierSelectionView = ((App)Application.Current).ServiceProvider.GetRequiredService<PacifierSelectionView>();
            ((MainWindow)Application.Current.MainWindow).NavigateTo(pacifierSelectionView);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsViewFactory = ((App)Application.Current).ServiceProvider.GetRequiredService<Func<string, SettingsView>>();
            var settingsView = settingsViewFactory("");
            ((MainWindow)Application.Current.MainWindow).NavigateTo(settingsView);
        }

        private void DeveloperButton_Click(object sender, RoutedEventArgs e)
        {
            // Resolve the DeveloperView from the service provider and navigate to it
            var developerView = ((App)Application.Current).ServiceProvider.GetRequiredService<DeveloperView>();
            ((MainWindow)Application.Current.MainWindow).NavigateTo(developerView);
        }
    }
}