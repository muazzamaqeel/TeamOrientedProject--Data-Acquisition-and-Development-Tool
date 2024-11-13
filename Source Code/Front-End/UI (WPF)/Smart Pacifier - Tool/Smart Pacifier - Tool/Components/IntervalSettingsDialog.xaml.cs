using Smart_Pacifier___Tool.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;

namespace Smart_Pacifier___Tool.Components
{
    /// <summary>
    /// Interaction logic for IntervalSettingsDialog.xaml
    /// </summary>
    public partial class IntervalSettingsDialog : Window
    {
        /// <summary>
        /// Gets the sensor intervals.
        /// </summary>
        public Dictionary<string, List<int>> SensorIntervals { get; private set; } = new Dictionary<string, List<int>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="IntervalSettingsDialog"/> class.
        /// </summary>
        /// <param name="sensors">The list of sensors.</param>
        public IntervalSettingsDialog(List<SensorItem> sensors)
        {
            InitializeComponent();
            PopulateSensorSettings(sensors);
        }

        /// <summary>
        /// Populates the sensor settings.
        /// </summary>
        /// <param name="sensors">The list of sensors.</param>
        private void PopulateSensorSettings(List<SensorItem> sensors)
        {
            foreach (var sensor in sensors)
            {
                int graphCount = sensor.SensorButtonText == "Sensor 1" ? 9 : sensor.SensorButtonText == "Sensor 2" ? 3 : 5; // Adjust as needed

                var sensorPanel = new StackPanel
                {
                    Margin = new Thickness(5),
                    Background = Brushes.LightGray
                };

                sensorPanel.Children.Add(new TextBlock
                {
                    Text = sensor.SensorButtonText,
                    FontSize = 16,
                    FontWeight = FontWeights.Bold
                });

                for (int i = 0; i < graphCount; i++)
                {
                    var numericUpDown = new TextBox
                    {
                        Width = 50,
                        Margin = new Thickness(5),
                        TextAlignment = TextAlignment.Center
                    };

                    numericUpDown.Tag = $"{sensor.SensorButtonText}_Graph{i + 1}";
                    sensorPanel.Children.Add(numericUpDown);
                }

                SensorSettingsPanel.Children.Add(sensorPanel);
                SensorIntervals[sensor.SensorButtonText] = new List<int>(new int[graphCount]); // Initialize with default values
            }
        }

        /// <summary>
        /// Handles the Save button click event.
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            //foreach (var panel in SensorSettingsPanel.Children)
            //{
            //    if (panel is StackPanel sensorPanel)
            //    {
            //        var sensorName = (sensorPanel.Children[0] as TextBlock).Text;
            //        var intervals = new List<int>();

            //        for (int i = 1; i < sensorPanel.Children.Count; i++)
            //        {
            //            if (sensorPanel.Children[i] is TextBox numericBox && int.TryParse(numericBox.Text, out int interval))
            //            {
            //                intervals.Add(interval);
            //            }
            //        }

            //        if (SensorIntervals.ContainsKey(sensorName))
            //            SensorIntervals[sensorName] = intervals;
            //    }
            //}
            //this.DialogResult = true; // Close dialog and indicate success
        }
    }
}