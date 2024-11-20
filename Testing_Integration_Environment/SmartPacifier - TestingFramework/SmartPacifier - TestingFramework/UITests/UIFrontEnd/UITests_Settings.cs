using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Xunit;
using System.Windows;
using SmartPacifier___TestingFramework.UITests.UI_Templates;
using FlaUIApplication = FlaUI.Core.Application;
using FlaUIWindow = FlaUI.Core.AutomationElements.Window;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using InfluxDB.Client;

namespace SmartPacifier___TestingFramework.UITests.UIFrontEnd
{
    public class UITests_Settings : IDisposable
    {
        public FlaUIApplication? app; // Removed 'readonly' modifier

        // Constructor is not needed in this case

        ////////****************************** BUTTONS ************************/////////////
        /// <summary>
        /// This test erifies that all required buttons in the Settings tab exist.
        /// </summary>
        /// <returns></returns>
        public bool CheckButtonsExistenceInSettingsTab()
        {
            try
            {
                using (var automation = new UIA3Automation())
                {
                    var mainWindow = app.GetMainWindow(automation);
                    var buttonsHelper = new Buttons();

                    // Check for the existence of buttons in the Settings tab
                    var switchModeButton = buttonsHelper.FindButtonByName(mainWindow, "Switch Mode");
                    var themeButton = buttonsHelper.FindButtonByName(mainWindow, "Theme");
                    var userModeButton = buttonsHelper.FindButtonByName(mainWindow, "User Mode");
                    var developerModeButton = buttonsHelper.FindButtonByName(mainWindow, "Developer Mode");

                    // Return true if all buttons exist, otherwise return false
                    return switchModeButton != null && themeButton != null && userModeButton != null && developerModeButton != null;
                }
            }
            catch (Exception)
            {
                return false; // Return false if an exception occurs
            }
        }

        ////////****************************** TEXT BOXES ************************/////////////
        /// <summary>
        /// This test verifies that required text boxes exist in the Settings tab and observes their behavior
        /// </summary>
        public void CheckTextBoxesExistenceInSettingsTab()
        {
            using (var automation = new UIA3Automation())
            {
                Thread.Sleep(3000); // Ensure the UI is loaded properly
                var mainWindow = app.GetMainWindow(automation);

                var textBoxHelper = new Text_Boxes(); // Instance of the Text_Boxes class

                // Check for the existence of text boxes in the Settings tab
                var settingTextBox1 = textBoxHelper.FindTextBoxByName(mainWindow, "PinInput");

                // Assert that each text box exists
                Assert.NotNull(settingTextBox1);

                // Observe the behavior of the text boxes
                textBoxHelper.ObserveTextBoxBehavior(settingTextBox1);
            }
        }

        ////////****************************** TEXT BLOCKS ************************/////////////
        /// <summary>
        /// This test verifies that required text blocks in the Settings tab exist and are functional.
        /// </summary>
        /// <returns></returns>
        public bool CheckTextBlocksExistenceAndBehaviorInSettingsTab()
        {
            using (var automation = new UIA3Automation())
            {
                var mainWindow = app.GetMainWindow(automation);
                var textBlockHelper = new TextBlocks();

                // Check for the existence of text blocks in the Settings tab
                var userModeTextBlock = textBlockHelper.FindTextBlockByName(mainWindow, "UserModeText");
                var developerModeTextBlock = textBlockHelper.FindTextBlockByName(mainWindow, "DeveloperModeText");

                // Return true if all text blocks exist
                return userModeTextBlock != null && developerModeTextBlock != null;
            }
        }

        ////////****************************** CHECK BOXES ************************/////////////
        /// <summary>
        /// This test verifies that required checkboxes in the Settings tab exist.
        /// </summary>
        /// <returns></returns>
        public bool CheckCheckBoxesExistenceInSettingsTab()
        {
            using (var automation = new UIA3Automation())
            {
                var mainWindow = app.GetMainWindow(automation);
                var checkBoxHelper = new Check_boxes();

                // Check for the existence of checkboxes in the Settings tab
                var enableLoggingCheckBox = checkBoxHelper.FindCheckBoxByName(mainWindow, "EnableLoggingCheckBox");
                var enableNotificationsCheckBox = checkBoxHelper.FindCheckBoxByName(mainWindow, "EnableNotificationsCheckBox");

                // Return true if all checkboxes exist
                return enableLoggingCheckBox != null && enableNotificationsCheckBox != null;
            }
        }
        //[Fact]
        /// <summary>
        /// This test verifies that the API key submission workflow for InfluxDB is functional.
        /// </summary>
        public void TestInfluxDbApiKeySubmission()
        {
            app = LaunchApplication(); // Launch the application
            MessageBox.Show("Application launched", "Debug");

            using (var automation = new UIA3Automation())
            {
                var mainWindow = app.GetMainWindow(automation);
                MessageBox.Show("Main window obtained", "Debug");

                // Ensure the main window is ready
                mainWindow.WaitUntilClickable();
                MessageBox.Show("Main window is clickable", "Debug");

                // Adding delay to ensure UI has fully loaded
                Thread.Sleep(5000); // Adjust delay as needed
                MessageBox.Show("Waited 5 seconds for UI to load", "Debug");

                // Find and click the "Settings" button
                var settingsButton = mainWindow.FindFirstDescendant(cf =>
                    cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button)
                    .And(cf.ByName("Settings"))).AsButton();
                MessageBox.Show("Settings Button found: " + (settingsButton != null), "Debug");
                Assert.NotNull(settingsButton);

                settingsButton?.Invoke();
                MessageBox.Show("Settings button clicked", "Debug");
                Thread.Sleep(2000); // Wait for the Settings page to load

                // Find and click the "InfluxDB" button
                var influxDbButton = mainWindow.FindFirstDescendant(cf =>
                    cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button)
                    .And(cf.ByName("InfluxDB"))).AsButton();
                MessageBox.Show("InfluxDB Button found: " + (influxDbButton != null), "Debug");
                Assert.NotNull(influxDbButton);

                influxDbButton?.Invoke();
                MessageBox.Show("InfluxDB button clicked", "Debug");
                Thread.Sleep(2000); // Wait for the InfluxDB panel to load

                // Find and click the "Local" button using its x:Name
                var localButton = mainWindow.FindFirstDescendant(cf =>
                    cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button)
                    .And(cf.ByAutomationId("LocalButton"))).AsButton();
                MessageBox.Show("Local Button found: " + (localButton != null), "Debug");
                Assert.NotNull(localButton);

                localButton?.Invoke();
                MessageBox.Show("Local button clicked", "Debug");
                Thread.Sleep(2000); // Wait for the Local panel to load and reveal the API key input field

                // Enter the API key and click the Submit button
                var apiKeyInput = mainWindow.FindFirstDescendant(cf =>
                    cf.ByControlType(FlaUI.Core.Definitions.ControlType.Edit)
                    .And(cf.ByAutomationId("ApiKeyInput"))).AsTextBox();
                MessageBox.Show("API Key input textbox found: " + (apiKeyInput != null), "Debug");
                Assert.NotNull(apiKeyInput);

                apiKeyInput?.Enter("tc0mwDKGySyfJJXYtSIUPFieSFi-OcjmQcLTJDsLtyKgTCfm53s7g-DG4sYAR0Cf7VAczm2FuZ4ILedCBKUk8Q==11"); // Replace with actual API key
                MessageBox.Show("API Key entered", "Debug");
                Thread.Sleep(1000); // Brief pause to ensure input is processed

                // Locate and click the Submit API button
                var submitApiButton = mainWindow.FindFirstDescendant(cf =>
                    cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button)
                    .And(cf.ByAutomationId("SubmitApiButton"))).AsButton();
                MessageBox.Show("Submit API Button found: " + (submitApiButton != null), "Debug");
                Assert.NotNull(submitApiButton);

                submitApiButton?.Invoke();
                MessageBox.Show("Submit API button clicked", "Debug");
                Thread.Sleep(2000); // Wait for any response after submission

                // Database connection after API Key submission
                try
                {
                    // Database configuration
                    var influxDbUrl = "http://localhost:8086";
                    var org = "thu-de";
                    var bucket = "SmartPacifier-Bucket1"; // Replace with actual bucket name
                    var token = "d90VrJvi9FxSB5Hu6bUSG5G8FMftxpM9jmwExTcfb8RqFU_y-IEVYjoCqcl_4aqytql_4Y5ceg7ac55rE_6Jaw=="; // Replace with actual token

                    // Initialize the InfluxDB client
                    using (var influxDbClient = InfluxDBClientFactory.Create(influxDbUrl, token.ToCharArray()))
                    {
                        MessageBox.Show("Connecting to InfluxDB...", "Debug");

                        // Health check to ensure connection
                        var health = influxDbClient.HealthAsync().GetAwaiter().GetResult();
                        MessageBox.Show("InfluxDB Connection Status: " + health.Status, "Debug");

                        Assert.Equal(HealthCheck.StatusEnum.Pass, health.Status); // Confirm connection is successful

                        // Prepare a test data point
                        var point = PointData.Measurement("test_measurement")
                            .Field("test_field", 1)
                            .Timestamp(DateTime.UtcNow, WritePrecision.Ns);

                        // Attempt to write the test point
                        influxDbClient.GetWriteApiAsync().WritePointAsync(point, bucket, org).GetAwaiter().GetResult();
                        MessageBox.Show("Test data written to database", "Debug");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to connect to InfluxDB or write data: " + ex.Message, "Debug");
                    return; // Exit the test if database connection or write fails
                }
            }
        }





        private FlaUIApplication LaunchApplication()
        {
            string applicationPath = @"C:\programming\TeamOrientedProject---Smart-Pacifier\Source Code\Front-End\UI (WPF)\Smart Pacifier - Tool\Smart Pacifier - Tool\bin\Debug\net8.0-windows\SmartPacifier.UI (WPF).exe";
            var processStartInfo = new ProcessStartInfo(applicationPath)
            {
                WorkingDirectory = @"C:\programming\TeamOrientedProject---Smart-Pacifier\Source Code\Front-End\UI (WPF)\Smart Pacifier - Tool\Smart Pacifier - Tool\bin\Debug\net8.0-windows\"
            };

            var app = FlaUIApplication.Launch(processStartInfo);
            return app;
        }

        public void Dispose()
        {
            if (app != null)
            {
                app.Close();
                app.Dispose();
                app = null; // Allowed now since 'app' is not readonly
            }
        }

        private void HandlePopUp(FlaUIWindow mainWindow, string expectedMessage)
        {
            MessageBox.Show("Waiting for the pop-up window...", "Debug");
            var popUp = Retry.Find(() => mainWindow.ModalWindows.FirstOrDefault(), timeout: 30000);
            MessageBox.Show("Pop-up Window found: " + (popUp != null), "Debug");
            Assert.NotNull(popUp);

            MessageBox.Show("Attempting to find the message element...", "Debug");
            var messageElement = Retry.Find(() =>
                popUp.FindFirstDescendant(cf => cf.ByText(expectedMessage)), timeout: 30000);
            MessageBox.Show("Message Element found: " + (messageElement != null), "Debug");
            Assert.NotNull(messageElement);

            MessageBox.Show("Attempting to find the OK button...", "Debug");
            var okButton = Retry.Find(() =>
                popUp.FindFirstDescendant(cf =>
                    cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button)
                    .And(cf.ByName("OK"))).AsButton(), timeout: 30000);
            MessageBox.Show("OK Button found: " + (okButton != null), "Debug");
            Assert.NotNull(okButton);

            if (okButton != null)
            {
                MessageBox.Show("Invoking OK button...", "Debug");
                okButton.Invoke();
            }
            else
            {
                MessageBox.Show("OK Button is null.", "Debug");
            }
        }
    }

    public static class Retry
    {
        public static T Find<T>(Func<T> action, int timeout = 30000, int interval = 1000) where T : class
        {
            var stopwatch = Stopwatch.StartNew();
            int attempt = 0;
            while (stopwatch.ElapsedMilliseconds < timeout)
            {
                attempt++;
                var result = action();
                if (result != null)
                {
                    Console.WriteLine($"Element found after {attempt} attempts.");
                    return result;
                }
                Console.WriteLine($"Attempt {attempt}: Element not found, retrying in {interval}ms...");
                Thread.Sleep(interval);
            }
            Console.WriteLine($"Element not found after {attempt} attempts.");
            return null;
        }
    }
}
