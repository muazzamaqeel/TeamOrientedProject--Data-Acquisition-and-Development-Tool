using System.Collections.ObjectModel;
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
        public int InputText { get; set; }  // Initialize with an empty string

        public ObservableCollection<PacifierItem> PacifierItems { get; set; }
        public PacifierItem pacifierItem { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InputDialog"/> class.
        /// </summary>
        /// <param name="title">The title of the dialog window.</param>
        public InputDialog(ObservableCollection<PacifierItem> pacifierItems)
        {
            InitializeComponent();  // Initialize the XAML components
            PacifierItems = pacifierItems;
            pacifierItem = pacifierItems.FirstOrDefault(); 
            UpdateWindow();
        }

        private void UpdateWindow()
        {
            if (pacifierItem != null)
            {
                InputTextBox.Text = pacifierItem.UpdateFrequency.ToString();
            }
        }

        /// <summary>
        /// Handles the Save button click event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            InputText = int.Parse(InputTextBox.Text);  // Get the input from the TextBox
            foreach(var pacifierItem in PacifierItems)
            {
                pacifierItem.UpdateFrequency = InputText;
            }
            DialogResult = true;  // Close the dialog and signal success
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