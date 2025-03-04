using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;
using AventStack.ExtentReports.Reporter.Configuration;
using System.Globalization;

namespace SwagLabsAutomation.Utils;

public abstract class ExtentReportManager
{
    private static readonly Lock ReportLock = new();
    private static AventStack.ExtentReports.ExtentReports? _extent;
    private static string _reportPath = string.Empty;
    private static readonly Dictionary<string, ExtentTest> TestMap = new();
    
    public static AventStack.ExtentReports.ExtentReports GetInstance()
    {
        lock (ReportLock)
        {
            try
            {
                if (_extent != null) return _extent;
                Console.WriteLine("Initializing ExtentReports...");
                var reportDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
                Console.WriteLine($"Reports directory: {reportDir}");

                if (!Directory.Exists(reportDir))
                {
                    Console.WriteLine("Creating reports directory");
                    Directory.CreateDirectory(reportDir);
                }

                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                _reportPath = Path.Combine(reportDir, $"SwagLabsReport_{timestamp}.html");
                Console.WriteLine($"Report path will be: {_reportPath}");

                var htmlReporter = new ExtentHtmlReporter(_reportPath);
                Console.WriteLine("ExtentHtmlReporter created");

                htmlReporter.Config.Theme = Theme.Dark;
                htmlReporter.Config.DocumentTitle = "SwagLabs Test Automation Report";
                htmlReporter.Config.ReportName = "Swag Labs Test Results";
                htmlReporter.Config.EnableTimeline = true;

                _extent = new AventStack.ExtentReports.ExtentReports();
                _extent.AttachReporter(htmlReporter);
                Console.WriteLine("Reporter attached to ExtentReports");

                _extent.AddSystemInfo("Application", "Swag Labs");
                _extent.AddSystemInfo("Environment", "QA");
                _extent.AddSystemInfo("Browser", "Chrome");
                _extent.AddSystemInfo("Operating System", Environment.OSVersion.ToString());
                _extent.AddSystemInfo("Machine", Environment.MachineName);
                _extent.AddSystemInfo("User", Environment.UserName);
                _extent.AddSystemInfo("Date/Time", DateTime.Now.ToString(CultureInfo.InvariantCulture));

                return _extent;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR initializing ExtentReports: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                throw;
            }
        }
    }

    public static ExtentTest CreateTest(string testName, string? description = null)
    {
        lock (ReportLock)
        {
            var uniqueId = $"{testName}_{Environment.CurrentManagedThreadId}";
            
            if (TestMap.TryGetValue(uniqueId, out var test1))
            {
                return test1;
            }
            
            var test = GetInstance().CreateTest(testName, description);
            TestMap[uniqueId] = test;
            
            Console.WriteLine($"Created test '{testName}' with ID {uniqueId}");
            return test;
        }
    }
    
    public static void UpdateTest(ExtentTest test, Status status, string message)
    {
        lock (ReportLock)
        {
            test.Log(status, message);
        }
    }
    
    public static void AddScreenshot(ExtentTest test, string screenshotPath, string title = "Screenshot")
    {
        lock (ReportLock)
        {
            try
            {
                if (File.Exists(screenshotPath))
                {
                    test.AddScreenCaptureFromPath(screenshotPath, title);
                }
                else
                {
                    test.Warning($"Screenshot not found at path: {screenshotPath}");
                }
            }
            catch (Exception ex)
            {
                test.Warning($"Could not add screenshot: {ex.Message}");
            }
        }
    }

    public static void FlushReport()
    {
        lock (ReportLock)
        {
            GetInstance().Flush();
        }
    }

    public static void EndReport()
    {
        lock (ReportLock)
        {
            try
            {
                Console.WriteLine("Finalizing report...");
                if (_extent != null)
                {
                    _extent.Flush();
                    Console.WriteLine($"Report generated at: {Path.GetFullPath(_reportPath ?? throw new InvalidOperationException())}");
                    
                    TestMap.Clear();
                }
                else
                {
                    Console.WriteLine("WARNING: _extent is null, could not finalize the report!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR finalizing report: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
            }
        }
    }
    
    public static ExtentTest GetTestById(string testName)
    {
        lock (ReportLock)
        {
            var uniqueId = $"{testName}_{Environment.CurrentManagedThreadId}";
            
            if (TestMap.TryGetValue(uniqueId, out ExtentTest? value))
            {
                return value;
            }
            
            Console.WriteLine($"Test with ID {uniqueId} not found, creating new one");
            return CreateTest(testName);
        }
    }
}