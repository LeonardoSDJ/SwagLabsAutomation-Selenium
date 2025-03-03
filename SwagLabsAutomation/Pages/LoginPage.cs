using OpenQA.Selenium;

namespace SwagLabsAutomation.Pages;

public class LoginPage(IWebDriver? driver) : BasePage(driver)
{
    // Locators
    private static By UsernameField => By.Id("user-name");
    private static By PasswordField => By.Id("password");
    private static By LoginButton => By.Id("login-button");
    private static By ErrorMessage => By.CssSelector("[data-test='error']");

    public void NavigateToLoginPage()
    {
        Driver!.Navigate().GoToUrl("https://www.saucedemo.com/");
    }

    public ProductsPage Login(string username, string password)
    {
        WaitForElementVisible(UsernameField);
        Driver!.FindElement(UsernameField).SendKeys(username);
        Driver.FindElement(PasswordField).SendKeys(password);
        Driver.FindElement(LoginButton).Click();

        return new ProductsPage(Driver);
    }

    public string GetErrorMessage()
    {
        WaitForElementVisible(ErrorMessage);
        return Driver!.FindElement(ErrorMessage).Text;
    }

    public bool IsOnLoginPage()
    {
        return IsElementDisplayed(LoginButton);
    }
}