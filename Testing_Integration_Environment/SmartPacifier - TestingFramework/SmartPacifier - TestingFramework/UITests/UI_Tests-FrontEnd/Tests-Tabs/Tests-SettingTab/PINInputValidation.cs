using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Xunit;

namespace SmartPacifier_UITests
{
    public class PINValidationTests
    {
        private readonly Application app;

        // Constructor to accept the launched application
        public PINValidationTests(Application app)
        {
            this.app = app;
        }

        /// <summary>
        /// Validates the Developer tab activation by entering the correct PIN.
        /// </summary>
        public void ValidateDeveloperTabActivation_WithCorrectPin()
        {
            using var automation = new UIA3Automation();
            var mainWindow = app.GetMainWindow(automation);

            // Find and click on the "Settings" button in the sidebar
            var settingsButton = mainWindow.FindFirstDescendant(cf => cf.ByText("Settings")).AsButton();
            Assert.NotNull(settingsButton);
            settingsButton.Click();

            // Find and click the "Switch Mode" button
            var switchModeButton = mainWindow.FindFirstDescendant(cf => cf.ByText("Switch Mode")).AsButton();
            Assert.NotNull(switchModeButton);
            switchModeButton.Click();

            // Find and click the "Developer Mode" button
            var developerModeButton = mainWindow.FindFirstDescendant(cf => cf.ByText("Developer Mode")).AsButton();
            Assert.NotNull(developerModeButton);
            developerModeButton.Click();

            // Find the PIN entry text box and enter the correct PIN "1234"
            var pinEntry = mainWindow.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Edit)).AsTextBox();
            Assert.NotNull(pinEntry);
            pinEntry.Enter("1234");

            // Find and click the "Continue" button
            var continueButton = mainWindow.FindFirstDescendant(cf => cf.ByText("Continue")).AsButton();
            Assert.NotNull(continueButton);
            continueButton.Click();

            // Check if the "Developer" tab is now visible in the sidebar
            var developerTab = mainWindow.FindFirstDescendant(cf => cf.ByText("Developer")).AsButton();
            Assert.NotNull(developerTab);

            // Use IsOffscreen to check if the Developer tab is visible
            bool isDeveloperTabVisible = !developerTab.Properties.IsOffscreen.Value;
            Assert.True(isDeveloperTabVisible, "Developer tab should be visible after entering the correct PIN.");
        }
    }
}
