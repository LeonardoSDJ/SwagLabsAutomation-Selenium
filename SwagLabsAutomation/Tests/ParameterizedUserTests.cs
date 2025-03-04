using OpenQA.Selenium;
using SwagLabsAutomation.Pages;
using SwagLabsAutomation.Utils;

namespace SwagLabsAutomation.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Self)] 
public class ParameterizedUserTests : TestBase
{
    private LoginPage _loginPage;
    private UserPerformanceTracker? _tracker;
       
    [OneTimeSetUp]
    public void GlobalSetup()
    {
        DriverFactory.QuitDriver();
    }
       
    [SetUp]
    public void SetupTest()
    {
        // Call base class Setup first
        base.Setup();
   
        _loginPage = new LoginPage(Driver);
        LogInfo("Navigating to login page");
        _loginPage.NavigateToLoginPage();
        LogInfo("Login page loaded");
    }

    [TearDown]
    public new void TearDown()
    {
        try
        {
            _tracker = null;
            base.TearDown();
            Thread.Sleep(500);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in TearDown: {ex.Message}");
        }
    }
       
    [OneTimeTearDown]
    public void FinalCleanup()
    {
        DriverFactory.QuitDriver();
        Thread.Sleep(1000);
    }

    // Test data - pairs of user and expected login result
    public static IEnumerable<TestCaseData> LoginTestCases
    {
        get
        {
            yield return new TestCaseData(UserModel.Standard, true, null)
                .SetName("Standard_User_Can_Login");

            yield return new TestCaseData(UserModel.LockedOut, false, "Epic sadface: Sorry, this user has been locked out.")
                .SetName("Locked_Out_User_Cannot_Login");

            yield return new TestCaseData(UserModel.Problem, true, null)
                .SetName("Problem_User_Can_Login_With_UI_Issues");

            yield return new TestCaseData(UserModel.PerformanceGlitch, true, null)
                .SetName("Performance_User_Can_Login_Slowly");
        }
    }

    [Test, TestCaseSource(nameof(LoginTestCases))]
    [Description("Tests login behavior for different user types")]
    public void Test_User_Login(UserModel user, bool shouldSucceed, string expectedErrorMessage)
    {
        // Arrange
        LogInfo($"Testing login for user: {user.Username} ({user.Type})");
        _tracker = new UserPerformanceTracker(Driver, user.Username, Test);

        // Act
        LogStep("Starting login", () => {
            _tracker.StartTracking("login");
            var productsPage = _loginPage.Login(user.Username, user.Password);
            var elapsedTime = _tracker.StopTracking("login");
               
            // Assert
            if (shouldSucceed)
            {
                LogInfo("Verifying redirect to products page");
                bool isOnProductsPage = productsPage.IsOnProductsPage();
                Assert.That(isOnProductsPage, Is.True,
                    $"User {user.Username} should login successfully");

                if (isOnProductsPage)
                    LogPass($"Login successful for {user.Username}");

                // Additional verification for performance_glitch_user
                if (user.Type == UserType.PerformanceGlitch)
                {
                    LogInfo($"Checking response time for glitch user: {elapsedTime}ms");
                    Assert.That(elapsedTime, Is.GreaterThan(2000),
                        "Login with performance_glitch_user should be slower");

                    if (elapsedTime > 2000)
                        LogPass($"Slow performance behavior confirmed: {elapsedTime}ms");

                    _tracker.LogUserBehavior("Slow Performance",
                        $"Login took {elapsedTime}ms, significantly slower than normal");
                }
            }
            else
            {
                LogInfo("Checking error message for locked user");
                string actualErrorMessage = _loginPage.GetErrorMessage();
                LogInfo($"Message received: '{actualErrorMessage}'");
                   
                Assert.That(actualErrorMessage, Is.EqualTo(expectedErrorMessage),
                    $"Error message for {user.Username} does not match expected");

                if (actualErrorMessage == expectedErrorMessage)
                    LogPass("Error message verified successfully");

                _tracker.LogUserBehavior("Locked Login",
                    $"Locked user attempted to login and received message: {actualErrorMessage}");
            }
        });
    }

    // Test data for UI verification by user type
    public static IEnumerable<TestCaseData> UiTestCases
    {
        get
        {
            yield return new TestCaseData(UserModel.Standard, false)
                .SetName("Standard_User_Shows_Correct_Images");

            yield return new TestCaseData(UserModel.Problem, true)
                .SetName("Problem_User_Shows_Incorrect_Images");
        }
    }

    [Test, TestCaseSource(nameof(UiTestCases))]
    [Description("Tests UI behavior for different user types")]
    public void Test_User_UI(UserModel user, bool shouldHaveSameImages)
    {
        // Arrange - do login
        LogInfo($"Testing UI for user: {user.Username} ({user.Type})");
        _tracker = new UserPerformanceTracker(Driver, user.Username, Test);
           
        LogStep("Performing login", () => {
            var productsPage = _loginPage.Login(user.Username, user.Password);
               
            // Act
            LogInfo("Verifying product images behavior");
            _tracker.StartTracking("verify_ui");
            bool allImagesAreSame = productsPage.AreAllProductImagesTheSame();
            _tracker.StopTracking("verify_ui");

            // Assert
            LogInfo($"Verification result: all images are the same = {allImagesAreSame}");
            Assert.That(allImagesAreSame, Is.EqualTo(shouldHaveSameImages),
                shouldHaveSameImages
                    ? "Images should all be the same for this user type"
                    : "Images should not all be the same for this user type");

            if (allImagesAreSame == shouldHaveSameImages)
                LogPass("Image behavior verified successfully");
            else
                LogFail("Image behavior different than expected");

            if (shouldHaveSameImages && allImagesAreSame)
            {
                _tracker.LogUserBehavior("UI Issue",
                    "All product images are identical, indicating known UI issue");
            }
        });
    }

    // Test data for checkout by user type
    public static IEnumerable<TestCaseData> CheckoutTestCases
    {
        get
        {
            yield return new TestCaseData(UserModel.Standard, true)
                .SetName("Standard_User_Can_Complete_Checkout");

            yield return new TestCaseData(UserModel.Problem, false)
                .SetName("Problem_User_Cannot_Complete_Checkout");

            yield return new TestCaseData(UserModel.PerformanceGlitch, true)
                .SetName("Performance_User_Can_Complete_Checkout_Slowly");
        }
    }

    [Test, TestCaseSource(nameof(CheckoutTestCases))]
    [Description("Tests checkout flow for different user types")]
    public void Test_User_Checkout(UserModel user, bool shouldComplete)
    {
        try
        {
            // Skip test for locked out user
            if (user.Type == UserType.LockedOut)
            {
                LogInfo("Checkout test skipped for locked out user");
                Assert.Ignore("Checkout test skipped for locked out user");
                return;
            }

            // Arrange - login and add product to cart
            LogInfo($"Testing checkout for user: {user.Username} ({user.Type})");
            _tracker = new UserPerformanceTracker(Driver, user.Username, Test);
           
            LogStep("Performing checkout process", () => {
                var productsPage = _loginPage.Login(user.Username, user.Password);
                LogInfo("Login completed successfully");

                _tracker.StartTracking("complete_checkout");

                // Add product to cart
                LogInfo("Adding product to cart");
                productsPage.AddProductToCart("sauce-labs-backpack");
                productsPage.GoToCart();
                LogInfo("Product added and navigating to cart");

                var cartPage = new CartPage(Driver);
                cartPage.GoToCheckout();
                LogInfo("Starting checkout");

                // Try to complete checkout
                var checkoutPage = new CheckoutPage(Driver);
                LogInfo("Filling personal information");
                checkoutPage.FillPersonalInfo("Test", "User", "12345");

                bool hasErrors = checkoutPage.HasFormErrors();
                LogInfo($"Form has errors: {hasErrors}");

                if (!hasErrors)
                {
                    LogInfo("Continuing with checkout");
                    checkoutPage.ClickContinue();
                    checkoutPage.CompleteCheckout();
                    LogInfo("Checkout completed");
                }

                var elapsedTime = _tracker.StopTracking("complete_checkout");

                // Assert - verify if checkout was completed as expected
                if (shouldComplete)
                {
                    bool orderComplete = checkoutPage.IsOrderComplete();
                    LogInfo($"Order complete: {orderComplete}");
                   
                    Assert.That(orderComplete, Is.True,
                        $"User {user.Username} should be able to complete checkout");

                    if (orderComplete)
                        LogPass("Checkout completed successfully");
                    else
                        LogFail("Checkout was not completed as expected");

                    if (user.Type == UserType.PerformanceGlitch)
                    {
                        LogInfo($"Checking response time for checkout: {elapsedTime}ms");
                        Assert.That(elapsedTime, Is.GreaterThan(3000),
                            "Checkout with performance_glitch_user should be slower");

                        if (elapsedTime > 3000)
                            LogPass($"Slow performance behavior confirmed: {elapsedTime}ms");

                        _tracker.LogUserBehavior("Slow Checkout",
                            $"Complete checkout took {elapsedTime}ms, significantly slower than normal");
                    }
                }
                else
                {
                    bool checkoutFailed = hasErrors || !checkoutPage.IsOrderComplete();
                    LogInfo($"Checkout failed as expected: {checkoutFailed}");
                   
                    Assert.That(checkoutFailed, Is.True,
                        $"User {user.Username} should not be able to complete checkout");

                    if (checkoutFailed)
                        LogPass("Checkout behavior for problem user verified successfully");
                    else
                        LogFail("Checkout was completed, which was not expected");

                    _tracker.LogUserBehavior("Checkout Issues",
                        "Could not complete checkout due to form problems");
                }
            });
        }
        catch (WebDriverTimeoutException tex)
        {
            // Special handling for timeouts
            LogInfo($"Timeout detected during test: {tex.Message}");
            LogFail($"Failure by timeout: {tex.Message}");
               
            // Fail test in a controlled manner
            Assert.Fail($"Timeout during test with {user.Username}: {tex.Message}");
        }
        catch (Exception ex)
        {
            // Handling for other exceptions
            LogInfo($"Exception detected during test: {ex.Message}");
            LogFail($"Failure by exception: {ex.Message}");
               
            // Fail test in a controlled manner
            Assert.Fail($"Error during test with {user.Username}: {ex.Message}");
        }
    }
}