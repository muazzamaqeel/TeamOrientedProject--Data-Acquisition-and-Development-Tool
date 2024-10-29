using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using System.Linq;
using Xunit;
using System.Threading;

namespace SmartPacifier___TestingFramework.UITests.UI_Tests_FrontEnd.UI_Tests_Tabs.UI_Test_Sidebar
{
    public class HeaderCheck
    {
        public readonly Application app;
        private readonly UIA3Automation automation;

        public HeaderCheck(Application app)
        {
            this.app = app;
            this.automation = new UIA3Automation();
        }

        // General method to check button click and corresponding window and header
        private bool CheckWindowHeader(string buttonName, string expectedHeader)
        {
            Thread.Sleep(3000); // Ensure the UI is loaded properly

            var mainWindow = app.GetMainWindow(automation);
            Assert.NotNull(mainWindow); // Ensure the main window is open

            // Find and click the specified button
            var button = mainWindow.FindFirstDescendant(x => x.ByName(buttonName)).AsButton();
            button.Invoke();

            // Wait for the new window to appear
            var newWindow = WaitForNewWindow(buttonName);
            if (newWindow != null)
            {
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
            }
            else
            {
                Console.WriteLine($"New window for '{buttonName}' not found.");
            }

            return false; // Return false if the new window is not found
        }


        // Method to wait for a new window to appear based on the button name
        private AutomationElement WaitForNewWindow(string buttonName)
        {
            // You might want to implement a timeout to avoid an infinite wait
            for (int i = 0; i < 10; i++) // wait up to 10 seconds
            {
                var allWindows = automation.GetDesktop().FindAllChildren();
                var newWindow = allWindows.FirstOrDefault(w =>
                    w.ControlType == FlaUI.Core.Definitions.ControlType.Window &&
                    w.Properties.Name.Value.Contains(buttonName));
                if (newWindow != null) return newWindow;

                Thread.Sleep(1000); // wait for a second before checking again
            }
            return null; // Return null if no new window appears within the timeout
        }

        // Specific methods for each button that use the general method
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
