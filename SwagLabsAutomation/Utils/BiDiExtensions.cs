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
    /// <param name="test">Test Extent para registrar informações</param>
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
        // Verificar erros e capturar screenshots relevantes
        handler.CaptureErrorScreenshots(testName);
        
        // Adicionar informações coletadas ao relatório
        handler.AddInfoToReport();
        
        // Capturar screenshot final do teste
        handler.CaptureScreenshot(testName);
        
        // Desativar monitoramentos
        handler.DisableAllMonitoring();
        
        // Liberar recursos
        handler.Dispose();
    }
    
    /// <summary>
    /// Configura monitoramento completo (rede, console e performance)
    /// </summary>
    /// <param name="handler">BiDiHandler a ser configurado</param>
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