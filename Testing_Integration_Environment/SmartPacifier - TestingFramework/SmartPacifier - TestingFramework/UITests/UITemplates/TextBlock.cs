using System;
using FlaUI.Core;
using FlaUI.UIA3;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Xunit;
using System.Diagnostics;

namespace SmartPacifier___TestingFramework.UITests.UI_Templates
{
    public class TextBlocks
    {
        /// <summary>
        /// This function is used to find a text block by its name or, if not found, fallback to any available text block.
        /// The idea is to reuse this function for multiple UI Windows and perform common checks.
        /// </summary>
        /// <param name="mainWindow"></param>
        /// <param name="textBlockName"></param>
        /// <returns></returns>
        public Label? FindTextBlockByName(Window mainWindow, string textBlockName)
        {
            // Try to find the text block by its x:Name
            var textBlock = mainWindow.FindFirstDescendant(cf => cf.ByName(textBlockName))?.AsLabel();

            // If text block by name is not found, try finding by control type (all text blocks)
            if (textBlock == null)
            {
                var textBlocks = mainWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.Text));
                foreach (var tb in textBlocks)
                {
                    System.Diagnostics.Debug.WriteLine($"TextBlock: {tb.Name}");
                }

                // Fallback to the first text block (just for testing if none is found by name)
                textBlock = textBlocks.FirstOrDefault()?.AsLabel();
            }

            return textBlock;
        }

        /// <summary>
        /// This function is used to observe the behaviour of a text block in the UI.
        /// It can be reused to ensure that text blocks follow a unified design.
        /// </summary>
        /// <param name="textBlock"></param>
        public void ObserveTextBlockBehavior(Label textBlock)
        {
            // Assert that the text block is enabled (Unified Design Behavior)
            Assert.True(textBlock.IsEnabled);

            // To check visibility, you can use the BoundingRectangle property
            var boundingRect = textBlock.BoundingRectangle;

            // Assert that the text block has a valid size (not collapsed or hidden)
            Assert.False(boundingRect.IsEmpty);

            // Optional: Print text block details to debug output
            System.Diagnostics.Debug.WriteLine($"TextBlock: {textBlock.Name}, Text: {textBlock.Text}");

            // Check if the text block has content
            Assert.False(string.IsNullOrEmpty(textBlock.Text));
        }
    }
}
