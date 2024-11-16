using FlaUI.Core;
using FlaUI.UIA3;
using Xunit;
using System;
using System.Threading;
using FlaUI.Core.AutomationElements;

namespace SmartPacifier___TestingFramework.UITests.UIFrontEnd
{
    public class PINValidationTests
    {
        private readonly Application app;

        public PINValidationTests(Application app)
        {
            this.app = app;
        }

        public void ValidateDeveloperTabActivation_WithCorrectPin()
        {
            using var automation = new UIA3Automation();
            var mainWindow = app.GetMainWindow(automation);
            Assert.NotNull(mainWindow); // Ensure the main window is accessible

            // Use WaitUntil method for button checks
            var settingsButton = WaitUntil(() => mainWindow.FindFirstDescendant(cf => cf.ByName("Settings"))?.AsButton());
            Assert.NotNull(settingsButton); // Assert the Settings button exists
            settingsButton.Click();
            Thread.Sleep(1000); // Allow UI to update

            var switchModeButton = WaitUntil(() => mainWindow.FindFirstDescendant(cf => cf.ByName("Switch Mode"))?.AsButton());
            Assert.NotNull(switchModeButton); // Assert the Switch Mode button exists
            switchModeButton.Click();
            Thread.Sleep(1000); // Allow UI to update

            var developerModeButton = WaitUntil(() => mainWindow.FindFirstDescendant(cf => cf.ByName("Developer Mode"))?.AsButton());
            Assert.NotNull(developerModeButton); // Assert the Developer Mode button exists
            developerModeButton.Click();
            Thread.Sleep(1000); // Allow UI to update

            var pinEntry = WaitUntil(() => mainWindow.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Edit))?.AsTextBox());
            Assert.NotNull(pinEntry); // Assert the PIN entry box exists
            pinEntry.Enter("1234");
            Thread.Sleep(1000); // Allow UI to update

            var continueButton = WaitUntil(() => mainWindow.FindFirstDescendant(cf => cf.ByName("Continue"))?.AsButton());
            Assert.NotNull(continueButton); // Assert the Continue button exists
            continueButton.Click();
            Thread.Sleep(1000); // Allow UI to update

            var developerTab = WaitUntil(() => mainWindow.FindFirstDescendant(cf => cf.ByName("Developer"))?.AsButton());
            Assert.NotNull(developerTab); // Assert the Developer tab exists
            Assert.True(!developerTab.Properties.IsOffscreen.Value, "Developer tab should be visible after entering the correct PIN.");
        }

        private T WaitUntil<T>(Func<T> condition, TimeSpan? timeout = null) where T : class
        {
            var startTime = DateTime.UtcNow;
            var waitTimeout = timeout ?? TimeSpan.FromSeconds(10); // Default timeout

            while (DateTime.UtcNow - startTime < waitTimeout)
            {
                var result = condition();
                if (result != null) return result;

                Thread.Sleep(100); // Small wait before next check
            }

            return null; // Return null if the condition was never met
        }
    }
}
