using OpenQA.Selenium;
using System.Diagnostics;
using AventStack.ExtentReports;

namespace SwagLabsAutomation.Utils;

public class UserPerformanceTracker
{
    private readonly IWebDriver? _driver;
    private readonly ExtentTest? _test; // Make _test nullable
    private readonly string _username;
    private readonly Stopwatch _stopwatch;
    private readonly object _operation;
    private AventStack.ExtentReports.ExtentReports extentReports;

    public UserPerformanceTracker(IWebDriver? driver, string username, ExtentTest test)
    {
        _driver = driver;
        _username = username;
        _test = test;
        _stopwatch = new Stopwatch();
        _operation = new object(); // Initialize operation
        extentReports = new AventStack.ExtentReports.ExtentReports(); // Initialize extentReports
    }

    public UserPerformanceTracker(IWebDriver? driver, string username, AventStack.ExtentReports.ExtentReports extentReports)
    {
        _driver = driver;
        _username = username;
        this.extentReports = extentReports;
        _stopwatch = new Stopwatch();
        _operation = new object(); // Initialize operation
    }

    public void StartTracking(string operation)
    {
        _stopwatch.Reset();
        _stopwatch.Start();
    
        try
        {
            LogMessage($"Iniciando operação '{operation}' para usuário '{_username}'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao registrar início da operação: {ex.Message}");
            _test?.Warning($"Erro ao iniciar tracking: {ex.Message}");
        }
    }

    public long StopTracking(string operation)
    {
        _stopwatch.Stop();
        long elapsedMs = _stopwatch.ElapsedMilliseconds;

        LogMessage($"Operação '{operation}' para usuário '{_username}' concluída em {elapsedMs}ms");

        try
        {
            // Criar o diretório Screenshots caso não exista
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string screenshotDirectory = Path.Combine(baseDir, "Screenshots");
            if (!Directory.Exists(screenshotDirectory))
            {
                Directory.CreateDirectory(screenshotDirectory);
            }

            // Capturar o screenshot
            if (_driver != null)
            {
                Screenshot screenshot = ((ITakesScreenshot)_driver).GetScreenshot();

                // Definir o caminho do arquivo
                string screenshotPath = Path.Combine(screenshotDirectory, 
                    $"{_username}_{operation}_{DateTime.Now:yyyyMMdd_HHmmss}.png");

                // Salvar o screenshot
                screenshot.SaveAsFile(screenshotPath);

                // Adicionar informações ao relatório
                _test?.Log(Status.Info, $"Operação: {operation}, Tempo: {elapsedMs}ms");
                _test?.AddScreenCaptureFromPath(screenshotPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao salvar screenshot: {ex.Message}");
            _test?.Warning($"Não foi possível salvar screenshot: {ex.Message}");
        }

        return elapsedMs;
    }

    // Em UserPerformanceTracker.cs
    public void LogUserBehavior(string behavior, string details)
    {
        LogMessage($"Comportamento detectado para '{_username}': {behavior} - {details}");

        try
        {
            // Criar o diretório Screenshots com path absoluto
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string screenshotDirectory = Path.Combine(baseDir, "Screenshots");
            if (!Directory.Exists(screenshotDirectory))
            {
                Directory.CreateDirectory(screenshotDirectory);
            }

            // Capturar o screenshot
            if (_driver != null)
            {
                Screenshot screenshot = ((ITakesScreenshot)_driver).GetScreenshot();

                // Usar behavior como parte do nome do arquivo em vez de _operation
                string screenshotPath = Path.Combine(screenshotDirectory, 
                    $"{_username}_{behavior.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.png");

                // Salvar o screenshot
                screenshot.SaveAsFile(screenshotPath);

                // Adicionar ao relatório
                _test?.Log(Status.Info, $"{behavior}: {details}");
                _test?.AddScreenCaptureFromPath(screenshotPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao salvar screenshot: {ex.Message}");
            _test?.Warning($"Não foi possível salvar screenshot: {ex.Message}");
        }
    }

    private void LogMessage(string message)
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
    
        try
        {
            // Criar diretório de logs se não existir
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string logDirectory = Path.Combine(baseDir, "Logs");
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
        
            // Adicionar logs a um arquivo
            File.AppendAllText(Path.Combine(logDirectory, "user_tests.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao salvar log: {ex.Message}");
        }
    }
}