using SwagLabsAutomation.Pages;
using SwagLabsAutomation.Utils;

namespace SwagLabsAutomation.Tests;

public class LoginTests : TestBase
{
    private LoginPage _loginPage;

    [SetUp]
    public void SetupTest()
    {
        _loginPage = new LoginPage(Driver);
        LogInfo("Navigating to login page");
        _loginPage.NavigateToLoginPage();
        LogInfo("Login page loaded");
    }

    [Test]
    public void LoginWithValidCredentials()
    {
        // Arrange
        const string username = "standard_user";
        const string password = "secret_sauce";
        LogInfo($"Attempting login with user: {username}");

        // Act
        LogInfo("Performing login");
        var productsPage = _loginPage.Login(username, password);

        // Assert
        LogInfo("Verifying redirection to products page");
        var isOnProductsPage = productsPage.IsOnProductsPage();
        Assert.That(isOnProductsPage, Is.True, "Login failed, did not redirect to products page.");

        if (isOnProductsPage)
            LogPass("Login successful, redirected to products page");
        else
            LogFail("Failed to redirect after login");
    }

    [Test]
    public void LoginWithInvalidCredentials()
    {
        // Arrange
        const string username = "invalid_user";
        const string password = "invalid_password";
        LogInfo($"Attempting login with invalid credentials: {username}");

        // Act
        LogInfo("Performing login with invalid credentials");
        _loginPage.Login(username, password);

        LogInfo("Checking error message");
        var errorMessage = _loginPage.GetErrorMessage();
        LogInfo($"Error message received: '{errorMessage}'");

        // Assert
        LogInfo("Verifying stayed on login page");
        var isOnLoginPage = _loginPage.IsOnLoginPage();
        Assert.That(isOnLoginPage, Is.True, "Did not remain on login page after attempt with invalid credentials.");

        LogInfo("Verifying error message content");
        var containsErrorMessage = errorMessage.Contains("Username and password do not match");
        Assert.That(containsErrorMessage, Is.True, "Incorrect error message or not displayed.");

        if (isOnLoginPage && containsErrorMessage)
            LogPass("Invalid login test completed successfully");
    }
}