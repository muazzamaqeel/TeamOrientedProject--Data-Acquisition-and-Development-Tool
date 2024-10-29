using System.Windows;

namespace Smart_Pacifier___Tool.Tabs.DeveloperTab
{
    public partial class AddDataWindow : Window
    {
        public string Timestamp { get; private set; } = string.Empty;  // Provide a default value
        public string Pacifier { get; private set; } = string.Empty;
        public string Campaign { get; private set; } = string.Empty;
        public string Sensor { get; private set; } = string.Empty;
        public double Value { get; private set; }

        public AddDataWindow()
        {
            InitializeComponent();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Input validation (example, can be more advanced)
            if (string.IsNullOrEmpty(TimestampTextBox.Text) || string.IsNullOrEmpty(PacifierTextBox.Text)
                || string.IsNullOrEmpty(CampaignTextBox.Text) || string.IsNullOrEmpty(SensorTextBox.Text)
                || !double.TryParse(ValueTextBox.Text, out double value))
            {
                ErrorMessage.Text = "Please fill all fields correctly.";
                ErrorMessage.Visibility = Visibility.Visible;
                return;
            }

            // Store the entered values
            Timestamp = TimestampTextBox.Text;
            Pacifier = PacifierTextBox.Text;
            Campaign = CampaignTextBox.Text;
            Sensor = SensorTextBox.Text;
            Value = value;

            // Close dialog with OK result
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
