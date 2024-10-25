using System;
using System.Diagnostics;
using FlaUI.Core;
using FlaUI.UIA3;
using Xunit;
using SmartPacifier___TestingFramework.UI_Tests_FrontEnd.UI_Tests_Tabs.UI_Tests_SettingsTab;

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
        /// Manually run the CheckButtonsExistenceInSettingsTab test from the UITests_Settings class.
        /// </summary>
        [Fact]
        public void RunSettingsTabTests()
        {
            app = LaunchApplication();
            try
            {
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


    }
}
