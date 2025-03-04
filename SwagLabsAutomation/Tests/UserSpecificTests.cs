using OpenQA.Selenium;
using SwagLabsAutomation.Pages;
using SwagLabsAutomation.Utils;

namespace SwagLabsAutomation.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Self)] 
public class UserSpecificTests : TestBase
{
    private LoginPage _loginPage;
    private ProductsPage _productsPage;
    private CartPage _cartPage;

    [SetUp]
    public void SetupTest()
    {
        Setup();
            
        _loginPage = new LoginPage(Driver);
        _productsPage = new ProductsPage(Driver);
        _cartPage = new CartPage(Driver);
            
        AddTestMetadata();
    }

    [Test]
    [Description("Verifies that locked_out_user cannot log in")]
    public void LockedOutUser_CannotLogin()
    {
        LogStep("Navigating to login page", () => {
            _loginPage.NavigateToLoginPage();
        });

        LogStep("Attempting login with locked out user", () => {
            _loginPage.Login("locked_out_user", "secret_sauce");
        });

        LogStep("Verifying error message for locked out user", () => {
            var errorMessage = _loginPage.GetErrorMessage();
            LogInfo($"Error message received: '{errorMessage}'");
                
            Assert.That(errorMessage, Is.EqualTo("Epic sadface: Sorry, this user has been locked out."),
                "Error message for locked out user was not displayed correctly");

            LogInfo("Verifying user remains on login page");
            if (Driver != null)
                Assert.That(Driver.Url, Does.Not.Contain("/inventory.html"),
                    "Locked out user was able to login, which should not happen");

            LogPass("Locked out user test completed successfully");
        });
    }

    [Test]
    [Description("Verifies specific behaviors of problem_user")]
    public void ProblemUser_ShowsIncorrectProductImages()
    {
        LogStep("Logging in with problem user", () => {
            _loginPage.NavigateToLoginPage();
            _loginPage.Login("problem_user", "secret_sauce");
            LogInfo("Login performed with problem user");
        });

        LogStep("Verifying image behavior", () => {
            var firstImage = _productsPage.GetFirstProductImageSrc();
            LogInfo($"First image URL: {firstImage}");
                
            var allImagesIdentical = _productsPage.AreAllProductImagesTheSame();
            LogInfo($"All images are identical: {allImagesIdentical}");
                
            Assert.That(allImagesIdentical, Is.True,
                "Product images for problem_user should all be identical");
                
            if (allImagesIdentical)
                LogPass("Image behavior for problem user verified successfully");
        });
    }

    [Test]
    [Description("Verifies form filling issues with problem_user")]
    public void ProblemUser_CannotCompleteCheckout()
    {
        LogStep("Preparing checkout test for problem user", () => {
            _loginPage.NavigateToLoginPage();
            _loginPage.Login("problem_user", "secret_sauce");
        
            _productsPage.AddProductToCart("sauce-labs-backpack");
            _productsPage.GoToCart();
        });

        LogStep("Starting checkout process", () => {
            _cartPage.GoToCheckout();
        
            var checkoutPage = new CheckoutPage(Driver);
            checkoutPage.FillPersonalInfo("Test", "User", "12345");
        
            var lastNameValue = Driver!.FindElement(By.Id("last-name")).GetAttribute("value");
        
            Assert.That(lastNameValue, Is.Not.EqualTo("User"), 
                "Last name field should be empty or incorrect for problem_user");
        
            if (lastNameValue != "User")
                LogPass("Behavior verified: last name field does not accept input correctly");
        });
    }

    [Test]
    [Description("Verifies specific issues with problem_user sorting")]
    public void ProblemUser_SortingDoesNotWork()
    {
        LogStep("Logging in with problem user", () => {
            _loginPage.NavigateToLoginPage();
            _loginPage.Login("problem_user", "secret_sauce");
        });

        LogStep("Testing sorting", () => {
            var namesBefore = _productsPage.GetAllProductNames();
        
            Driver?.FindElement(By.ClassName("product_sort_container")).Click();
            Driver?.FindElement(By.CssSelector("option[value='za']")).Click();
        
            Thread.Sleep(1000);
        
            var namesAfter = _productsPage.GetAllProductNames();
        
            var sortingWorked = true;
        
            for (var i = 0; i < namesBefore.Count - 1; i++)
            {
                if (string.CompareOrdinal(namesAfter[i], namesAfter[i + 1]) >= 0) continue;
                sortingWorked = false;
                break;
            }
        
            Assert.That(sortingWorked, Is.False, 
                "Sorting for problem_user should not work correctly");
        
            LogInfo($"Sorting worked correctly? {sortingWorked}");
        
            if (!sortingWorked)
                LogPass("Incorrect sorting behavior verified successfully");
        });
    }

    [Test]
    [Description("Verifies loading time of performance_glitch_user")]
    public void PerformanceUser_HasSlowLoading()
    {
        LogStep("Testing loading time for performance glitch user", () => {
            LogInfo("Starting stopwatch");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            _loginPage.NavigateToLoginPage();
            LogInfo("Performing login with performance_glitch_user");
            _loginPage.Login("performance_glitch_user", "secret_sauce");

            stopwatch.Stop();
            long elapsedTime = stopwatch.ElapsedMilliseconds;
            LogInfo($"Loading time: {elapsedTime}ms");

            Assert.That(elapsedTime, Is.GreaterThan(2000),
                "Loading time for performance_glitch_user should be slower");
                
            if (elapsedTime > 2000)
                LogPass($"Slowness behavior confirmed: {elapsedTime}ms");
            else
                LogFail($"Loading time ({elapsedTime}ms) was not significantly slow");
        });
    }

    [Test]
    [Description("Verifies that performance_glitch_user can complete purchase flow, despite slow performance")]
    public void PerformanceUser_CanCompleteCheckout()
    {
        LogStep("Testing checkout flow for performance glitch user", () => {
            _loginPage.NavigateToLoginPage();
            LogInfo("Performing login with performance_glitch_user");
            _loginPage.Login("performance_glitch_user", "secret_sauce");
            LogInfo("Login successful");

            LogInfo("Adding product to cart");
            _productsPage.AddProductToCart("sauce-labs-backpack");
            LogInfo("Navigating to cart");
            _productsPage.GoToCart();
            LogInfo("Starting checkout");
            _cartPage.GoToCheckout();

            var checkoutPage = new CheckoutPage(Driver);
            LogInfo("Filling personal information");
            checkoutPage.FillPersonalInfo("Test", "User", "12345");
            LogInfo("Continuing to next step");
            checkoutPage.ClickContinue();
            LogInfo("Completing purchase");
            checkoutPage.CompleteCheckout();

            bool orderComplete = checkoutPage.IsOrderComplete();
            LogInfo($"Order complete: {orderComplete}");
                
            Assert.That(orderComplete, Is.True,
                "performance_glitch_user should be able to complete checkout");
                
            if (orderComplete)
                LogPass("Checkout completed successfully for performance user");
            else
                LogFail("Checkout was not completed for performance user");
        });
    }
}