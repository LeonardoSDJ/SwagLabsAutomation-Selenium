using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Diagnostics;

namespace SwagLabsAutomation.Utils;

public static class DriverFactory
{
    private static readonly ThreadLocal<IWebDriver?> DriverInstance = new();
    private static readonly object _lockObject = new();
    
    public static IWebDriver? GetDriver()
    {
        if (DriverInstance.Value != null) return DriverInstance.Value;
        
        lock (_lockObject)
        {
            try
            {
                var options = new ChromeOptions();
                // Configurações básicas
                options.AddArgument("--no-sandbox");
                options.AddArgument("--disable-dev-shm-usage");
                options.AddArgument("--disable-gpu");
                
                // Configurações adicionais para evitar processos órfãos
                options.AddArgument("--disable-extensions");
                options.AddArgument("--disable-infobars");
                options.AddArgument("--disable-notifications");
                options.AddArgument("--disable-popup-blocking");
                options.AddArgument("--remote-debugging-port=0");
                
                // Serviço ChromeDriver com janela oculta
                var service = ChromeDriverService.CreateDefaultService();
                service.HideCommandPromptWindow = true;
                
                // Inicializar driver
                DriverInstance.Value = new ChromeDriver(service, options);
                DriverInstance.Value.Manage().Window.Maximize();
                DriverInstance.Value.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
                
                Console.WriteLine("ChromeDriver iniciado com sucesso");
                return DriverInstance.Value;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Erro ao criar driver: {ex.Message}");
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
            Console.WriteLine("Encerrando instância do ChromeDriver");
            
            // Tente fechar todas as janelas primeiro
            try 
            { 
                DriverInstance.Value.Close(); 
            }
            catch { /* ignorado */ }
            
            // Em seguida, encerre o driver
            try 
            { 
                DriverInstance.Value.Quit(); 
            }
            catch { /* ignorado */ }
            
            // Por fim, descarte o objeto
            try 
            { 
                DriverInstance.Value.Dispose(); 
            }
            catch { /* ignorado */ }
            
            // Limpe a referência
            DriverInstance.Value = null;
        }
        catch (Exception ex) 
        { 
            Console.WriteLine($"Erro ao encerrar driver: {ex.Message}");
        }
        finally
        {
            // Garante que processos órfãos sejam encerrados
            KillChromeProcesses();
            
            // Aguardar tempo suficiente para encerramento completo
            Thread.Sleep(500);
        }
    }
    
    public static void KillChromeProcesses()
    {
        try
        {
            Console.WriteLine("Verificando processos órfãos...");
            
            // Encerrar processos chromedriver
            int chromedriverCount = 0;
            foreach (var process in Process.GetProcessesByName("chromedriver"))
            {
                try 
                { 
                    process.Kill(true);
                    chromedriverCount++;
                }
                catch { /* ignorado */ }
            }
            
            if (chromedriverCount > 0)
            {
                Console.WriteLine($"Encerrados {chromedriverCount} processos chromedriver órfãos");
            }
            
            // Encerrar processos Chrome relacionados à automação
            int chromeCount = 0;
            foreach (var process in Process.GetProcessesByName("chrome"))
            {
                try 
                {
                    string title = process.MainWindowTitle.ToLower();
                    if (title == "data:," || 
                        title.Contains("chrome-automation") ||
                        title.Contains("saucedemo") || 
                        title.Contains("swag labs"))
                    {
                        process.Kill(true);
                        chromeCount++;
                    }
                }
                catch { /* ignorado */ }
            }
            
            if (chromeCount > 0)
            {
                Console.WriteLine($"Encerrados {chromeCount} processos Chrome órfãos");
            }
            
            // Se precisar de uma abordagem mais agressiva, pode usar taskkill
            if (chromedriverCount > 0 || chromeCount > 0)
            {
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
                catch { /* ignorado */ }
            }
        }
        catch (Exception ex) 
        { 
            Console.WriteLine($"Erro ao encerrar processos: {ex.Message}");
        }
    }
}