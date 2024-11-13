using System.Windows;

namespace Smart_Pacifier___Tool.Components
{
    /// <summary>
    /// Interaction logic for InputDialog.xaml
    /// </summary>
    public partial class InputDialog : Window
    {
        /// <summary>
        /// Gets the input text from the dialog.
        /// </summary>
        public string InputText { get; private set; } = string.Empty;  // Initialize with an empty string

        /// <summary>
        /// Initializes a new instance of the <see cref="InputDialog"/> class.
        /// </summary>
        /// <param name="title">The title of the dialog window.</param>
        public InputDialog(string title)
        {
            InitializeComponent();  // Initialize the XAML components
            this.Title = title;  // Set the window title dynamically
        }

        /// <summary>
        /// Handles the OK button click event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            InputText = InputTextBox.Text.Trim();  // Get the input from the TextBox
            if (!string.IsNullOrWhiteSpace(InputText))  // Ensure it's not empty
            {
                DialogResult = true;  // Close the dialog and signal success
            }
            else
            {
                MessageBox.Show("Please enter a valid name.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);  // Show a warning message
            }
        }

        /// <summary>
        /// Handles the Cancel button click event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;  // Close the dialog and signal cancellation
        }
    }
}