using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;
using AventStack.ExtentReports.Reporter.Configuration;
using System;
using System.IO;

namespace SwagLabsAutomation.Utils;

public  class ExtentReportManager
{
    private static AventStack.ExtentReports.ExtentReports? _extent;
    private static string? _reportPath;

    public static AventStack.ExtentReports.ExtentReports GetInstance()
    {
        try
        {
            if (_extent == null)
            {
                Console.WriteLine("Inicializando ExtentReports...");
                string reportDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
                Console.WriteLine($"Diretório de relatórios: {reportDir}");

                if (!Directory.Exists(reportDir))
                {
                    Console.WriteLine("Criando diretório de relatórios");
                    Directory.CreateDirectory(reportDir);
                }

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                _reportPath = Path.Combine(reportDir, $"SwagLabsReport_{timestamp}.html");
                Console.WriteLine($"Caminho do relatório será: {_reportPath}");

                var htmlReporter = new ExtentHtmlReporter(_reportPath);
                Console.WriteLine("ExtentHtmlReporter criado");

                htmlReporter.Config.Theme = Theme.Dark;
                htmlReporter.Config.DocumentTitle = "SwagLabs Test Automation Report";
                htmlReporter.Config.ReportName = "Swag Labs Test Results";
                htmlReporter.Config.EnableTimeline = true;

                _extent = new AventStack.ExtentReports.ExtentReports();
                _extent.AttachReporter(htmlReporter);
                Console.WriteLine("Reporter anexado ao ExtentReports");

                // Informações do sistema
                _extent.AddSystemInfo("Aplicação", "Swag Labs");
                _extent.AddSystemInfo("Ambiente", "QA");
                _extent.AddSystemInfo("Navegador", "Chrome");
                _extent.AddSystemInfo("Sistema Operacional", Environment.OSVersion.ToString());
                _extent.AddSystemInfo("Máquina", Environment.MachineName);
                _extent.AddSystemInfo("Usuário", Environment.UserName);
                _extent.AddSystemInfo("Data/Hora", DateTime.Now.ToString());
            }

            return _extent;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERRO ao inicializar ExtentReports: {ex.Message}");
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
            Console.WriteLine("Finalizando relatório...");
            if (_extent != null)
            {
                _extent.Flush();
                Console.WriteLine($"Relatório gerado em: {_reportPath}");
            }
            else
            {
                Console.WriteLine("AVISO: _extent é nulo, não foi possível finalizar o relatório!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERRO ao finalizar relatório: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
        }
    }
}