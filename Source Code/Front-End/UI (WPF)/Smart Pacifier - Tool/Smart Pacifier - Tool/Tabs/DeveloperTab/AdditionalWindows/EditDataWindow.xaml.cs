using System.Windows;

namespace Smart_Pacifier___Tool.Tabs.DeveloperTab
{
    public partial class EditDataWindow : Window
    {
        public string Timestamp { get; private set; } = string.Empty;  // Initialize with an empty string
        public string Pacifier { get; private set; } = string.Empty;
        public string Campaign { get; private set; } = string.Empty;
        public string Sensor { get; private set; } = string.Empty;
        public double Value { get; private set; }

        public EditDataWindow(SensorData data)
        {
            InitializeComponent();

            // Pre-fill with existing data
            TimestampTextBox.Text = data.Timestamp;
            PacifierTextBox.Text = data.Pacifier;
            CampaignTextBox.Text = data.Campaign;
            SensorTextBox.Text = data.Sensor;
            ValueTextBox.Text = data.Value.ToString();
        }


        private void SaveButton_Click(object sender, RoutedEventArgs e)
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

            // Close the dialog with OK result
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
