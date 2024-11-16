using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using System;
using System.Linq;
using System.Threading;
using Xunit;

namespace SmartPacifier___TestingFramework.UITests.UIFrontEnd
{
    public class HeaderCheck
    {
        public readonly Application app;
        private readonly UIA3Automation automation;

        public HeaderCheck(Application app)
        {
            this.app = app;
            automation = new UIA3Automation();
        }

        private bool CheckWindowHeader(string buttonName, string expectedHeader)
        {
            var mainWindow = app.GetMainWindow(automation);

            // Debugging output
            if (mainWindow == null)
            {
                Console.WriteLine("Main window is null. The application might not be launched properly.");
                return false; // Return false if main window is null
            }

            Assert.NotNull(mainWindow); // Ensure the main window is open

            // Find and click the specified button
            var button = mainWindow.FindFirstDescendant(x => x.ByName(buttonName));
            if (button == null)
            {
                Console.WriteLine($"Button '{buttonName}' not found.");
                return false; // Handle accordingly
            }

            // Click the button
            button.AsButton().Invoke(); // Make sure to cast to Button

            // Wait for the new window to appear
            var newWindow = WaitForNewWindow(buttonName);
            if (newWindow == null)
            {
                Console.WriteLine($"New window for '{buttonName}' did not appear.");
                return false; // Return false if the new window is not found
            }

            // Find the header element in the new window
            var headerElement = newWindow.FindFirstDescendant(x => x.ByControlType(FlaUI.Core.Definitions.ControlType.Header));
            if (headerElement != null)
            {
                var actualHeader = headerElement.Properties.Name.Value.Trim();
                Console.WriteLine($"New Window Title: '{newWindow.Properties.Name.Value}'");
                Console.WriteLine($"Actual Header: '{actualHeader}', Expected Header: '{expectedHeader.Trim()}'");
                return actualHeader == expectedHeader.Trim(); // Trim both for comparison
            }
            else
            {
                Console.WriteLine("Header element not found.");
            }

            return false; // Return false if the header element is not found
        }

        private AutomationElement WaitForNewWindow(string buttonName, int timeoutInSeconds = 10)
        {
            var endTime = DateTime.Now.AddSeconds(timeoutInSeconds);
            while (DateTime.Now < endTime)
            {
                var allWindows = automation.GetDesktop().FindAllChildren();
                var newWindow = allWindows.FirstOrDefault(w =>
                    w.ControlType == FlaUI.Core.Definitions.ControlType.Window &&
                    w.Properties.Name.Value.Contains(buttonName));
                if (newWindow != null) return newWindow;

                Thread.Sleep(1000); // Wait for a second before checking again
            }

            // If we reach here, the new window did not appear
            return null;
        }

        public bool ButtonClick_USERMODEShouldOpenNewActiveMonitoringWindow()
        {
            return CheckWindowHeader("Active Monitoring", "Active Monitoring");
        }

        public bool ButtonClick_USERMODEShouldOpenNewSettingsWindow()
        {
            return CheckWindowHeader("Settings", "Settings");
        }

        public bool ButtonClick_USERMODEShouldOpenNewCampaignsWindow()
        {
            return CheckWindowHeader("Campaigns", "Select a Campaign");
        }
    }
}
