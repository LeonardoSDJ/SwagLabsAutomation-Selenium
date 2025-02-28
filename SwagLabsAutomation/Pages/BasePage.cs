using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;

namespace SwagLabsAutomation.Pages
{
    public class BasePage(IWebDriver driver)
    {
        protected readonly IWebDriver Driver = driver;
        private readonly WebDriverWait Wait = new(driver, TimeSpan.FromSeconds(10));

        protected void WaitForElementVisible(By locator)
        {
            Wait.Until(ExpectedConditions.ElementIsVisible(locator));
        }

        protected void WaitForElementClickable(By locator)
        {
            Wait.Until(ExpectedConditions.ElementToBeClickable(locator));
        }

        protected bool IsElementDisplayed(By locator)
        {
            try
            {
                return Driver.FindElement(locator).Displayed;
            }
            catch
            {
                return false;
            }
        }
    }
}