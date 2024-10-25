using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlaUI.Core;
using FlaUI.UIA3;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Xunit;
using System.Diagnostics;

namespace SmartPacifier___TestingFramework.UITests.UI_Templates
{
    public class Check_boxes
    {
        /// <summary>
        /// This function is used to find a checkbox by its name or, if not found, fallback to any available checkbox.
        /// The idea is to reuse this function for multiple UI Windows and perform common checks.
        /// </summary>
        /// <param name="mainWindow"></param>
        /// <param name="checkBoxName"></param>
        /// <returns></returns>
        public CheckBox? FindCheckBoxByName(Window mainWindow, string checkBoxName)
        {
            // Try to find the check box by its x:Name
            var checkBox = mainWindow.FindFirstDescendant(cf => cf.ByName(checkBoxName))?.AsCheckBox();

            // If check box by name is not found, try finding by control type (all checkboxes)
            if (checkBox == null)
            {
                var checkBoxes = mainWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.CheckBox));
                foreach (var cb in checkBoxes)
                {
                    System.Diagnostics.Debug.WriteLine($"CheckBox: {cb.Name}");
                }

                // Fallback to the first checkbox (just for testing if none is found by name)
                checkBox = checkBoxes.FirstOrDefault()?.AsCheckBox();
            }
            return checkBox;
        }

        /// <summary>
        /// This function is used to observe the behaviour of a checkbox in the UI.
        /// It can be reused to ensure that checkboxes follow a unified design.
        /// </summary>
        /// <param name="checkBox"></param>
        public void ObserveCheckBoxBehavior(CheckBox checkBox)
        {
            // Assert that the check box is enabled (Unified Design Behavior)
            Assert.True(checkBox.IsEnabled);

            // To check visibility, you can use the BoundingRectangle property
            var boundingRect = checkBox.BoundingRectangle;

            // Assert that the check box has a valid size (not collapsed or hidden)
            Assert.False(boundingRect.IsEmpty);

            // Optional: Print check box details to debug output
            System.Diagnostics.Debug.WriteLine($"CheckBox: {checkBox.Name}, IsChecked: {checkBox.IsChecked}");

            // Toggle the checkbox state to test interaction
            bool initialState = checkBox.IsChecked ?? false; // null-safe check for IsChecked

            // Check the checkbox
            checkBox.IsChecked = true;
            Assert.True(checkBox.IsChecked);

            // Uncheck the checkbox
            checkBox.IsChecked = false;
            Assert.False(checkBox.IsChecked);

            // Restore to the initial state
            checkBox.IsChecked = initialState;
        }
    }
}