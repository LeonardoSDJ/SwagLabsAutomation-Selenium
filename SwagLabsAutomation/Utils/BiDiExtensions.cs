using OpenQA.Selenium;
using AventStack.ExtentReports;

namespace SwagLabsAutomation.Utils;

/// <summary>
/// Extension methods to facilitate the use of BiDiHandler
/// </summary>
public static class BiDiExtensions
{
    /// <summary>
    /// Creates a BiDiHandler and configures network monitoring
    /// </summary>
    /// <param name="driver">WebDriver in use</param>
    /// <param name="test">Extent Test for logging information</param>
    /// <param name="enableNetwork">Enable network monitoring</param>
    /// <returns>Configured BiDiHandler</returns>
    public static BiDiHandler SetupBiDiMonitoring(
        this IWebDriver driver, 
        ExtentTest? test = null,
        bool enableNetwork = true)
    {
        var handler = new BiDiHandler(driver, test);
        
        // Test connectivity with DevTools before proceeding
        var devToolsAvailable = handler.TestDevToolsConnectivity();
        
        if (devToolsAvailable)
        {
            if (enableNetwork)
            {
                handler.EnableNetworkMonitoring();
            }
        }
        else
        {
            // Fallback to simplified implementation if DevTools is not available
            handler.UseSimpleImplementation();
        }
        
        return handler;
    }
    
    /// <summary>
    /// Adds BiDi information to the report and captures screenshots if there are errors
    /// </summary>
    /// <param name="handler">BiDiHandler in use</param>
    /// <param name="testName">Test name</param>
    public static void ProcessBiDiResults(this BiDiHandler handler, string testName)
    {
        // Check for errors and capture relevant screenshots
        handler.CaptureErrorScreenshots(testName);
        
        // Add collected information to the report
        handler.AddInfoToReport();
        
        // Capture final test screenshot
        handler.CaptureScreenshot(testName);
        
        // Disable monitoring
        handler.DisableAllMonitoring();
        
        // Release resources
        handler.Dispose();
    }
    
    /// <summary>
    /// Configure complete monitoring (network, console, and performance)
    /// </summary>
    /// <param name="handler">BiDiHandler to be configured</param>
    public static void EnableFullMonitoring(this BiDiHandler handler)
    {
        if (handler.TestDevToolsConnectivity())
        {
            handler.EnableNetworkMonitoring();
            handler.EnableConsoleMonitoring();
            handler.EnablePerformanceMonitoring();
        }
        else
        {
            handler.UseSimpleImplementation();
        }
    }
}