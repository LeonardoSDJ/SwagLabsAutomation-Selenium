using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Diagnostics;

namespace SwagLabsAutomation.Utils;

public static class DriverFactory
{
    private static readonly ThreadLocal<IWebDriver?> DriverInstance = new();
    private static readonly ThreadLocal<string> SessionId = new();
    private static readonly Lock LockObject = new();

    public static IWebDriver? GetDriver(string testName = "")
    {
        if (DriverInstance.Value != null) return DriverInstance.Value;
        
        lock (LockObject)
        {
            try
            {
                var options = new ChromeOptions();
                // Basics configs
                options.AddArgument("--no-sandbox");
                options.AddArgument("--disable-dev-shm-usage");
                options.AddArgument("--disable-gpu");
                options.AddArgument("start-maximized");
                
                // Configs for avoiding orphans processes
                options.AddArgument("--disable-extensions");
                options.AddArgument("--disable-infobars");
                options.AddArgument("--disable-notifications");
                options.AddArgument("--disable-popup-blocking");
                
                // Use a single port of debbuging for evey thread
                var randomPort = new Random().Next(9000, 10000);
                options.AddArgument($"--remote-debugging-port={randomPort}");
                
                // Add a unique identifier for each instance
                var instanceId = Guid.NewGuid().ToString().Substring(0, 8);
                SessionId.Value = $"{testName}_{instanceId}";
                options.AddArgument($"--user-data-dir=./chrome-data-{SessionId.Value}");
                
                // ChromeDriver service with hidden window
                var service = ChromeDriverService.CreateDefaultService();
                service.HideCommandPromptWindow = true;
                
                // Initialize the ChromeDriver
                DriverInstance.Value = new ChromeDriver(service, options);
                DriverInstance.Value.Manage().Window.Maximize();
                DriverInstance.Value.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
                
                Console.WriteLine($"ChromeDriver started successfully: {SessionId.Value}");
                return DriverInstance.Value;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                QuitDriver();
                throw;
            }
        }
    }
    
    public static void QuitDriver()
    {
        if (DriverInstance.Value == null) return;
        
        try
        {
            Console.WriteLine("Terminating ChromeDriver instance");
            
            // Try to close all windows first
            try 
            { 
                DriverInstance.Value.Close(); 
            }
            catch { /* ignored */ }
            
            // Next, quit the driver
            try 
            { 
                DriverInstance.Value.Quit(); 
            }
            catch { /* ignored */ }
            
            // Finally, dispose the object
            try 
            { 
                DriverInstance.Value.Dispose(); 
            }
            catch { /* ignored */ }
            
            // Clear the reference
            DriverInstance.Value = null;
        }
        catch (Exception ex) 
        { 
            Console.WriteLine($"Error terminating driver: {ex.Message}");
        }
        finally
        {
            // Ensure orphaned processes are terminated
            KillChromeProcesses();
            
            // Wait enough time for complete termination
            Thread.Sleep(500);
        }
    }
    
    public static void KillChromeProcesses()
    {
        try
        {
            Console.WriteLine("Checking for orphaned processes...");
            
            // Terminate chromedriver processes
            int chromedriverCount = 0;
            foreach (var process in Process.GetProcessesByName("chromedriver"))
            {
                try 
                { 
                    process.Kill(true);
                    chromedriverCount++;
                }
                catch { /* ignored */ }
            }
            
            if (chromedriverCount > 0)
            {
                Console.WriteLine($"Terminated {chromedriverCount} orphaned chromedriver processes");
            }
            
            // Terminate Chrome processes related to automation
            var chromeCount = 0;
            foreach (var process in Process.GetProcessesByName("chrome"))
            {
                try 
                {
                    var title = process.MainWindowTitle.ToLower();
                    if (title != "data:," &&
                        !title.Contains("chrome-automation") &&
                        !title.Contains("saucedemo") &&
                        !title.Contains("swag labs")) continue;
                    process.Kill(true);
                    chromeCount++;
                }
                catch { /* ignored */ }
            }
            
            if (chromeCount > 0)
            {
                Console.WriteLine($"Terminated {chromeCount} orphaned Chrome processes");
            }
            
            // If needed, use a more aggressive approach with taskkill
            if (chromedriverCount <= 0 && chromeCount <= 0) return;
            try
            {
                using var taskkill = new Process();
                taskkill.StartInfo.FileName = "taskkill";
                taskkill.StartInfo.Arguments = "/F /IM chromedriver.exe /T";
                taskkill.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                taskkill.StartInfo.CreateNoWindow = true;
                taskkill.Start();
                taskkill.WaitForExit(2000);
            }
            catch { /* ignored */ }
        }
        catch (Exception ex) 
        { 
            Console.WriteLine($"Error terminating processes: {ex.Message}");
        }
    }
}