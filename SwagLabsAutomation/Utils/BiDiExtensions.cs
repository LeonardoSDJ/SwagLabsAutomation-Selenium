using OpenQA.Selenium;
using AventStack.ExtentReports;

namespace SwagLabsAutomation.Utils;

/// <summary>
/// Métodos de extensão para facilitar o uso do BiDiHandler
/// </summary>
public static class BiDiExtensions
{
    /// <summary>
    /// Cria um BiDiHandler e configura o monitoramento de rede
    /// </summary>
    /// <param name="driver">WebDriver em uso</param>
    /// <param name="test">Teste Extent para registrar informações</param>
    /// <param name="enableNetwork">Ativar monitoramento de rede</param>
    /// <returns>BiDiHandler configurado</returns>
    public static BiDiHandler SetupBiDiMonitoring(
        this IWebDriver driver, 
        ExtentTest? test = null,
        bool enableNetwork = true)
    {
        var handler = new BiDiHandler(driver, test);
        
        // Testar conectividade com DevTools antes de prosseguir
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
            // Fallback para implementação simplificada se DevTools não estiver disponível
            handler.UseSimpleImplementation();
        }
        
        return handler;
    }
    
    /// <summary>
    /// Adiciona informações de BiDi ao relatório e captura screenshots se houver erros
    /// </summary>
    /// <param name="handler">BiDiHandler em uso</param>
    /// <param name="testName">Nome do teste</param>
    public static void ProcessBiDiResults(this BiDiHandler handler, string testName)
    {
        // Capturar screenshot em caso de erro ou como parte do relatório
        handler.CaptureScreenshot(testName);
        
        // Coletar e reportar erros de JavaScript (usando implementação simplificada)
        var jsErrors = handler.CollectJavaScriptErrors();
        if (jsErrors.Count > 0)
        {
            // Registramos os erros de JavaScript encontrados na página
            LogJsErrors(jsErrors);
        }
        
        // Desativar monitoramento de rede
        handler.DisableNetworkMonitoring();
        
        // Liberar recursos
        handler.Dispose();
    }
    
    /// <summary>
    /// Registra erros de JavaScript no console e no relatório Extent
    /// </summary>
    /// <param name="errors">Lista de erros de JavaScript</param>
    private static void LogJsErrors(List<BiDiHandler.ConsoleMessage> errors)
    {
        if (errors.Count == 0) return;
        
        foreach (var error in errors)
        {
            Console.WriteLine($"[JS Error] {error.Level}: {error.Text} - {error.Url}:{error.LineNumber}");
        }
    }
}