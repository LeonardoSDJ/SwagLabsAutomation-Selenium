using OpenQA.Selenium;
using System.Diagnostics;
using AventStack.ExtentReports;

namespace SwagLabsAutomation.Utils;

public class UserPerformanceTracker
{
    private readonly IWebDriver? _driver;
    private readonly ExtentTest? _test;
    private readonly string _username;
    private readonly Stopwatch _stopwatch;
    private readonly object _operation;
    private AventStack.ExtentReports.ExtentReports _extentReports;

    public UserPerformanceTracker(IWebDriver? driver, string username, ExtentTest test)
    {
        _driver = driver;
        _username = username;
        _test = test;
        _stopwatch = new Stopwatch();
        _operation = new object(); // Initialize operation
        _extentReports = new AventStack.ExtentReports.ExtentReports(); // Initialize extentReports
    }

    public UserPerformanceTracker(IWebDriver? driver, string username, AventStack.ExtentReports.ExtentReports extentReports)
    {
        _driver = driver;
        _username = username;
        this._extentReports = extentReports;
        _stopwatch = new Stopwatch();
        _operation = new object(); // Initialize operation
    }

    public void StartTracking(string operation)
    {
        _stopwatch.Reset();
        _stopwatch.Start();
    
        try
        {
            LogMessage($"Starting operation '{operation}' for user '{_username}'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error recording operation start: {ex.Message}");
            _test?.Warning($"Error starting tracking: {ex.Message}");
        }
    }

    public long StopTracking(string operation)
    {
        _stopwatch.Stop();
        long elapsedMs = _stopwatch.ElapsedMilliseconds;

        LogMessage($"Operation '{operation}' for user '{_username}' completed in {elapsedMs}ms");

        try
        {
            // Create Screenshots directory if it doesn't exist
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string screenshotDirectory = Path.Combine(baseDir, "Screenshots");
            if (!Directory.Exists(screenshotDirectory))
            {
                Directory.CreateDirectory(screenshotDirectory);
            }

            // Capture screenshot
            if (_driver != null)
            {
                Screenshot screenshot = ((ITakesScreenshot)_driver).GetScreenshot();

                // Define file path
                string screenshotPath = Path.Combine(screenshotDirectory, 
                    $"{_username}_{operation}_{DateTime.Now:yyyyMMdd_HHmmss}.png");

                // Save screenshot
                screenshot.SaveAsFile(screenshotPath);

                // Add information to report
                _test?.Log(Status.Info, $"Operation: {operation}, Time: {elapsedMs}ms");
                _test?.AddScreenCaptureFromPath(screenshotPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving screenshot: {ex.Message}");
            _test?.Warning($"Could not save screenshot: {ex.Message}");
        }

        return elapsedMs;
    }

    public void LogUserBehavior(string behavior, string details)
    {
        LogMessage($"Behavior detected for '{_username}': {behavior} - {details}");

        try
        {
            // Create Screenshots directory with absolute path
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string screenshotDirectory = Path.Combine(baseDir, "Screenshots");
            if (!Directory.Exists(screenshotDirectory))
            {
                Directory.CreateDirectory(screenshotDirectory);
            }

            // Capture screenshot
            if (_driver != null)
            {
                Screenshot screenshot = ((ITakesScreenshot)_driver).GetScreenshot();

                // Use behavior as part of the filename instead of _operation
                string screenshotPath = Path.Combine(screenshotDirectory, 
                    $"{_username}_{behavior.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.png");

                // Save screenshot
                screenshot.SaveAsFile(screenshotPath);

                // Add to report
                _test?.Log(Status.Info, $"{behavior}: {details}");
                _test?.AddScreenCaptureFromPath(screenshotPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving screenshot: {ex.Message}");
            _test?.Warning($"Could not save screenshot: {ex.Message}");
        }
    }

    private void LogMessage(string message)
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
    
        try
        {
            // Create logs directory if it doesn't exist
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string logDirectory = Path.Combine(baseDir, "Logs");
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
        
            // Add logs to a file
            File.AppendAllText(Path.Combine(logDirectory, "user_tests.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving log: {ex.Message}");
        }
    }
}