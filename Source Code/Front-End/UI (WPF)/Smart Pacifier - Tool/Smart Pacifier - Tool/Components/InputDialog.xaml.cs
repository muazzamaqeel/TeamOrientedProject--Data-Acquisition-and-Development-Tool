using System.Windows;

namespace Smart_Pacifier___Tool.Components
{
    public partial class InputDialog : Window
    {
        public string InputText { get; private set; } = string.Empty;  // Initialize with an empty string

        public InputDialog(string title)
        {
            InitializeComponent();
            this.Title = title;  // Set the window title dynamically
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            InputText = InputTextBox.Text.Trim();  // Get the input from the TextBox
            if (!string.IsNullOrWhiteSpace(InputText))  // Ensure it's not empty
            {
                DialogResult = true;  // Close the dialog and signal success
            }
            else
            {
                MessageBox.Show("Please enter a valid name.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;  // Close the dialog and signal cancellation
        }
    }
}
