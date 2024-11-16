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

        public void RunPinValidationTests()
        {
            app = LaunchApplication();
            try
            {
                var pinValidationTests = new PINValidationTests(app);
                pinValidationTests.ValidateDeveloperTabActivation_WithCorrectPin();  // Call the function

                //var settingsTests = new UITests_Settings(app);
                //Assert.True(settingsTests.CheckButtonsExistenceInSettingsTab(), "Buttons existence test failed.");
                //Assert.True(settingsTests.CheckTextBlocksExistenceAndBehaviorInSettingsTab(), "Text blocks existence and behavior test failed.");
                //Assert.True(settingsTests.CheckCheckBoxesExistenceInSettingsTab(), "Check boxes existence test failed.");
            }
            finally
            {
                app.Close();
            }
        }

        //[Fact]
        public void SideBarButtonsHeaderMatchValidationTests()
        {
            app = LaunchApplication();
            try
            {
                // Pass the launched FlaUI Application to the HeaderCheck class
                var userHeaderTests = new HeaderCheck(app);

                // Check and assert each function for USER MODE
                try
                {
                    Assert.True(userHeaderTests.ButtonClick_USERMODEShouldOpenNewActiveMonitoringWindow(), "Active Monitoring button header match test failed.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during Active Monitoring test: {ex.Message}");
                }

                try
                {
                    Assert.True(userHeaderTests.ButtonClick_USERMODEShouldOpenNewCampaignsWindow(), "Campaigns button header match test failed.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during Campaigns test: {ex.Message}");
                }

                try
                {
                    Assert.True(userHeaderTests.ButtonClick_USERMODEShouldOpenNewSettingsWindow(), "Settings button header match test failed.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during Settings test: {ex.Message}");
                }

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
