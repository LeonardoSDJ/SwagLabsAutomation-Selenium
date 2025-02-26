using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;

namespace SwagLabsAutomation.Pages
{
    public class BasePage
    {
        protected IWebDriver Driver;
        protected WebDriverWait Wait;

        public BasePage(IWebDriver driver)
        {
            Driver = driver;
            Wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        }

        public void WaitForElementVisible(By locator)
        {
            Wait.Until(ExpectedConditions.ElementIsVisible(locator));
        }

        public void WaitForElementClickable(By locator)
        {
            Wait.Until(ExpectedConditions.ElementToBeClickable(locator));
        }

        public bool IsElementDisplayed(By locator)
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