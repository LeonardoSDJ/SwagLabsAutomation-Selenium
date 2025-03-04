using AventStack.ExtentReports;
using NUnit.Framework.Interfaces;
using OpenQA.Selenium;

namespace SwagLabsAutomation.Utils
{
    public class TestBase
    {
        protected IWebDriver? Driver;
        protected ExtentTest Test;
        protected readonly string TestIdentifier;
    
        public TestBase()
        {
            // Unique identifier for each test
            TestIdentifier = $"{TestContext.CurrentContext.Test.ClassName}_{TestContext.CurrentContext.Test.Name}";
        }

        [SetUp]
        public virtual void Setup()
        {
            // Initialize ExtentReports test
            Test = ExtentReportManager.CreateTest(TestContext.CurrentContext.Test.Name);
            Test.Info($"Starting test, ID:  {TestIdentifier}");

            try
            {
                // Use DriverFactory exclusively to get driver
                Driver = DriverFactory.GetDriver(TestIdentifier);
                Test.Info("Browser started and configured");
            }
            catch (Exception ex)
            {
                Test.Fail($"Failed to initialize browser: {ex.Message}");
                throw;
            }
        }

        [TearDown]
        public virtual void TearDown()
        {
            try
            {
                var status = TestContext.CurrentContext.Result.Outcome.Status;
                var errorMessage = TestContext.CurrentContext.Result.Message;

                if (status == TestStatus.Failed)
                {
                    Test.Fail($"Test failed: {errorMessage}");
                    CaptureScreenshot();
                }
                else if (status == TestStatus.Passed)
                {
                    Test.Pass("Test completed successfully");
                }
                else
                {
                    Test.Skip("Test was skipped");
                }
        
                // Explicitly flush ExtentReport
                ExtentReportManager.GetInstance().Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during TearDown: {ex.Message}");
            }
            finally
            {
                // Explicitly dispose driver (necessary for NUnit)
                if (Driver != null)
                {
                    try { Driver.Dispose(); }
                    catch
                    {
                        // ignored
                    }

                    Driver = null;
                }

                // Delegate cleanup to DriverFactory
                DriverFactory.QuitDriver();
            }
        }

        private void CaptureScreenshot()
        {
            try
            {
                if (Driver == null) return;
                
                var screenshotDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshots");
                if (!Directory.Exists(screenshotDir))
                {
                    Directory.CreateDirectory(screenshotDir);
                }

                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var testName = TestContext.CurrentContext.Test.Name;
                var screenshotPath = Path.Combine(screenshotDir, $"{testName}_{timestamp}.png");

                var screenshot = ((ITakesScreenshot)Driver).GetScreenshot();
                screenshot.SaveAsFile(screenshotPath);

                Test.AddScreenCaptureFromPath(screenshotPath, "Screenshot at failure moment");
                Console.WriteLine($"Screenshot saved at: {screenshotPath}");
            }
            catch (Exception ex)
            {
                Test.Error($"Failed to capture screenshot: {ex.Message}");
            }
        }

        // Helper methods to log test information
        protected void LogInfo(string message)
        {
            Test.Info(message);
        }

        protected void LogPass(string message)
        {
            Test.Pass(message);
        }

        protected void LogFail(string message)
        {
            Test.Fail(message);
        }

        protected void LogWarning(string message)
        {
            Test.Warning(message);
        }

        protected void LogStep(string stepName, Action action)
        {
            try
            {
                Test.Info($"Step: {stepName}");
                action();
                Test.Pass($"Step '{stepName}' executed successfully");
            }
            catch (Exception ex)
            {
                Test.Fail($"Failed at step '{stepName}': {ex.Message}");
                CaptureScreenshot();
                throw;
            }
        }

        protected void AddTestMetadata()
        {
            Test.Info($"<div style='background-color: #f5f5f5; padding: 10px; border-radius: 5px;'>" +
                      $"<b>Start:</b> {DateTime.Now:dd/MM/yyyy HH:mm:ss}<br/>" +
                      $"<b>Browser:</b> Chrome<br/>" +
                      $"<b>User:</b> {Environment.UserName}" +
                      $"</div>");
        }
        
        protected void MeasurePerformance(string operationName, Action action)
        {
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            try
            {
                action();
            }
            finally
            {
                stopwatch.Stop();
                Test.Info($"Performance: '{operationName}' took {stopwatch.ElapsedMilliseconds} ms");
            }
        }

        // Global cleanup method that will be called once after all tests
        [OneTimeTearDown]
        public void GlobalCleanup()
        {
            try
            {
                // Ensure complete cleanup
                DriverFactory.QuitDriver();
                
                // Check and kill any ChromeDriver process that might still exist
                DriverFactory.KillChromeProcesses();
                
                // Finalize reports
                ExtentReportManager.EndReport();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during global cleanup: {ex.Message}");
            }
        }
    }
}