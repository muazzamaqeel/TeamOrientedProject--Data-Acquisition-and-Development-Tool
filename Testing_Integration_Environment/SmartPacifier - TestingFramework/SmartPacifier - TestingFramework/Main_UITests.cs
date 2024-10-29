using System;
using System.Diagnostics;
using FlaUI.Core;
using FlaUI.UIA3;
using Xunit;
using SmartPacifier_UITests;
using SmartPacifier___TestingFramework.UI_Tests_FrontEnd.UI_Tests_Tabs.UI_Tests_SettingsTab;
using SmartPacifier___TestingFramework.UITests.UI_Tests_FrontEnd.UI_Tests_Tabs.UI_Test_Sidebar;

namespace SmartPacifier___TestingFramework
{
    public class Main_UITests
    {
        private Application? app;

        /// <summary>
        /// This function is used to launch the application for all UI Tests.
        /// </summary>
        /// <returns></returns>
        private Application LaunchApplication()
        {
            string applicationPath = @"C:\programming\TeamOrientedProject---Smart-Pacifier\Source Code\Front-End\UI (WPF)\Smart Pacifier - Tool\Smart Pacifier - Tool\bin\Debug\net8.0-windows\SmartPacifier.UI (WPF).exe";
            var processStartInfo = new ProcessStartInfo(applicationPath)
            {
                WorkingDirectory = @"C:\programming\TeamOrientedProject---Smart-Pacifier\Source Code\Front-End\UI (WPF)\Smart Pacifier - Tool\Smart Pacifier - Tool\bin\Debug\net8.0-windows\"
            };

            app = Application.Launch(processStartInfo);
            return app;
        }


        /// <summary>
        /// Runs the PIN validation tests for the Developer tab.
        /// </summary>
        [Fact]
        public void RunPinValidationTests()
        {
            app = LaunchApplication();
            try
            {

                // Pass the launched application to the PinValidationTests class
                var pinValidationTests = new PINValidationTests(app);
                pinValidationTests.ValidateDeveloperTabActivation_WithCorrectPin();  // Call the function

                var settingsTests = new UITests_Settings(app);

                // Assert each function returns true
                Assert.True(settingsTests.CheckButtonsExistenceInSettingsTab(), "Buttons existence test failed.");
                Assert.True(settingsTests.CheckTextBlocksExistenceAndBehaviorInSettingsTab(), "Text blocks existence and behavior test failed.");
                //Assert.True(settingsTests.CheckCheckBoxesExistenceInSettingsTab(), "Check boxes existence test failed.");
          
            }
            finally
            {
                app.Close();
            }
        }

        /// <summary>
        /// Runs the header matching validation tests for the User Mode
        /// </summary>
        [Fact]
        public void SideBarButtonsHeaderMatchValidationTests()
        {
            app = LaunchApplication();
            try
            {
                // Pass the launched FlaUI Application to the HeaderCheck class
                var userHeaderTests = new HeaderCheck(app);

                // Assert each function returns true for USER MODE
                Assert.True(userHeaderTests.ButtonClick_USERMODEShouldOpenNewActiveMonitoringWindow(), "Active Monitoring button header match test failed.");
                Assert.True(userHeaderTests.ButtonClick_USERMODEShouldOpenNewCampaignsWindow(), "Campaigns button header match test failed.");
                Assert.True(userHeaderTests.ButtonClick_USERMODEShouldOpenNewSettingsWindow(), "Settings button header match test failed.");

                // Assert each function returns true for DEVELOPER MODE
                // Uncomment and implement similar checks for developer mode if needed
                // Assert.True(userHeaderTests.ButtonClick_ShouldOpenNewDEVELOPERWindow(), "Developer Mode Buttons header match test failed.");
            }
            finally
            {
                app.Close();
            }
        }








    }
}
