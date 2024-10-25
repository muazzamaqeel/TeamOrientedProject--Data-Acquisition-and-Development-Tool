using System;
using FlaUI.Core;
using FlaUI.UIA3;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Xunit;
using System.Diagnostics;

namespace SmartPacifier___TestingFramework.UITests.UI_Templates
{
    public class Text_Boxes
    {
        /// <summary>
        /// This function is used to find a text box by its name or, if not found, fallback to any available text box.
        /// The idea is to reuse this function for multiple UI Windows and perform common checks.
        /// </summary>
        /// <param name="mainWindow"></param>
        /// <param name="textBoxName"></param>
        /// <returns></returns>
        public TextBox? FindTextBoxByName(Window mainWindow, string textBoxName)
        {
            // Try to find the text box by its x:Name
            var textBox = mainWindow.FindFirstDescendant(cf => cf.ByName(textBoxName))?.AsTextBox();

            // If text box by name is not found, try finding by control type (all text boxes)
            if (textBox == null)
            {
                var textBoxes = mainWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.Edit));
                foreach (var tb in textBoxes)
                {
                    System.Diagnostics.Debug.WriteLine($"TextBox: {tb.Name}");
                }

                // Fallback to the first text box (just for testing if none is found by name)
                textBox = textBoxes.FirstOrDefault()?.AsTextBox();
            }

            return textBox;
        }

        /// <summary>
        /// This function is used to observe the behaviour of a text box in the UI.
        /// It can be reused to ensure that text boxes follow a unified design.
        /// </summary>
        /// <param name="textBox"></param>
        public void ObserveTextBoxBehavior(TextBox textBox)
        {
            // Assert that the text box is enabled (Unified Design Behavior)
            Assert.True(textBox.IsEnabled);

            // To check visibility, you can use the BoundingRectangle property
            var boundingRect = textBox.BoundingRectangle;

            // Assert that the text box has a valid size (not collapsed or hidden)
            Assert.False(boundingRect.IsEmpty);

            // Optional: Print text box details to debug output
            System.Diagnostics.Debug.WriteLine($"TextBox: {textBox.Name}, Text: {textBox.Text}");

            // You can also enter text to ensure it's interactive
            textBox.Enter("Test Input");
            Assert.Equal("Test Input", textBox.Text);
        }
    }
}
