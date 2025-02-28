using AventStack.ExtentReports;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.IO;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using SwagLabsAutomation.Utils;

namespace SwagLabsAutomation.Utils
{
    public class TestBase : IDisposable
    {
        protected IWebDriver Driver;
        protected ExtentTest Test;

        [SetUp]
        public void Setup()
        {
            // Inicializa o teste do ExtentReports
            Test = ExtentReportManager.CreateTest(TestContext.CurrentContext.Test.Name);
            Test.Info("Iniciando teste");

            // Configuração automática do driver
            try
            {
                new DriverManager().SetUpDriver(new ChromeConfig());
                Test.Info("Driver configurado com sucesso");
            }
            catch (Exception ex)
            {
                Test.Fail($"Falha ao configurar o driver: {ex.Message}");
                throw;
            }

            // Inicialização do driver
            try
            {
                Driver = new ChromeDriver();
                Driver.Manage().Window.Maximize();
                Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
                Test.Info("Browser iniciado e configurado");
            }
            catch (Exception ex)
            {
                Test.Fail($"Falha ao inicializar o browser: {ex.Message}");
                throw;
            }
        }

        [TearDown]
        public void TearDown()
        {
            var status = TestContext.CurrentContext.Result.Outcome.Status;
            var errorMessage = TestContext.CurrentContext.Result.Message;

            if (status == TestStatus.Failed)
            {
                Test.Fail($"Teste falhou: {errorMessage}");
                CaptureScreenshot();
            }
            else if (status == TestStatus.Passed)
            {
                Test.Pass("Teste concluído com sucesso");
            }
            else
            {
                Test.Skip("Teste foi ignorado");
            }

            Dispose();
        }

        public void Dispose()
        {
            try
            {
                if (Driver != null)
                {
                    Test.Info("Finalizando o navegador");
                    Driver.Quit();
                    Driver.Dispose();
                }
            }
            catch (Exception ex)
            {
                Test.Warning($"Erro ao finalizar o navegador: {ex.Message}");
            }
            finally
            {
                Driver = null;
            }
        }

        public void CaptureScreenshot()
        {
            try
            {
                if (Driver != null)
                {
                    string screenshotDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshots");
                    if (!Directory.Exists(screenshotDir))
                    {
                        Directory.CreateDirectory(screenshotDir);
                    }

                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    var testName = TestContext.CurrentContext.Test.Name;
                    var screenshotPath = Path.Combine(screenshotDir, $"{testName}_{timestamp}.png");

                    var screenshot = ((ITakesScreenshot)Driver).GetScreenshot();
                    screenshot.SaveAsFile(screenshotPath);

                    Test.AddScreenCaptureFromPath(screenshotPath, "Screenshot no momento da falha");
                    Console.WriteLine($"Screenshot salvo em: {screenshotPath}");
                }
            }
            catch (Exception ex)
            {
                Test.Error($"Falha ao capturar screenshot: {ex.Message}");
            }
        }

        // Métodos auxiliares para registrar informações de teste
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
                Test.Info($"Passo: {stepName}");
                action();
                Test.Pass($"Passo '{stepName}' executado com sucesso");
            }
            catch (Exception ex)
            {
                Test.Fail($"Falha no passo '{stepName}': {ex.Message}");
                CaptureScreenshot();
                throw;
            }
        }

        protected void AddTestMetadata()
        {
            Test.Info($"<div style='background-color: #f5f5f5; padding: 10px; border-radius: 5px;'>" +
                      $"<b>Início:</b> {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}<br/>" +
                      $"<b>Navegador:</b> Chrome<br/>" +
                      $"<b>Usuário:</b> {Environment.UserName}" +
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
                Test.Info($"Desempenho: '{operationName}' levou {stopwatch.ElapsedMilliseconds} ms");
            }
        }

        protected void CaptureScreenshotWithHighlight(IWebElement element, string stepDescription)
        {
            try
            {
                if (Driver != null)
                {
                    // Destaca o elemento com JavaScript
                    IJavaScriptExecutor js = (IJavaScriptExecutor)Driver;
                    string originalStyle = (string)js.ExecuteScript("return arguments[0].getAttribute('style');", element);
                    js.ExecuteScript("arguments[0].setAttribute('style', 'border: 2px solid red; background-color: yellow;');", element);

                    // Captura a screenshot
                    string screenshotDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshots");
                    if (!Directory.Exists(screenshotDir))
                    {
                        Directory.CreateDirectory(screenshotDir);
                    }

                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    var testName = TestContext.CurrentContext.Test.Name;
                    var screenshotPath = Path.Combine(screenshotDir, $"{testName}_{timestamp}.png");

                    var screenshot = ((ITakesScreenshot)Driver).GetScreenshot();
                    screenshot.SaveAsFile(screenshotPath);

                    // Restaura o estilo original
                    js.ExecuteScript("arguments[0].setAttribute('style', arguments[1]);", element, originalStyle ?? "");

                    Test.AddScreenCaptureFromPath(screenshotPath, stepDescription);
                }
            }
            catch (Exception ex)
            {
                Test.Warning($"Falha ao capturar screenshot com destaque: {ex.Message}");
            }
        }
        static TestBase()
        {
            Console.WriteLine("Construtor estático TestBase - Garantindo inicialização do ExtentReports");
            var instance = ExtentReportManager.GetInstance();
        }
    }
}