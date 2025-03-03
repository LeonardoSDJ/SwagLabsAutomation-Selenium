using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;
using AventStack.ExtentReports.Reporter.Configuration;
using System.Globalization;

namespace SwagLabsAutomation.Utils;

public abstract class ExtentReportManager
{
    private static AventStack.ExtentReports.ExtentReports? _extent;
    private static string _reportPath = string.Empty;
    public static AventStack.ExtentReports.ExtentReports GetInstance()
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

            // System information
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

    public static ExtentTest CreateTest(string testName, string? description = null)
    {
        return GetInstance().CreateTest(testName, description);
    }

    public static void EndReport()
    {
        try
        {
            Console.WriteLine(value: "Finalizing report...");
            if (_extent != null)
            {
                _extent.Flush();
                Console.WriteLine(value: $"Report generated at: {Path.GetFullPath(path: _reportPath ?? throw new InvalidOperationException())}");
            }
            else
            {
                Console.WriteLine(value: "WARNING: _extent is null, could not finalize the report!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(value: $"ERROR finalizing report: {ex.Message}");
            Console.WriteLine(value: $"StackTrace: {ex.StackTrace}");
        }
    }
}