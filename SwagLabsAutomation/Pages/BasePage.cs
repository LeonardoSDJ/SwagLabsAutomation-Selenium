using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace SwagLabsAutomation.Pages;

public class BasePage(IWebDriver? driver)
{
    protected readonly IWebDriver? Driver = driver;
    private readonly WebDriverWait _wait = new(driver ?? throw new ArgumentNullException(nameof(driver)), TimeSpan.FromSeconds(10));

    protected void WaitForElementVisible(By locator)
    {
        _wait.Until(ExpectedConditions.ElementIsVisible(locator));
    }

    protected void WaitForElementClickable(By locator)
    {
        _wait.Until(ExpectedConditions.ElementToBeClickable(locator));
    }

    protected bool IsElementDisplayed(By locator)
    {
        try
        {
            return Driver!.FindElement(locator).Displayed;
        }
        catch
        {
            return false;
        }
    }
}