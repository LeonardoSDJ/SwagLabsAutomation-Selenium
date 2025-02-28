using OpenQA.Selenium;
using System.Diagnostics;
using AventStack.ExtentReports;

namespace SwagLabsAutomation.Utils
{
    public class UserPerformanceTracker
    {
        private readonly IWebDriver _driver;
        private readonly ExtentTest? _test; // Make _test nullable
        private readonly string _username;
        private readonly Stopwatch _stopwatch;
        private readonly object _operation;
        private AventStack.ExtentReports.ExtentReports extentReports;

        public UserPerformanceTracker(IWebDriver driver, string username, ExtentTest test)
        {
            _driver = driver;
            _username = username;
            _test = test;
            _stopwatch = new Stopwatch();
            _operation = new object(); // Initialize operation
            extentReports = new AventStack.ExtentReports.ExtentReports(); // Initialize extentReports
        }

        public UserPerformanceTracker(IWebDriver driver, string username, AventStack.ExtentReports.ExtentReports extentReports)
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
            LogMessage($"Iniciando operação '{operation}' para usuário '{_username}'");
        }

        public long StopTracking(string operation)
        {
            _stopwatch.Stop();
            long elapsedMs = _stopwatch.ElapsedMilliseconds;

            LogMessage($"Operação '{operation}' para usuário '{_username}' concluída em {elapsedMs}ms");

            // Criar o diretório Screenshots caso não exista
            string screenshotDirectory = "./Screenshots";
            if (!System.IO.Directory.Exists(screenshotDirectory))
            {
                System.IO.Directory.CreateDirectory(screenshotDirectory);
            }

            // Capturar o screenshot
            Screenshot screenshot = ((ITakesScreenshot)_driver).GetScreenshot();

            // Definir o caminho do arquivo de forma absoluta para evitar problemas de permissão
            string screenshotPath = Path.Combine(screenshotDirectory, $"{_username}_{operation}_{DateTime.Now:yyyyMMdd_HHmmss}.png");

            // Salvar o screenshot
            screenshot.SaveAsFile(screenshotPath);

            // Adicionar informações ao relatório
            _test?.Log(Status.Info, $"Operação: {operation}, Tempo: {elapsedMs}ms");
            _test?.AddScreenCaptureFromPath(screenshotPath);

            return elapsedMs;
        }

        public void LogUserBehavior(string behavior, string details)
        {
            LogMessage($"Comportamento detectado para '{_username}': {behavior} - {details}");

            // Criar o diretório Screenshots caso não exista
            string screenshotDirectory = "./Screenshots";
            if (!System.IO.Directory.Exists(screenshotDirectory))
            {
                System.IO.Directory.CreateDirectory(screenshotDirectory);
            }

            // Capturar o screenshot
            Screenshot screenshot = ((ITakesScreenshot)_driver).GetScreenshot();

            // Definir o caminho do arquivo de forma absoluta para evitar problemas de permissão
            string screenshotPath = Path.Combine(screenshotDirectory, $"{_username}_{_operation}_{DateTime.Now:yyyyMMdd_HHmmss}.png");

            // Salvar o screenshot
            screenshot.SaveAsFile(screenshotPath);

            // Adicionar ao relatório
            _test?.Log(Status.Info, $"{behavior}: {details}");
            _test?.AddScreenCaptureFromPath(screenshotPath);
        }

        private void LogMessage(string message)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
            // Adicionar logs a um arquivo
            System.IO.File.AppendAllText("./Logs/user_tests.log",
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n");
        }
    }
}

