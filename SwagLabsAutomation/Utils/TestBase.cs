using AventStack.ExtentReports;
using NUnit.Framework.Interfaces;
using OpenQA.Selenium;

namespace SwagLabsAutomation.Utils
{
    public class TestBase
    {
        protected IWebDriver? Driver;
        protected ExtentTest Test;

        [SetUp]
        public virtual void Setup()
        {
            // Inicializa o teste do ExtentReports
            Test = ExtentReportManager.CreateTest(TestContext.CurrentContext.Test.Name);
            Test.Info("Iniciando teste");

            try
            {
                // Usar exclusivamente o DriverFactory para obter o driver
                Driver = DriverFactory.GetDriver();
                Test.Info("Browser iniciado e configurado");
            }
            catch (Exception ex)
            {
                Test?.Fail($"Falha ao inicializar o browser: {ex.Message}");
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
                    Test?.Fail($"Teste falhou: {errorMessage}");
                    CaptureScreenshot();
                }
                else if (status == TestStatus.Passed)
                {
                    Test?.Pass("Teste concluído com sucesso");
                }
                else
                {
                    Test?.Skip("Teste foi ignorado");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro durante TearDown: {ex.Message}");
            }
            finally
            {
                // Explicitamente fazer o Dispose do driver (necessário para o NUnit)
                if (Driver != null)
                {
                    try { Driver.Dispose(); } catch { }
                    Driver = null;
                }
        
                // Delega a limpeza para o DriverFactory
                DriverFactory.QuitDriver();
            }
        }

        protected void CaptureScreenshot()
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

                Test.AddScreenCaptureFromPath(screenshotPath, "Screenshot no momento da falha");
                Console.WriteLine($"Screenshot salvo em: {screenshotPath}");
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
                Test?.Info($"Passo: {stepName}");
                action();
                Test?.Pass($"Passo '{stepName}' executado com sucesso");
            }
            catch (Exception ex)
            {
                Test?.Fail($"Falha no passo '{stepName}': {ex.Message}");
                CaptureScreenshot();
                throw;
            }
        }

        protected void AddTestMetadata()
        {
            Test.Info($"<div style='background-color: #f5f5f5; padding: 10px; border-radius: 5px;'>" +
                      $"<b>Início:</b> {DateTime.Now:dd/MM/yyyy HH:mm:ss}<br/>" +
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

        // Método de limpeza global que será chamado uma vez após todos os testes
        [OneTimeTearDown]
        public void GlobalCleanup()
        {
            try
            {
                // Garantir limpeza total
                DriverFactory.QuitDriver();
                
                // Verificar e matar qualquer processo ChromeDriver que ainda exista
                DriverFactory.KillChromeProcesses();
                
                // Finalizar relatórios
                ExtentReportManager.EndReport();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro durante limpeza global: {ex.Message}");
            }
        }
    }
}