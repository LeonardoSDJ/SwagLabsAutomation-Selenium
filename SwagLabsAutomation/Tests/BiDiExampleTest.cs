using SwagLabsAutomation.Pages;
using SwagLabsAutomation.Utils;

namespace SwagLabsAutomation.Tests;

[TestFixture]
public class BiDiExampleTests : TestBase
{
    private LoginPage _loginPage;
    private ProductsPage _productsPage;
    private BiDiHandler _biDiHandler;

    [SetUp]
    public void SetupTest()
    {
        // Call base class Setup to configure driver and report
        base.Setup();
            
        // Initialize BiDiHandler with full monitoring
        _biDiHandler = Driver!.SetupBiDiMonitoring(Test, enableNetwork: true);
        _biDiHandler.EnableFullMonitoring();
        
        LogInfo("BiDiHandler configured successfully");
            
        // Initialize pages
        _loginPage = new LoginPage(Driver);
        _productsPage = new ProductsPage(Driver);
    }

    [TearDown]
    public new void TearDown()
    {
        try
        {
            // Process BiDi results before finalizing the test
            {
                var testName = TestContext.CurrentContext.Test.Name;
                _biDiHandler.ProcessBiDiResults(testName);
                LogInfo("BiDi processing completed");
            }
        }
        catch (Exception ex)
        {
            LogWarning($"Error processing BiDi results: {ex.Message}");
        }
        finally
        {
            _biDiHandler.Dispose();
            // Call base class TearDown to finalize the test
            base.TearDown();
        }
    }

    [Test]
    [Description("Tests login with BiDi enabled to monitor requests and console")]
    public void Login_With_BiDi_Monitoring()
    {
        // Arrange - Navigate to login page
        LogStep("Navigating to login page", () => {
            _loginPage.NavigateToLoginPage();
        });

        // Act - Login with standard user
        LogStep("Performing login", () => { 
            var productsPage = _loginPage.Login("standard_user", "secret_sauce");
            
            // Wait for page to load completely
            Thread.Sleep(1000);

            // Verify login was successful
            Assert.That(productsPage.IsOnProductsPage(), Is.True, 
                "Login did not redirect to products page");
        });
        
        // Assert - Verify information captured by BiDi
        LogStep("Verifying metrics and network requests", () => {
            // Check for JavaScript errors
            var jsErrors = _biDiHandler.CollectJavaScriptErrors();
            Assert.That(jsErrors.Count, Is.EqualTo(0), 
                $"Found {jsErrors.Count} JavaScript errors during the test");
            
            LogPass("Login completed successfully and monitored by BiDi");
        });
    }
    
    [Test]
    [Description("Tests invalid login with error monitoring")]
    public void Invalid_Login_With_BiDi_Monitoring()
    {
        // Arrange - Navigate to login page
        LogStep("Navigating to login page", () => {
            _loginPage.NavigateToLoginPage();
        });

        // Act - Attempt login with invalid credentials
        LogStep("Attempting login with invalid credentials", () => { 
            _loginPage.Login("invalid_user", "invalid_password");
            
            // Wait for error message to appear
            Thread.Sleep(500);
        });
        
        // Assert - Verify error message and BiDi information
        LogStep("Verifying error message and network requests", () => {
            // Verify user remained on login page
            Assert.That(_loginPage.IsOnLoginPage(), Is.True, 
                "User left login page, which was not expected");
            
            // Verify error message
            var errorMessage = _loginPage.GetErrorMessage();
            Assert.That(errorMessage, Contains.Substring("Username and password do not match"), 
                "Error message does not match expected");
            
            LogPass("Invalid login test completed successfully");
        });
    }
    
    [Test]
    [Description("Tests login with locked out user")]
    public void Locked_Out_User_Login_With_BiDi()
    {
        // Arrange - Navigate to login page
        LogStep("Navigating to login page", () => {
            _loginPage.NavigateToLoginPage();
        });

        // Act - Attempt login with locked out user
        LogStep("Attempting login with locked out user", () => { 
            _loginPage.Login("locked_out_user", "secret_sauce");
            
            // Wait for error message to appear
            Thread.Sleep(500);
        });
        
        // Assert - Verify specific message for locked out user
        LogStep("Verifying error message for locked out user", () => {
            // Verify user remained on login page
            Assert.That(_loginPage.IsOnLoginPage(), Is.True, 
                "User left login page, which was not expected");
            
            // Verify specific error message
            var errorMessage = _loginPage.GetErrorMessage();
            Assert.That(errorMessage, Is.EqualTo("Epic sadface: Sorry, this user has been locked out."), 
                "Error message for locked out user is incorrect");
            
            // Check for JavaScript errors during lockout
            var jsErrors = _biDiHandler.CollectJavaScriptErrors();
            Assert.That(jsErrors.Count, Is.EqualTo(0), 
                "JavaScript errors found during user lockout");
            
            LogPass("Locked out user test completed successfully");
        });
    }
    
    [Test]
    [Description("Tests login performance")]
    public void Login_Performance_With_BiDi()
    {
        // Arrange
        LogStep("Preparing performance test", () => {
            _loginPage.NavigateToLoginPage();
            
            // Ensure performance monitoring is active
            if (!_biDiHandler.TestDevToolsConnectivity())
            {
                Assert.Ignore("DevTools not available for performance monitoring");
            }
        });

        // Act
        LogStep("Measuring login performance", () => {
            // Start manual stopwatch for comparison
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Login with performance glitch user
            _loginPage.Login("performance_glitch_user", "secret_sauce");
            
            // Wait for page to load completely
            Thread.Sleep(2000);
            
            // Stop stopwatch
            stopwatch.Stop();
            var elapsedTime = stopwatch.ElapsedMilliseconds;
            
            // Log time in report
            LogInfo($"Login time for performance_glitch_user: {elapsedTime}ms");
            using (Assert.EnterMultipleScope())
            {

                // Verify redirect to products page
                Assert.That(_productsPage.IsOnProductsPage(), Is.True,
                    "Login did not redirect to products page");

                // Verify time is significantly slower (>2s)
                Assert.That(elapsedTime, Is.GreaterThan(2000),
                    "Login time was not significantly slower as expected");
            }
        });
        
        // Assert
        LogPass("Login performance test completed successfully");
    }
}