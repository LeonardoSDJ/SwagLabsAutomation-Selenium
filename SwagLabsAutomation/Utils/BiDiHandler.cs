using System.Collections;
using OpenQA.Selenium;
using OpenQA.Selenium.DevTools;
using System.Reflection;
using System.Collections.Concurrent;
using AventStack.ExtentReports;

namespace SwagLabsAutomation.Utils;

/// <summary>
/// Manipulador de funcionalidades BiDi (Bidirecional) do Selenium WebDriver.
/// Fornece acesso a recursos como monitoramento de rede, console e JavaScript.
/// </summary>
public class BiDiHandler
{
    private readonly IWebDriver? _driver;
    private readonly ExtentTest? _test;
    private DevToolsSession? _session;
    private bool _isNetworkMonitoringEnabled;
    private bool _isConsoleMonitoringEnabled;
    private bool _isPerformanceMonitoringEnabled;
    
    // Coleção para armazenar requisições de rede
    private readonly ConcurrentBag<NetworkRequest> _networkRequests = new();
    
    // Coleção para armazenar mensagens de console
    private readonly ConcurrentBag<ConsoleMessage> _consoleMessages = new();
    
    // Coleção para armazenar métricas de performance
    private readonly ConcurrentBag<PerformanceMetric> _performanceMetrics = new();

    // Informações de reflexão para versão do DevTools
    private Type? _networkAdapterType;
    private Type? _consoleAdapterType;
    private Type? _performanceAdapterType;
    private Type? _runtimeAdapterType;
    
    /// <summary>
    /// Construtor para o BiDiHandler
    /// </summary>
    /// <param name="driver">WebDriver atual</param>
    /// <param name="test">ExtentTest para log (opcional)</param>
    public BiDiHandler(IWebDriver? driver, ExtentTest? test = null)
    {
        _driver = driver;
        _test = test;
        
        // Inicializar DevTools se o driver for compatível
        if (_driver is IDevTools devToolsDriver)
        {
            try
            {
                _session = devToolsDriver.GetDevToolsSession();
                LogInfo("Sessão DevTools inicializada com sucesso");
                
                // Detectar versões disponíveis do DevTools
                DetectDevToolsVersions();
            }
            catch (Exception ex)
            {
                LogWarning($"Não foi possível inicializar a sessão DevTools: {ex.Message}");
            }
        }
        else
        {
            LogWarning("O driver fornecido não suporta DevTools");
        }
    }
    
    /// <summary>
    /// Detecta as versões disponíveis do DevTools via reflexão
    /// </summary>
    private void DetectDevToolsVersions()
    {
        try
        {
            // Encontrar namespaces de versão do DevTools (V85, V112, V113, etc.)
            var seleniumAssembly = typeof(IDevTools).Assembly;
            var devToolsVersions = new List<string>();
            
            // Procurar versões disponíveis no namespace DevTools
            foreach (var type in seleniumAssembly.GetTypes())
            {
                if (type.Namespace != null && type.Namespace.StartsWith("OpenQA.Selenium.DevTools.V"))
                {
                    var version = type.Namespace.Split('.')[3]; // OpenQA.Selenium.DevTools.V120
                    if (!devToolsVersions.Contains(version))
                    {
                        devToolsVersions.Add(version);
                    }
                }
            }
            
            // Ordenar versões em ordem decrescente para usar a mais recente
            devToolsVersions.Sort((a, b) => 
            {
                if (int.TryParse(a.Substring(1), out int versionA) && 
                    int.TryParse(b.Substring(1), out int versionB))
                {
                    return versionB.CompareTo(versionA); // Ordem decrescente
                }
                return 0;
            });
            
            if (devToolsVersions.Count > 0)
            {
                LogInfo($"Versões do DevTools encontradas: {string.Join(", ", devToolsVersions)}");
                LogInfo($"Usando versão: {devToolsVersions[0]}");
                
                // Obter tipos dos adaptadores para a versão mais recente
                string latestVersion = devToolsVersions[0];
                
                _networkAdapterType = seleniumAssembly.GetType($"OpenQA.Selenium.DevTools.{latestVersion}.Network.NetworkAdapter");
                _consoleAdapterType = seleniumAssembly.GetType($"OpenQA.Selenium.DevTools.{latestVersion}.Console.ConsoleAdapter");
                _performanceAdapterType = seleniumAssembly.GetType($"OpenQA.Selenium.DevTools.{latestVersion}.Performance.PerformanceAdapter");
                _runtimeAdapterType = seleniumAssembly.GetType($"OpenQA.Selenium.DevTools.{latestVersion}.Runtime.RuntimeAdapter");
                
                if (_networkAdapterType != null && _consoleAdapterType != null && 
                    _performanceAdapterType != null && _runtimeAdapterType != null)
                {
                    LogInfo($"Todos os adaptadores encontrados para versão {latestVersion}");
                }
                else
                {
                    LogWarning($"Alguns adaptadores não foram encontrados para versão {latestVersion}");
                }
            }
            else
            {
                LogWarning("Nenhuma versão do DevTools encontrada");
            }
        }
        catch (Exception ex)
        {
            LogError($"Erro ao detectar versões do DevTools: {ex.Message}");
        }
    }

    /// <summary>
    /// Inicia o monitoramento de requisições de rede
    /// </summary>
    public void EnableNetworkMonitoring()
    {
        if (_session == null || _networkAdapterType == null)
        {
            LogWarning("Sessão DevTools ou adaptador de rede não disponível");
            return;
        }

        try
        {
            // Limpar requisições anteriores
            _networkRequests.Clear();
            
            // Obter adaptador de rede por reflexão
            var getVersionSpecificDomains = _session.GetType().GetMethod("GetVersionSpecificDomains")
                ?.MakeGenericMethod(_networkAdapterType);
            
            if (getVersionSpecificDomains == null)
            {
                LogWarning("Método GetVersionSpecificDomains não encontrado");
                return;
            }
            
            var network = getVersionSpecificDomains.Invoke(_session, null);
            if (network == null)
            {
                LogWarning("Não foi possível criar adaptador de rede");
                return;
            }
            
            // Habilitar o domínio Network
            var enableMethod = _networkAdapterType.GetMethod("Enable");
            var enableSettingsType = _networkAdapterType.Assembly.GetType(
                _networkAdapterType.Namespace + ".EnableCommandSettings");
            
            if (enableMethod != null && enableSettingsType != null)
            {
                var enableSettings = Activator.CreateInstance(enableSettingsType);
                enableMethod.Invoke(network, [enableSettings]);
                
                // Registrar eventos usando reflexão (esta parte é complexa, usaremos uma abordagem simplificada)
                RegisterNetworkEvents(network);
                
                _isNetworkMonitoringEnabled = true;
                LogInfo("Monitoramento de rede ativado com sucesso");
            }
            else
            {
                LogWarning("Métodos ou tipos necessários para monitoramento de rede não encontrados");
            }
        }
        catch (Exception ex)
        {
            LogError($"Erro ao ativar monitoramento de rede: {ex.Message}");
        }
    }

    /// <summary>
    /// Implementação simplificada do registro de eventos de rede
    /// </summary>
    private void RegisterNetworkEvents(object network)
    {
        try
        {
            // Obter tipos e eventos necessários por reflexão
            var requestWillBeSentEventInfo = _networkAdapterType?.GetEvent("RequestWillBeSent");
            var responseReceivedEventInfo = _networkAdapterType?.GetEvent("ResponseReceived");
            var loadingFailedEventInfo = _networkAdapterType?.GetEvent("LoadingFailed");
            
            if (requestWillBeSentEventInfo == null || responseReceivedEventInfo == null || loadingFailedEventInfo == null)
            {
                LogWarning("Um ou mais eventos de rede não foram encontrados");
                return;
            }
            
            // Obter tipos de argumentos para os eventos
            var requestWillBeSentArgsType = requestWillBeSentEventInfo.EventHandlerType?.GetMethod("Invoke")?.GetParameters()[1].ParameterType;
            var responseReceivedArgsType = responseReceivedEventInfo.EventHandlerType?.GetMethod("Invoke")?.GetParameters()[1].ParameterType;
            var loadingFailedArgsType = loadingFailedEventInfo.EventHandlerType?.GetMethod("Invoke")?.GetParameters()[1].ParameterType;
            
            if (requestWillBeSentArgsType == null || responseReceivedArgsType == null || loadingFailedArgsType == null)
            {
                LogWarning("Não foi possível determinar os tipos de argumentos dos eventos");
                return;
            }
            
            // Criar delegates tipados usando Reflection.Emit
            var requestWillBeSentHandler = CreateEventHandler(requestWillBeSentEventInfo.EventHandlerType, requestWillBeSentArgsType, 
                (sender, args) => HandleRequestWillBeSent(args));
                
            var responseReceivedHandler = CreateEventHandler(responseReceivedEventInfo.EventHandlerType, responseReceivedArgsType, 
                (sender, args) => HandleResponseReceived(args));
                
            var loadingFailedHandler = CreateEventHandler(loadingFailedEventInfo.EventHandlerType, loadingFailedArgsType, 
                (sender, args) => HandleLoadingFailed(args));
            
            // Registrar os handlers aos eventos
            requestWillBeSentEventInfo.AddEventHandler(network, requestWillBeSentHandler);
            responseReceivedEventInfo.AddEventHandler(network, responseReceivedHandler);
            loadingFailedEventInfo.AddEventHandler(network, loadingFailedHandler);
            
            LogInfo("Eventos de rede registrados com sucesso");
        }
        catch (Exception ex)
        {
            LogError($"Erro ao registrar eventos de rede: {ex.Message}");
        }
    }
    private Delegate CreateEventHandler(Type? eventHandlerType, Type eventArgsType, Action<object, object?> handler)
    {
        // Aqui usaríamos Expression Trees ou Reflection.Emit para criar um delegate
        // compatível com o tipo do evento. Simplificando, vamos usar DynamicInvoke.
    
        return Delegate.CreateDelegate(eventHandlerType!, this, 
            GetType().GetMethod("DynamicEventHandler", 
                    BindingFlags.NonPublic | BindingFlags.Instance)
                !.MakeGenericMethod(eventArgsType));
    }
    
    private void DynamicEventHandler<T>(object sender, T? args)
    {
        var argsType = typeof(T);
    
        // Determinar qual handler chamar com base no tipo do argumento
        if (argsType.Name.Contains("RequestWillBeSent"))
            HandleRequestWillBeSent(args);
        else if (argsType.Name.Contains("ResponseReceived"))
            HandleResponseReceived(args);
        else if (argsType.Name.Contains("LoadingFailed"))
            HandleLoadingFailed(args);
    }
    
    private void HandleRequestWillBeSent(object? args)
    {
        try
        {
            // Extrair dados do evento via reflexão
            var requestProperty = args?.GetType().GetProperty("Request");
            var requestIdProperty = args?.GetType().GetProperty("RequestId");
            var timestampProperty = args?.GetType().GetProperty("Timestamp");
            var typeProperty = args?.GetType().GetProperty("Type");
        
            if (requestProperty == null || requestIdProperty == null) return;
        
            var request = requestProperty.GetValue(args);
            var urlProperty = request?.GetType().GetProperty("Url");
            var methodProperty = request?.GetType().GetProperty("Method");
        
            if (urlProperty == null || methodProperty == null) return;
        
            var requestId = requestIdProperty.GetValue(args)?.ToString() ?? string.Empty;
            var url = urlProperty.GetValue(request)?.ToString() ?? string.Empty;
            var method = methodProperty.GetValue(request)?.ToString() ?? string.Empty;
            var resourceType = typeProperty?.GetValue(args)?.ToString() ?? string.Empty;
        
            // Criar objeto NetworkRequest e adicionar à coleção
            var networkRequest = new NetworkRequest
            {
                RequestId = requestId,
                Url = url,
                Method = method,
                Timestamp = DateTime.Now,
                ResourceType = resourceType
            };
        
            _networkRequests.Add(networkRequest);
            LogInfo($"Requisição capturada: {method} {url}");
        }
        catch (Exception ex)
        {
            LogWarning($"Erro ao processar RequestWillBeSent: {ex.Message}");
        }
    }
    
    private void HandleResponseReceived(object? args)
    {
        try
        {
            // Extrair dados do evento via reflexão
            var requestIdProperty = args.GetType().GetProperty("RequestId");
            var responseProperty = args.GetType().GetProperty("Response");
        
            if (requestIdProperty == null || responseProperty == null) return;
        
            var requestId = requestIdProperty.GetValue(args)?.ToString() ?? string.Empty;
            var response = responseProperty.GetValue(args);
        
            if (response == null) return;
        
            var statusProperty = response.GetType().GetProperty("Status");
            var statusTextProperty = response.GetType().GetProperty("StatusText");
            var mimeTypeProperty = response.GetType().GetProperty("MimeType");
        
            if (statusProperty == null) return;
        
            var status = statusProperty.GetValue(response)?.ToString() ?? string.Empty;
            var statusText = statusTextProperty?.GetValue(response)?.ToString() ?? string.Empty;
            var mimeType = mimeTypeProperty?.GetValue(response)?.ToString() ?? string.Empty;
        
            // Encontrar a requisição correspondente e atualizar
            var request = _networkRequests.FirstOrDefault(r => r.RequestId == requestId);
            if (request == null) return;
            request.Status = status;
            request.StatusText = statusText;
            request.ResponseTime = DateTime.Now;
            request.MimeType = mimeType;
            
            LogInfo($"Resposta recebida: {request.Method} {request.Url} - Status: {status}");
        }
        catch (Exception ex)
        {
            LogWarning($"Erro ao processar ResponseReceived: {ex.Message}");
        }
    }
    
    private void HandleLoadingFailed(object? args)
    {
        try
        {
            // Extrair dados do evento via reflexão
            var requestIdProperty = args.GetType().GetProperty("RequestId");
            var errorTextProperty = args.GetType().GetProperty("ErrorText");
        
            if (requestIdProperty == null || errorTextProperty == null) return;
        
            var requestId = requestIdProperty.GetValue(args)?.ToString() ?? string.Empty;
            var errorText = errorTextProperty.GetValue(args)?.ToString() ?? "Unknown error";
        
            // Encontrar a requisição correspondente e atualizar
            var request = _networkRequests.FirstOrDefault(r => r.RequestId == requestId);
            if (request == null) return;
            request.Status = "Failed";
            request.StatusText = errorText;
            request.ResponseTime = DateTime.Now;
            
            LogWarning($"Falha no carregamento: {request.Method} {request.Url} - Erro: {errorText}");
        }
        catch (Exception ex)
        {
            LogWarning($"Erro ao processar LoadingFailed: {ex.Message}");
        }
    }

    /// <summary>
    /// Método simplificado para inativar o monitoramento de rede
    /// </summary>
    public void DisableNetworkMonitoring()
    {
        try
        {
            if (_session == null || _networkAdapterType == null || !_isNetworkMonitoringEnabled) return;
            var getVersionSpecificDomains = _session.GetType().GetMethod("GetVersionSpecificDomains")
                ?.MakeGenericMethod(_networkAdapterType);

            if (getVersionSpecificDomains == null) return;
            var network = getVersionSpecificDomains.Invoke(_session, null);
            var disableMethod = _networkAdapterType.GetMethod("Disable");

            if (network == null || disableMethod == null) return;
            disableMethod.Invoke(network, null);
            _isNetworkMonitoringEnabled = false;
            LogInfo("Monitoramento de rede desativado com sucesso");
        }
        catch (Exception ex)
        {
            LogWarning($"Erro ao desativar monitoramento de rede: {ex.Message}");
        }
    }
    
    public void EnableConsoleMonitoring()
{
    if (_session == null || _consoleAdapterType == null)
    {
        LogWarning("Sessão DevTools ou adaptador de console não disponível");
        return;
    }

    try
    {
        // Limpar mensagens anteriores
        _consoleMessages.Clear();
        
        // Obter adaptador de console por reflexão
        var getVersionSpecificDomains = _session.GetType().GetMethod("GetVersionSpecificDomains")
            ?.MakeGenericMethod(_consoleAdapterType);
        
        if (getVersionSpecificDomains == null)
        {
            LogWarning("Método GetVersionSpecificDomains não encontrado para console");
            return;
        }
        
        var console = getVersionSpecificDomains.Invoke(_session, null);
        if (console == null)
        {
            LogWarning("Não foi possível criar adaptador de console");
            return;
        }
        
        // Habilitar o domínio Console
        var enableMethod = _consoleAdapterType.GetMethod("Enable");
        
        if (enableMethod != null)
        {
            enableMethod.Invoke(console, null);
            
            // Registrar evento MessageAdded
            var messageAddedEvent = _consoleAdapterType.GetEvent("MessageAdded");
            
            if (messageAddedEvent != null)
            {
                var messageAddedArgsType = messageAddedEvent.EventHandlerType?.GetMethod("Invoke")?.GetParameters()[1].ParameterType;
                
                if (messageAddedArgsType != null)
                {
                    var messageAddedHandler = CreateEventHandler(messageAddedEvent.EventHandlerType, messageAddedArgsType,
                        (sender, args) => HandleConsoleMessageAdded(args));
                    
                    messageAddedEvent.AddEventHandler(console, messageAddedHandler);
                    
                    _isConsoleMonitoringEnabled = true;
                    LogInfo("Monitoramento de console ativado com sucesso");
                }
                else
                {
                    LogWarning("Tipo de argumento para evento MessageAdded não encontrado");
                }
            }
            else
            {
                LogWarning("Evento MessageAdded não encontrado");
            }
        }
        else
        {
            LogWarning("Método Enable não encontrado para console");
        }
    }
    catch (Exception ex)
    {
        LogError($"Erro ao ativar monitoramento de console: {ex.Message}");
    }
}
    private void HandleConsoleMessageAdded(object? args)
{
    try
    {
        // Extrair dados da mensagem por reflexão
        var messageProperty = args?.GetType().GetProperty("Message");
        
        if (messageProperty == null) return;
        
        var message = messageProperty.GetValue(args);
        if (message == null) return;
        
        var textProperty = message.GetType().GetProperty("Text");
        var levelProperty = message.GetType().GetProperty("Level");
        var urlProperty = message.GetType().GetProperty("Url");
        var lineProperty = message.GetType().GetProperty("Line");
        
        if (textProperty == null || levelProperty == null) return;
        
        var text = textProperty.GetValue(message)?.ToString() ?? string.Empty;
        var level = levelProperty.GetValue(message)?.ToString() ?? "Info";
        var url = urlProperty?.GetValue(message)?.ToString() ?? string.Empty;
        long line = 0;
        
        if (lineProperty != null)
        {
            var lineValue = lineProperty.GetValue(message);
            if (lineValue != null)
            {
                long.TryParse(lineValue.ToString(), out line);
            }
        }
        
        // Criar mensagem de console e adicionar à coleção
        var consoleMessage = new ConsoleMessage
        {
            Text = text,
            Level = level,
            Url = url,
            LineNumber = line,
            Timestamp = DateTime.Now
        };
        
        _consoleMessages.Add(consoleMessage);
        
        // Log de acordo com o nível
        switch (level.ToLower())
        {
            case "error":
                LogError($"Console Error: {text}");
                break;
            case "warning":
                LogWarning($"Console Warning: {text}");
                break;
            default:
                LogInfo($"Console: {text}");
                break;
        }
    }
    catch (Exception ex)
    {
        LogWarning($"Erro ao processar mensagem de console: {ex.Message}");
    }
}
    public void DisableConsoleMonitoring()
    {
        try
        {
            if (_session != null && _consoleAdapterType != null && _isConsoleMonitoringEnabled)
            {
                var getVersionSpecificDomains = _session.GetType().GetMethod("GetVersionSpecificDomains")
                    ?.MakeGenericMethod(_consoleAdapterType);
            
                if (getVersionSpecificDomains != null)
                {
                    var console = getVersionSpecificDomains.Invoke(_session, null);
                    var disableMethod = _consoleAdapterType.GetMethod("Disable");
                
                    if (console != null && disableMethod != null)
                    {
                        disableMethod.Invoke(console, null);
                        _isConsoleMonitoringEnabled = false;
                        LogInfo("Monitoramento de console desativado com sucesso");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogWarning($"Erro ao desativar monitoramento de console: {ex.Message}");
        }
    }
    public void EnablePerformanceMonitoring()
{
    if (_session == null || _performanceAdapterType == null)
    {
        LogWarning("Sessão DevTools ou adaptador de performance não disponível");
        return;
    }

    try
    {
        // Limpar métricas anteriores
        _performanceMetrics.Clear();
        
        // Obter adaptador de performance por reflexão
        var getVersionSpecificDomains = _session.GetType().GetMethod("GetVersionSpecificDomains")
            ?.MakeGenericMethod(_performanceAdapterType);
        
        if (getVersionSpecificDomains == null)
        {
            LogWarning("Método GetVersionSpecificDomains não encontrado para performance");
            return;
        }
        
        var performance = getVersionSpecificDomains.Invoke(_session, null);
        if (performance == null)
        {
            LogWarning("Não foi possível criar adaptador de performance");
            return;
        }
        
        // Habilitar o domínio Performance
        var enableMethod = _performanceAdapterType.GetMethod("Enable");
        
        if (enableMethod != null)
        {
            enableMethod.Invoke(performance, null);
            
            // Configurar coleta automática de métricas
            var setTimeDomainMethod = _performanceAdapterType.GetMethod("SetTimeDomain");
            if (setTimeDomainMethod != null)
            {
                // Precisamos encontrar o tipo de configuração e criar uma instância
                var timeDomainSettingsType = _performanceAdapterType.Assembly.GetType(
                    _performanceAdapterType.Namespace + ".SetTimeDomainCommandSettings");
                
                if (timeDomainSettingsType != null)
                {
                    var settings = Activator.CreateInstance(timeDomainSettingsType);
                    
                    // Definir propriedade TimeDomain como "threadTicks"
                    var timeDomainProperty = timeDomainSettingsType.GetProperty("TimeDomain");
                    if (timeDomainProperty != null)
                    {
                        timeDomainProperty.SetValue(settings, "threadTicks");
                        setTimeDomainMethod.Invoke(performance, [settings]);
                    }
                }
            }
            
            // Registrar evento MetricsReceived
            var metricsReceivedEvent = _performanceAdapterType.GetEvent("MetricsReceived");
            
            if (metricsReceivedEvent != null)
            {
                var metricsReceivedArgsType = metricsReceivedEvent.EventHandlerType?.GetMethod("Invoke")?.GetParameters()[1].ParameterType;
                
                if (metricsReceivedArgsType != null)
                {
                    var metricsReceivedHandler = CreateEventHandler(metricsReceivedEvent.EventHandlerType, metricsReceivedArgsType,
                        (sender, args) => HandleMetricsReceived(args));
                    
                    metricsReceivedEvent.AddEventHandler(performance, metricsReceivedHandler);
                    
                    // Iniciar coleta periódica de métricas
                    var getMetricsMethod = _performanceAdapterType.GetMethod("GetMetrics");
                    if (getMetricsMethod != null)
                    {
                        // Criar timer para coletar métricas a cada 2 segundos
                        var timer = new System.Threading.Timer(_ =>
                        {
                            try
                            {
                                getMetricsMethod.Invoke(performance, null);
                            }
                            catch (Exception ex)
                            {
                                LogWarning($"Erro ao coletar métricas: {ex.Message}");
                            }
                        }, null, 0, 2000);
                    }
                    
                    _isPerformanceMonitoringEnabled = true;
                    LogInfo("Monitoramento de performance ativado com sucesso");
                }
                else
                {
                    LogWarning("Tipo de argumento para evento MetricsReceived não encontrado");
                }
            }
            else
            {
                LogWarning("Evento MetricsReceived não encontrado");
            }
        }
        else
        {
            LogWarning("Método Enable não encontrado para performance");
        }
    }
    catch (Exception ex)
    {
        LogError($"Erro ao ativar monitoramento de performance: {ex.Message}");
    }
}
    private void HandleMetricsReceived(object? args)
    {
        try
        {
            // Extrair dados das métricas por reflexão
            var metricsProperty = args?.GetType().GetProperty("Metrics");
        
            if (metricsProperty == null) return;
        
            var metrics = metricsProperty.GetValue(args) as System.Collections.IEnumerable;
            if (metrics == null) return;
        
            foreach (var metric in metrics)
            {
                var nameProperty = metric.GetType().GetProperty("Name");
                var valueProperty = metric.GetType().GetProperty("Value");
            
                if (nameProperty == null || valueProperty == null) continue;
            
                var name = nameProperty.GetValue(metric)?.ToString() ?? string.Empty;
                var value = valueProperty.GetValue(metric)?.ToString() ?? string.Empty;
            
                // Criar métrica e adicionar à coleção
                var performanceMetric = new PerformanceMetric
                {
                    Name = name,
                    Value = value,
                    Timestamp = DateTime.Now
                };
            
                _performanceMetrics.Add(performanceMetric);
                LogInfo($"Métrica: {name} = {value}");
            }
        }
        catch (Exception ex)
        {
            LogWarning($"Erro ao processar métricas: {ex.Message}");
        }
    }
    public void DisablePerformanceMonitoring()
    {
        try
        {
            if (_session == null || _performanceAdapterType == null || !_isPerformanceMonitoringEnabled) return;
            var getVersionSpecificDomains = _session.GetType().GetMethod("GetVersionSpecificDomains")
                ?.MakeGenericMethod(_performanceAdapterType);

            if (getVersionSpecificDomains == null) return;
            var performance = getVersionSpecificDomains.Invoke(_session, null);
            var disableMethod = _performanceAdapterType.GetMethod("Disable");

            if (performance == null || disableMethod == null) return;
            disableMethod.Invoke(performance, null);
            _isPerformanceMonitoringEnabled = false;
            LogInfo("Monitoramento de performance desativado com sucesso");
        }
        catch (Exception ex)
        {
            LogWarning($"Erro ao desativar monitoramento de performance: {ex.Message}");
        }
    }
    
    public void DisableAllMonitoring()
    {
        DisableNetworkMonitoring();
        DisableConsoleMonitoring();
        DisablePerformanceMonitoring();
        LogInfo("Todos os monitoramentos foram desativados");
    }
    public void CaptureErrorScreenshots(string testName)
    {
        // Verificar se existem erros de console
        var errors = _consoleMessages.Where(m => m.Level.ToLower() == "error").ToList();
    
        if (errors.Count > 0)
        {
            LogWarning($"Encontrados {errors.Count} erros de JavaScript/console");
            CaptureScreenshot($"{testName}_JSError");
        
            // Registrar detalhes dos erros
            foreach (var error in errors)
            {
                LogError($"Erro JS: {error.Text} - {error.Url}:{error.LineNumber}");
            }
        }
    
        // Verificar se existem falhas de rede
        var failedRequests = _networkRequests.Where(r => r.Status == "Failed" || 
                                                         (int.TryParse(r.Status, out int statusCode) && statusCode >= 400)).ToList();
    
        if (failedRequests.Count > 0)
        {
            LogWarning($"Encontrados {failedRequests.Count} erros de rede");
            CaptureScreenshot($"{testName}_NetworkError");
        
            // Registrar detalhes das falhas
            foreach (var request in failedRequests)
            {
                LogError($"Erro Rede: {request.Method} {request.Url} - Status: {request.Status} {request.StatusText}");
            }
        }
    }
    
    public void AddInfoToReport()
{
    if (_test == null) return;
    
    try
    {
        // Adicionar métricas de performance, se disponíveis
        if (_performanceMetrics.Count > 0)
        {
            var metrics = _performanceMetrics
                .GroupBy(m => m.Name)
                .Select(g => new
                {
                    Name = g.Key,
                    LastValue = g.OrderByDescending(m => m.Timestamp).First().Value,
                    Min = g.Min(m => double.TryParse(g.First().Value, out double val) ? val : 0),
                    Max = g.Max(m => double.TryParse(g.First().Value, out double val) ? val : 0),
                    Count = g.Count()
                })
                .ToList();
            
            var metricsTable = metrics.Aggregate("<table border='1'><tr><th>Métrica</th><th>Valor</th><th>Min</th><th>Max</th></tr>", (current, metric) => current + $"<tr><td>{metric.Name}</td><td>{metric.LastValue}</td><td>{metric.Min}</td><td>{metric.Max}</td></tr>");
            metricsTable += "</table>";
            
            _test.Info(metricsTable);
        }
        
        // Adicionar resumo de requisições de rede
        if (_networkRequests.Count > 0)
        {
            var totalRequests = _networkRequests.Count;
            var successRequests = _networkRequests.Count(r => int.TryParse(r.Status, out var status) && status is >= 200 and < 400);
            var failedRequests = _networkRequests.Count(r => r.Status == "Failed" || 
                (int.TryParse(r.Status, out int status) && status >= 400));
            var pendingRequests = totalRequests - successRequests - failedRequests;
            
            _test.Info($"<div>Requisições: {totalRequests} total | {successRequests} sucesso | " +
                       $"{failedRequests} falhas | {pendingRequests} pendentes</div>");
            
            // Detalhar falhas se houver
            if (failedRequests > 0)
            {
                var failedRequestsList = _networkRequests
                    .Where(r => r.Status == "Failed" || 
                        (int.TryParse(r.Status, out int statusCode) && statusCode >= 400))
                    .OrderByDescending(r => r.Timestamp)
                    .ToList();
                
                var failuresTable = failedRequestsList.Aggregate("<table border='1'><tr><th>URL</th><th>Status</th><th>Erro</th></tr>", (current, request) => current + $"<tr><td>{request.Url}</td><td>{request.Status}</td><td>{request.StatusText}</td></tr>");
                failuresTable += "</table>";
                
                _test.Warning(failuresTable);
            }
        }
        
        // Adicionar mensagens de console
        if (_consoleMessages.Count <= 0) return;
        {
            var errorCount = _consoleMessages.Count(m => m.Level.ToLower() == "error");
            var warningCount = _consoleMessages.Count(m => m.Level.ToLower() == "warning");
            
            _test.Info($"<div>Console: {_consoleMessages.Count} mensagens | {errorCount} erros | {warningCount} avisos</div>");
            
            // Detalhar erros se houver
            if (errorCount <= 0) return;
            {
                var errorMessages = _consoleMessages
                    .Where(m => m.Level.ToLower() == "error")
                    .OrderByDescending(m => m.Timestamp)
                    .ToList();
                
                var errorsTable = errorMessages.Aggregate("<table border='1'><tr><th>Mensagem</th><th>URL</th><th>Linha</th></tr>", (current, message) => current + $"<tr><td>{message.Text}</td><td>{message.Url}</td><td>{message.LineNumber}</td></tr>");
                errorsTable += "</table>";
                
                _test.Error(errorsTable);
            }
        }
    }
    catch (Exception ex)
    {
        LogWarning($"Erro ao adicionar informações ao relatório: {ex.Message}");
    }
}

    /// <summary>
    /// Método para alternar entre implementação específica ou genérica
    /// </summary>
    public void UseSimpleImplementation()
    {
        // Este método serve como ponto de entrada para implementar uma versão
        // mais simples que não depende de versões específicas do DevTools
        
        if (_driver == null) return;
        
        try
        {
            // Por exemplo, podemos implementar captura de erros de JavaScript usando
            // o executor JavaScript padrão do WebDriver em vez dos DevTools:
            
            ((IJavaScriptExecutor)_driver).ExecuteScript(
                "window.addEventListener('error', function(e) { " +
                "  if (!window.__seleniumErrors) window.__seleniumErrors = []; " +
                "  window.__seleniumErrors.push({" +
                "    message: e.message," +
                "    url: e.filename," +
                "    line: e.lineno," +
                "    timestamp: new Date().toISOString()" +
                "  });" +
                "});"
            );
            
            LogInfo("Implementação simplificada de monitoramento ativada");
        }
        catch (Exception ex)
        {
            LogWarning($"Erro ao configurar implementação simplificada: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Coleta erros de JavaScript usando a implementação simplificada
    /// </summary>
    public List<ConsoleMessage> CollectJavaScriptErrors()
    {
        var errors = new List<ConsoleMessage>();
        
        if (_driver == null) return errors;
        
        try
        {
            if (((IJavaScriptExecutor)_driver).ExecuteScript(
                    "return window.__seleniumErrors || [];"
                ) is IEnumerable jsErrors)
            {
                errors.AddRange(from Dictionary<string, object> error in jsErrors
                select new ConsoleMessage
                {
                    Level = "Error",
                    Text = error["message"]?.ToString() ?? "Unknown error",
                    Url = error["url"]?.ToString() ?? "",
                    LineNumber = Convert.ToInt64(error["line"] ?? 0),
                    Timestamp = DateTime.Now
                });
            }
        }
        catch (Exception ex)
        {
            LogWarning($"Erro ao coletar erros de JavaScript: {ex.Message}");
        }
        
        return errors;
    }

    /// <summary>
    /// Função para testar a conectividade com o DevTools
    /// </summary>
    public bool TestDevToolsConnectivity()
    {
        if (_driver is not IDevTools tools) return false;
        
        try
        {
            using var session = tools.GetDevToolsSession();
            LogInfo("Conectividade com DevTools testada com sucesso");
            return true;
        }
        catch (Exception ex)
        {
            LogWarning($"Falha ao conectar com DevTools: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Captura screenshot em caso de erro
    /// </summary>
    public string CaptureScreenshot(string prefix)
    {
        if (_driver == null) return string.Empty;
        
        try
        {
            var screenshot = ((ITakesScreenshot)_driver).GetScreenshot();
            var screenshotDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshots");
            
            if (!Directory.Exists(screenshotDir))
            {
                Directory.CreateDirectory(screenshotDir);
            }
            
            var screenshotPath = Path.Combine(
                screenshotDir, 
                $"{prefix}_{DateTime.Now:yyyyMMdd_HHmmss}.png"
            );
            
            screenshot.SaveAsFile(screenshotPath);
            LogInfo($"Screenshot salvo em: {screenshotPath}");
            
            _test?.AddScreenCaptureFromPath(screenshotPath);
            
            return screenshotPath;
        }
        catch (Exception ex)
        {
            LogWarning($"Erro ao capturar screenshot: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Libera recursos
    /// </summary>
    public void Dispose()
    {
        try
        {
            DisableNetworkMonitoring();
            // Desativar outros monitoramentos...
            
            _session = null;
        }
        catch (Exception ex)
        {
            LogWarning($"Erro ao liberar recursos: {ex.Message}");
        }
    }

    #region Classes auxiliares para armazenamento de dados

    /// <summary>
    /// Representa uma requisição de rede capturada
    /// </summary>
    public class NetworkRequest
    {
        public string Url { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string RequestId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public DateTime? ResponseTime { get; set; }
        public string ResourceType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusText { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Representa uma mensagem de console capturada
    /// </summary>
    public class ConsoleMessage
    {
        public string Level { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Url { get; set; } = string.Empty;
        public long LineNumber { get; set; }
    }

    /// <summary>
    /// Representa uma métrica de performance capturada
    /// </summary>
    public class PerformanceMetric
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    #endregion

    #region Métodos de logging

    private void LogInfo(string message)
    {
        Console.WriteLine($"[BiDiHandler] INFO: {message}");
        _test?.Info($"[BiDi] {message}");
    }

    private void LogWarning(string message)
    {
        Console.WriteLine($"[BiDiHandler] AVISO: {message}");
        _test?.Warning($"[BiDi] {message}");
    }

    private void LogError(string message)
    {
        Console.WriteLine($"[BiDiHandler] ERRO: {message}");
        _test?.Error($"[BiDi] {message}");
    }

    #endregion
}