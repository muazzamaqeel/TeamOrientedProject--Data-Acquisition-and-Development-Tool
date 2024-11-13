using System.Windows;
using System.Windows.Controls;

namespace Smart_Pacifier___Tool.Components
{
    /// <summary>
    /// Interaction logic for TextBoxWithPlaceholder.xaml
    /// </summary>
    public partial class TextBoxWithPlaceholder : UserControl
    {
        /// <summary>
        /// DependencyProperty for the Text property.
        /// </summary>
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(TextBoxWithPlaceholder), new PropertyMetadata(string.Empty));

        /// <summary>
        /// DependencyProperty for the PlaceholderText property.
        /// </summary>
        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.Register("PlaceholderText", typeof(string), typeof(TextBoxWithPlaceholder), new PropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets the text of the TextBox.
        /// </summary>
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        /// <summary>
        /// Gets or sets the placeholder text of the TextBox.
        /// </summary>
        public string PlaceholderText
        {
            get { return (string)GetValue(PlaceholderTextProperty); }
            set { SetValue(PlaceholderTextProperty, value); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextBoxWithPlaceholder"/> class.
        /// </summary>
        public TextBoxWithPlaceholder()
        {
            InitializeComponent();
        }
    }
}