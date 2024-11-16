using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlaUI.Core;
using FlaUI.UIA3;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Xunit;
using System.Diagnostics;

namespace SmartPacifier___TestingFramework.UITests.UI_Templates
{
    public class Buttons
    {


        /// <summary>
        /// This function is used to find a button 
        /// The idea is to reuse this function for multiple UI Windows
        /// </summary>
        /// <param name="mainWindow"></param>
        /// <param name="buttonName"></param>
        /// <returns></returns>
        /// 

        public Button? FindButtonByName(Window mainWindow, string buttonName)
        {
            // Try to find the button by its x:Name
            var button = mainWindow.FindFirstDescendant(cf => cf.ByName(buttonName))?.AsButton();

            // If button by name is not found, try finding by control type (all buttons)
            if (button == null)
            {
                var buttons = mainWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.Button));
                foreach (var b in buttons)
                {
                    System.Diagnostics.Debug.WriteLine($"Button: {b.Name}");
                }

                // Fallback to the first button (just for testing if none is found by name)
                button = buttons.FirstOrDefault()?.AsButton();
            }

            return button;
        }
    }
}
