using FlaUI.Core;
using FlaUI.UIA3;
using Xunit;
using SmartPacifier___TestingFramework.UITests.UI_Templates;//Importing the Text Boxes class

namespace SmartPacifier___TestingFramework.UI_Tests_FrontEnd.UI_Tests_Tabs.UI_Tests_SettingsTab
{
    public class UITests_Settings
    {
        public readonly Application app;

        // Constructor to pass the launched application
        public UITests_Settings(Application app)
        {
            this.app = app;
        }

        ////////******************************BUTTONS************************/////////////
        public bool CheckButtonsExistenceInSettingsTab()
        {
            try
            {
                using (var automation = new UIA3Automation())
                {
                    var mainWindow = app.GetMainWindow(automation);
                    var buttonsHelper = new Buttons();

                    // Check for the existence of buttons in the Settings tab
                    var switchModeButton = buttonsHelper.FindButtonByName(mainWindow, "SwitchModeButton");
                    var themeButton = buttonsHelper.FindButtonByName(mainWindow, "ThemeButton");
                    var userModeButton = buttonsHelper.FindButtonByName(mainWindow, "UserModeButton");
                    var developerModeButton = buttonsHelper.FindButtonByName(mainWindow, "DeveloperModeButton");

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
        public void CheckTextBoxesExistenceInSettingsTab()
        {
            try
            {
                using (var automation = new UIA3Automation())
                {
                    Thread.Sleep(3000); // Ensure the UI is loaded properly
                    var mainWindow = app.GetMainWindow(automation);

                    var textBoxHelper = new Text_Boxes(); // Instance of the Text_Boxes class

                    // Check for the existence of text boxes in the Settings tab
                    var settingTextBox1 = textBoxHelper.FindTextBoxByName(mainWindow, "DeveloperModeText");

                    // Assert that each text box exists
                    Assert.NotNull(settingTextBox1);

                    // Observe the behavior of the text boxes
                    textBoxHelper.ObserveTextBoxBehavior(settingTextBox1);
                }
            }
            finally
            {
                // Note: Don't close the app here; it will be closed by Main_TestSuite
            }
        }
        ////////****************************** TEXT BLOCKS ************************/////////////
        public bool CheckTextBlocksExistenceAndBehaviorInSettingsTab()
        {
            try
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
            catch (Exception)
            {
                return false; // Return false if an exception occurs
            }
        }

        ////////****************************** CHECK BOXES ************************/////////////
        public bool CheckCheckBoxesExistenceInSettingsTab()
        {
            try
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
            catch (Exception)
            {
                return false; // Return false if an exception occurs
            }
        }





    }
}
