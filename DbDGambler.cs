using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Chrome;
using System;

namespace BWPlayerTwitchMagic
{
    public class DbDGambler
    {
        private IWebDriver _driver;

/*        public DbDGambler()
        {
            var options = new FirefoxOptions();
            options.AddArgument("--start-maximized"); // Start browser maximized
            options.AddArgument("--disable-infobars");
            options.AddArgument("--disable-extensions");
            options.AddArgument("--disable-popup-blocking");
            options.AddArgument("--disable-notifications");

            _driver = new FirefoxDriver(options);

            _driver.Manage().Window.Size = new System.Drawing.Size(800, 300);
        }
        //FireFox
*/ 
        public DbDGambler()
        {
            var options = new ChromeOptions();
            options.AddArgument("--start-maximized"); // Start browser maximized
            options.AddArgument("--disable-infobars");
            options.AddArgument("--disable-extensions");
            options.AddArgument("--disable-popup-blocking");
            options.AddArgument("--disable-notifications");

            _driver = new ChromeDriver(options);

            _driver.Manage().Window.Size = new System.Drawing.Size(800, 300);
        }
        public void OpenPerkSelector()
        {
            _driver.Navigate().GoToUrl("https://dpsm.3stadt.com/survivor?sids=0,1,3,72,83,4,65,5,107,68,6,7,8,9,81,11,66,128,95,99,12,13,14,133,16,17,80,18,20,21,98,143,144,141,69,92,114,22,23,108,131,104,70,24,25,26,27,28,29,130,64,145,30,31,32,33,35,113,37,38,39,115,111,40,41,123,67,42,96,148,43,44,77,140,47,48,50,135,51,52,53,147,54,55,56,59,60,134&streammode=1&autostart=1000");
        
            // Inject JavaScript to set the background color to green
            var jsExecutor = (IJavaScriptExecutor)_driver;
          //  jsExecutor.ExecuteScript("document.body.style.backgroundColor = 'lightblue';");
        }

        public void RerollPerks()
        {
            try
            {
                // Find all elements with the class "slot-container"
                var slotContainers = _driver.FindElements(By.ClassName("slot-container"));

                if (slotContainers.Count > 0)
                {
                    // Click on the first slot-container (or any specific one you want)
                    slotContainers[0].Click();
                    Console.WriteLine("Perks rerolled successfully by clicking on a slot.");
                }
                else
                {
                    Console.WriteLine("No slot-container elements found.");
                }
            }
            catch (NoSuchElementException)
            {
                Console.WriteLine("Slot-container elements not found.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while rerolling perks: {ex.Message}");
            }
        }

        public void Close()
        {
            // Close the browser
            _driver.Quit();
        }
    }
}