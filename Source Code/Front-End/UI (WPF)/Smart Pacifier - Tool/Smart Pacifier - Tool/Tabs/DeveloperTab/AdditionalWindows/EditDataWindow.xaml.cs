using System.Collections.Generic;
using System.Windows;
using SmartPacifier.Interface.Services;

namespace Smart_Pacifier___Tool.Tabs.DeveloperTab
{
    public partial class EditDataWindow : Window
    {
        private readonly IManagerCampaign _managerCampaign;
        private readonly IManagerPacifiers _managerPacifiers;

        // Properties to store the edited data
        public string Timestamp { get; private set; } = string.Empty;
        public string Pacifier { get; private set; } = string.Empty;
        public string Campaign { get; private set; } = string.Empty;
        public string Sensor { get; private set; } = string.Empty;
        public double Value { get; private set; }

        private readonly string _originalCampaignName;

        // Constructor that initializes the EditDataWindow with existing data
        public EditDataWindow(IManagerCampaign managerCampaign, IManagerPacifiers managerPacifiers)
        {
            InitializeComponent();

            _managerCampaign = managerCampaign;
            _managerPacifiers = managerPacifiers;
            /*
            // Store original campaign name to check if it was changed
            _originalCampaignName = data.Campaign;

            // Pre-fill with existing data
            TimestampTextBox.Text = data.Timestamp;
            PacifierTextBox.Text = data.Pacifier;
            CampaignTextBox.Text = data.Campaign;
            SensorTextBox.Text = data.Sensor;
            ValueTextBox.Text = data.Value.ToString();
        
            */
            }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Input validation
            if (string.IsNullOrEmpty(TimestampTextBox.Text) || string.IsNullOrEmpty(PacifierTextBox.Text)
                || string.IsNullOrEmpty(CampaignTextBox.Text) || string.IsNullOrEmpty(SensorTextBox.Text)
                || !double.TryParse(ValueTextBox.Text, out double value))
                {
                    ErrorMessage.Text = "Please fill all fields correctly.";
                    ErrorMessage.Visibility = Visibility.Visible;
                    return;
                }

            // Save the updated values
            Timestamp = TimestampTextBox.Text;
            Pacifier = PacifierTextBox.Text;
            Campaign = CampaignTextBox.Text;
            Sensor = SensorTextBox.Text;
            Value = value;

            // Check if the campaign name has been changed
            if (_originalCampaignName != Campaign)
                {
                    await _managerCampaign.UpdateCampaignAsync(_originalCampaignName, Campaign);
                    var tags = new Dictionary<string, string>
                {
                    { "campaign_name", Campaign },
                    { "pacifier_id", Pacifier },
                    { "sensor_type", Sensor }
                };
                        var fields = new Dictionary<string, object>
                {
                    { "timestamp", Timestamp },
                    { "value", Value }
                };
                    await _managerPacifiers.WriteDataAsync("pacifiers", fields, tags);
                }

            // Set DialogResult only if the window is shown as a dialog
            if (this.IsActive && this.IsVisible)
                {
                    this.DialogResult = true;
                }
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.IsActive && this.IsVisible)
            {
                this.DialogResult = false;
            }
            Close();
        }

    }
}
