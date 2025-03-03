using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using AventStack.ExtentReports;
using OpenQA.Selenium;
using OpenQA.Selenium.DevTools;
using static System.Int64;

namespace SwagLabsAutomation.Utils;

/// <summary>
/// Manipulador de funcionalidades BiDi (Bidirecional) do Selenium WebDriver.
/// Fornece acesso a recursos como monitoramento de rede, console e JavaScript.
/// </summary>
public class BiDiHandler : IDisposable
{
    #region Propriedades e campos

    private readonly IWebDriver? _driver;
    private readonly ExtentTest? _test;
    private DevToolsSession? _session;
    private bool _isNetworkMonitoringEnabled;
    private bool _isConsoleMonitoringEnabled;
    private bool _isPerformanceMonitoringEnabled;
    
    // Coleções para armazenar dados de monitoramento
    private readonly ConcurrentBag<NetworkRequest> _networkRequests = new();
    private readonly ConcurrentBag<ConsoleMessage> _consoleMessages = new();
    private readonly ConcurrentBag<PerformanceMetric> _performanceMetrics = new();

    // Informações de reflexão para versão do DevTools
    private Type? _networkAdapterType;
    private Type? _consoleAdapterType;
    private Type? _performanceAdapterType;
    private Type? _runtimeAdapterType;

    // Timer para coleta periódica de métricas
    private Timer? _metricsTimer;

    #endregion

    #region Construtor e inicialização

    /// <summary>
    /// Construtor para o BiDiHandler
    /// </summary>
    /// <param name="driver">WebDriver atual</param>
    /// <param name="test">ExtentTest para log (opcional)</param>
    public BiDiHandler(IWebDriver? driver, ExtentTest? test = null)
    {
        _driver = driver;
        _test = test;
        
        InitializeDevToolsSession();
    }

    /// <summary>
    /// Inicializa a sessão DevTools se possível
    /// </summary>
    private void InitializeDevToolsSession()
    {
        if (_driver is not IDevTools devToolsDriver)
        {
            LogWarning("O driver fornecido não suporta DevTools");
            return;
        }

        try
        {
            _session = devToolsDriver.GetDevToolsSession();
            LogInfo("Sessão DevTools inicializada com sucesso");
            
            DetectDevToolsVersions();
        }
        catch (Exception ex)
        {
            LogWarning($"Não foi possível inicializar a sessão DevTools: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Detecta as versões disponíveis do DevTools via reflexão
    /// </summary>
    private void DetectDevToolsVersions()
    {
        try
        {
            var seleniumAssembly = typeof(IDevTools).Assembly;
            var devToolsVersions = GetDevToolsVersions(seleniumAssembly);
            
            if (devToolsVersions.Count == 0)
            {
                LogWarning("Nenhuma versão do DevTools encontrada");
                return;
            }
            
            // Obter versão mais recente
            string latestVersion = devToolsVersions[0];
            LogInfo($"Versões do DevTools encontradas: {string.Join(", ", devToolsVersions)}");
            LogInfo($"Usando versão: {latestVersion}");
            
            // Inicializar tipos de adaptadores
            InitializeAdapterTypes(seleniumAssembly, latestVersion);
        }
        catch (Exception ex)
        {
            LogError($"Erro ao detectar versões do DevTools: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtém as versões disponíveis do DevTools em ordem decrescente
    /// </summary>
    private static List<string> GetDevToolsVersions(Assembly seleniumAssembly)
    {
        var devToolsVersions = new List<string>();
        
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
        
        // Ordenar versões em ordem decrescente
        devToolsVersions.Sort((a, b) => 
        {
            if (int.TryParse(a.AsSpan(1), out var versionA) && 
                int.TryParse(b.AsSpan(1), out var versionB))
            {
                return versionB.CompareTo(versionA);
            }
            return 0;
        });
        
        return devToolsVersions;
    }

    /// <summary>
    /// Inicializa os tipos de adaptadores para a versão específica do DevTools
    /// </summary>
    private void InitializeAdapterTypes(Assembly seleniumAssembly, string version)
    {
        _networkAdapterType = seleniumAssembly.GetType($"OpenQA.Selenium.DevTools.{version}.Network.NetworkAdapter");
        _consoleAdapterType = seleniumAssembly.GetType($"OpenQA.Selenium.DevTools.{version}.Console.ConsoleAdapter");
        _performanceAdapterType = seleniumAssembly.GetType($"OpenQA.Selenium.DevTools.{version}.Performance.PerformanceAdapter");
        _runtimeAdapterType = seleniumAssembly.GetType($"OpenQA.Selenium.DevTools.{version}.Runtime.RuntimeAdapter");
    
        bool allTypesFound = _networkAdapterType != null && _consoleAdapterType != null && 
                             _performanceAdapterType != null && _runtimeAdapterType != null;
    
        if (allTypesFound)
        {
            LogInfo($"Todos os adaptadores encontrados para versão {version}");
            FindNetworkInterface(seleniumAssembly, version);
        }
        else
        {
            LogWarning($"Alguns adaptadores não foram encontrados para versão {version}");
        }
    }

    #endregion

    #region Monitoramento de Rede

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
            _networkRequests.Clear();
    // Aqui está a correção: use a interface INetwork
    var network = GetVersionSpecificDomain<object>(_session, _networkAdapterType);    if (network == null) return;
            // Habilitar o domínio Network
            EnableNetworkDomain(network);
        
            // Registrar eventos
            if (RegisterNetworkEvents(network))
            {
                _isNetworkMonitoringEnabled = true;
                LogInfo("Monitoramento de rede ativado com sucesso");
            }
        }
        catch (Exception ex)
        {
            LogError($"Erro ao ativar monitoramento de rede: {ex.Message}");
        }
    }
    
    private void FindNetworkInterface(Assembly seleniumAssembly, string version)
    {
        var networkNamespace = $"OpenQA.Selenium.DevTools.{version}.Network";

        foreach (var type in seleniumAssembly.GetTypes())
        {
            if (type.Namespace == null || !type.Namespace.StartsWith(networkNamespace) || !type.IsInterface ||
                !type.Name.StartsWith("I")) continue;
            LogInfo($"Interface de rede encontrada: {type.FullName}");
            var interfaceType = type.FullName;
            if (interfaceType == null) continue;
            var genericDomain = GetVersionSpecificDomain<object>(_session, Type.GetType(interfaceType)!);
            if (genericDomain == null) continue;
            LogInfo($"Domínio de rede inicializado usando {interfaceType}");
            EnableNetworkDomain(genericDomain);
        }
    }

    /// <summary>
    /// Habilita o domínio Network do DevTools
    /// </summary>
    private void EnableNetworkDomain(object network)
    {
        var enableMethod = _networkAdapterType!.GetMethod("Enable");
        var enableSettingsType = _networkAdapterType.Assembly.GetType(
            _networkAdapterType.Namespace + ".EnableCommandSettings");
        
        if (enableMethod == null || enableSettingsType == null)
        {
            LogWarning("Métodos ou tipos necessários para monitoramento de rede não encontrados");
            return;
        }
        
        var enableSettings = Activator.CreateInstance(enableSettingsType);
        enableMethod.Invoke(network, [enableSettings]);
    }

    /// <summary>
    /// Registra eventos para monitoramento de rede
    /// </summary>
    private bool RegisterNetworkEvents(object network)
    {
        try
        {
            var requestWillBeSentEventInfo = _networkAdapterType?.GetEvent("RequestWillBeSent");
            var responseReceivedEventInfo = _networkAdapterType?.GetEvent("ResponseReceived");
            var loadingFailedEventInfo = _networkAdapterType?.GetEvent("LoadingFailed");
            
            if (requestWillBeSentEventInfo == null || responseReceivedEventInfo == null || loadingFailedEventInfo == null)
            {
                LogWarning("Um ou mais eventos de rede não foram encontrados");
                return false;
            }
            
            // Obter tipos de argumentos para os eventos
            var requestWillBeSentArgsType = requestWillBeSentEventInfo.EventHandlerType?.GetMethod("Invoke")?.GetParameters()[1].ParameterType;
            var responseReceivedArgsType = responseReceivedEventInfo.EventHandlerType?.GetMethod("Invoke")?.GetParameters()[1].ParameterType;
            var loadingFailedArgsType = loadingFailedEventInfo.EventHandlerType?.GetMethod("Invoke")?.GetParameters()[1].ParameterType;
            
            if (requestWillBeSentArgsType == null || responseReceivedArgsType == null || loadingFailedArgsType == null)
            {
                LogWarning("Não foi possível determinar os tipos de argumentos dos eventos");
                return false;
            }
            
            // Criar e registrar handlers para eventos
            var requestWillBeSentHandler = CreateEventHandler(requestWillBeSentEventInfo.EventHandlerType!, requestWillBeSentArgsType, 
                (sender, args) => HandleRequestWillBeSent(args));
                
            var responseReceivedHandler = CreateEventHandler(responseReceivedEventInfo.EventHandlerType!, responseReceivedArgsType, 
                (sender, args) => HandleResponseReceived(args));
                
            var loadingFailedHandler = CreateEventHandler(loadingFailedEventInfo.EventHandlerType!, loadingFailedArgsType, 
                (sender, args) => HandleLoadingFailed(args));
            
            requestWillBeSentEventInfo.AddEventHandler(network, requestWillBeSentHandler);
            responseReceivedEventInfo.AddEventHandler(network, responseReceivedHandler);
            loadingFailedEventInfo.AddEventHandler(network, loadingFailedHandler);
            
            LogInfo("Eventos de rede registrados com sucesso");
            return true;
        }
        catch (Exception ex)
        {
            LogError($"Erro ao registrar eventos de rede: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Manipula evento RequestWillBeSent do DevTools
    /// </summary>
    private void HandleRequestWillBeSent(object? args)
    {
        try
        {
            // Extrair dados do evento via reflexão
            var requestProperty = args?.GetType().GetProperty("Request");
            var requestIdProperty = args?.GetType().GetProperty("RequestId");
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

    /// <summary>
    /// Manipula evento ResponseReceived do DevTools
    /// </summary>
    private void HandleResponseReceived(object? args)
    {
        if (args == null) return;
        
        try
        {
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

    /// <summary>
    /// Manipula evento LoadingFailed do DevTools
    /// </summary>
    private void HandleLoadingFailed(object? args)
    {
        if (args == null) return;
        
        try
        {
            var requestIdProperty = args.GetType().GetProperty("RequestId");
            var errorTextProperty = args.GetType().GetProperty("ErrorText");
        
            if (requestIdProperty == null || errorTextProperty == null) return;
        
            var requestId = requestIdProperty.GetValue(args)?.ToString() ?? string.Empty;
            var errorText = errorTextProperty.GetValue(args)?.ToString() ?? "Unknown error";
        
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
    /// Desativa o monitoramento de rede
    /// </summary>
    public void DisableNetworkMonitoring()
    {
        if (!_isNetworkMonitoringEnabled || _session == null || _networkAdapterType == null) return;
        
        try
        {
            var network = GetVersionSpecificDomain<object>(_session, _networkAdapterType);
            if (network == null) return;
            
            var disableMethod = _networkAdapterType.GetMethod("Disable");
            if (disableMethod == null) return;
            
            disableMethod.Invoke(network, null);
            _isNetworkMonitoringEnabled = false;
            LogInfo("Monitoramento de rede desativado com sucesso");
        }
        catch (Exception ex)
        {
            LogWarning($"Erro ao desativar monitoramento de rede: {ex.Message}");
        }
    }

    #endregion

    #region Monitoramento de Console

    /// <summary>
    /// Inicia o monitoramento do console do navegador
    /// </summary>
    public void EnableConsoleMonitoring()
    {
        if (_session == null || _consoleAdapterType == null)
        {
            LogWarning("Sessão DevTools ou adaptador de console não disponível");
            return;
        }

        try
        {
            _consoleMessages.Clear();
            
            var console = GetVersionSpecificDomain<object>(_session, _consoleAdapterType);
            if (console == null) return;
            
            // Habilitar o domínio Console
            var enableMethod = _consoleAdapterType.GetMethod("Enable");
            if (enableMethod == null)
            {
                LogWarning("Método Enable não encontrado para console");
                return;
            }
            
            enableMethod.Invoke(console, null);
            
            // Registrar evento MessageAdded
            if (!RegisterConsoleEvents(console)) return;
            _isConsoleMonitoringEnabled = true;
            LogInfo("Monitoramento de console ativado com sucesso");
        }
        catch (Exception ex)
        {
            LogError($"Erro ao ativar monitoramento de console: {ex.Message}");
        }
    }

    /// <summary>
    /// Registra eventos para monitoramento de console
    /// </summary>
    private bool RegisterConsoleEvents(object console)
    {
        try
        {
            var messageAddedEvent = _consoleAdapterType!.GetEvent("MessageAdded");
            if (messageAddedEvent == null)
            {
                LogWarning("Evento MessageAdded não encontrado");
                return false;
            }
            
            var messageAddedArgsType = messageAddedEvent.EventHandlerType?.GetMethod("Invoke")?.GetParameters()[1].ParameterType;
            if (messageAddedArgsType == null)
            {
                LogWarning("Tipo de argumento para evento MessageAdded não encontrado");
                return false;
            }
            
            var messageAddedHandler = CreateEventHandler(messageAddedEvent.EventHandlerType!, messageAddedArgsType,
                (sender, args) => HandleConsoleMessageAdded(args));
            
            messageAddedEvent.AddEventHandler(console, messageAddedHandler);
            return true;
        }
        catch (Exception ex)
        {
            LogWarning($"Erro ao registrar eventos de console: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Manipula evento MessageAdded do console do DevTools
    /// </summary>
    private void HandleConsoleMessageAdded(object? args)
    {
        if (args == null) return;
        
        try
        {
            var messageProperty = args.GetType().GetProperty("Message");
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
                    TryParse(lineValue.ToString(), out line);
                }
            }
            
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

    /// <summary>
    /// Desativa o monitoramento do console
    /// </summary>
    public void DisableConsoleMonitoring()
    {
        if (!_isConsoleMonitoringEnabled || _session == null || _consoleAdapterType == null) return;
        
        try
        {
            var console = GetVersionSpecificDomain<object>(_session, _consoleAdapterType);
            if (console == null) return;
            
            var disableMethod = _consoleAdapterType.GetMethod("Disable");
            if (disableMethod == null) return;
            
            disableMethod.Invoke(console, null);
            _isConsoleMonitoringEnabled = false;
            LogInfo("Monitoramento de console desativado com sucesso");
        }
        catch (Exception ex)
        {
            LogWarning($"Erro ao desativar monitoramento de console: {ex.Message}");
        }
    }

    #endregion

    #region Monitoramento de Performance

    /// <summary>
    /// Inicia o monitoramento de métricas de performance
    /// </summary>
    public void EnablePerformanceMonitoring()
    {
        if (_session == null || _performanceAdapterType == null)
        {
            LogWarning("Sessão DevTools ou adaptador de performance não disponível");
            return;
        }

        try
        {
            _performanceMetrics.Clear();
            
            var performance = GetVersionSpecificDomain<object>(_session, _performanceAdapterType);
            if (performance == null) return;
            
            // Habilitar o domínio Performance
            var enableMethod = _performanceAdapterType.GetMethod("Enable");
            if (enableMethod == null)
            {
                LogWarning("Método Enable não encontrado para performance");
                return;
            }
            
            enableMethod.Invoke(performance, null);
            
            // Configurar domínio de tempo
            ConfigurePerformanceTimeDomain(performance);
            
            // Registrar eventos
            if (RegisterPerformanceEvents(performance))
            {
                _isPerformanceMonitoringEnabled = true;
                LogInfo("Monitoramento de performance ativado com sucesso");
            }
        }
        catch (Exception ex)
        {
            LogError($"Erro ao ativar monitoramento de performance: {ex.Message}");
        }
    }

    /// <summary>
    /// Configura o domínio de tempo para métricas de performance
    /// </summary>
    private void ConfigurePerformanceTimeDomain(object performance)
    {
        var setTimeDomainMethod = _performanceAdapterType!.GetMethod("SetTimeDomain");
        if (setTimeDomainMethod == null) return;
        
        var timeDomainSettingsType = _performanceAdapterType.Assembly.GetType(
            _performanceAdapterType.Namespace + ".SetTimeDomainCommandSettings");
        
        if (timeDomainSettingsType == null) return;
        
        var settings = Activator.CreateInstance(timeDomainSettingsType);
        var timeDomainProperty = timeDomainSettingsType.GetProperty("TimeDomain");
        
        if (timeDomainProperty == null) return;
        
        timeDomainProperty.SetValue(settings, "threadTicks");
        setTimeDomainMethod.Invoke(performance, [settings]);
    }

    /// <summary>
    /// Registra eventos para monitoramento de performance
    /// </summary>
    private bool RegisterPerformanceEvents(object performance)
    {
        try
        {
            var metricsReceivedEvent = _performanceAdapterType!.GetEvent("MetricsReceived");
            if (metricsReceivedEvent == null)
            {
                LogWarning("Evento MetricsReceived não encontrado");
                return false;
            }
            
            var metricsReceivedArgsType = metricsReceivedEvent.EventHandlerType?.GetMethod("Invoke")?.GetParameters()[1].ParameterType;
            if (metricsReceivedArgsType == null)
            {
                LogWarning("Tipo de argumento para evento MetricsReceived não encontrado");
                return false;
            }
            
            var metricsReceivedHandler = CreateEventHandler(metricsReceivedEvent.EventHandlerType!, metricsReceivedArgsType,
                (sender, args) => HandleMetricsReceived(args));
            
            metricsReceivedEvent.AddEventHandler(performance, metricsReceivedHandler);
            
            // Iniciar coleta periódica de métricas
            StartPeriodicMetricsCollection(performance);
            
            return true;
        }
        catch (Exception ex)
        {
            LogWarning($"Erro ao registrar eventos de performance: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Inicia a coleta periódica de métricas
    /// </summary>
    private void StartPeriodicMetricsCollection(object performance)
    {
        var getMetricsMethod = _performanceAdapterType!.GetMethod("GetMetrics");
        if (getMetricsMethod == null) return;
        
        // Criar timer para coletar métricas a cada 2 segundos
        _metricsTimer = new Timer(_ =>
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

    /// <summary>
    /// Manipula evento MetricsReceived de performance do DevTools
    /// </summary>
    private void HandleMetricsReceived(object? args)
    {
        if (args == null) return;
        
        try
        {
            var metricsProperty = args.GetType().GetProperty("Metrics");
            if (metricsProperty == null) return;
            
            var metrics = metricsProperty.GetValue(args) as IEnumerable;
            if (metrics == null) return;
            
            foreach (var metric in metrics)
            {
                var nameProperty = metric.GetType().GetProperty("Name");
                var valueProperty = metric.GetType().GetProperty("Value");
                
                if (nameProperty == null || valueProperty == null) continue;
                
                var name = nameProperty.GetValue(metric)?.ToString() ?? string.Empty;
                var value = valueProperty.GetValue(metric)?.ToString() ?? string.Empty;
                
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

    /// <summary>
    /// Desativa o monitoramento de performance
    /// </summary>
    public void DisablePerformanceMonitoring()
    {
        if (!_isPerformanceMonitoringEnabled || _session == null || _performanceAdapterType == null) return;
        
        try
        {
            // Parar o timer
            _metricsTimer?.Dispose();
            _metricsTimer = null;
            
            var performance = GetVersionSpecificDomain<object>(_session, _performanceAdapterType);
            if (performance == null) return;
            
            var disableMethod = _performanceAdapterType.GetMethod("Disable");
            if (disableMethod == null) return;
            
            disableMethod.Invoke(performance, null);
            _isPerformanceMonitoringEnabled = false;
            LogInfo("Monitoramento de performance desativado com sucesso");
        }
        catch (Exception ex)
        {
            LogWarning($"Erro ao desativar monitoramento de performance: {ex.Message}");
        }
    }

    #endregion

    #region Utilitários e métodos auxiliares

    /// <summary>
    /// Obtém um domínio específico da versão do DevTools
    /// </summary>
    private T? GetVersionSpecificDomain<T>(DevToolsSession? session, Type adapterType)
    {
        try
        {
            Debug.Assert(session != null, nameof(session) + " != null");
            var getVersionSpecificDomains = session.GetType().GetMethod("GetVersionSpecificDomains")
                ?.MakeGenericMethod(adapterType);

            if (getVersionSpecificDomains != null) return (T?)getVersionSpecificDomains.Invoke(session, null);
            LogWarning($"Método GetVersionSpecificDomains não encontrado para {adapterType.Name}");
            return default;

        }
        catch (Exception ex)
        {
            LogWarning($"Erro ao obter domínio específico da versão: {ex.Message}");
            return default;
        }
    }

    /// <summary>
    /// Cria um handler de evento tipado usando reflexão
    /// </summary>
    private Delegate CreateEventHandler(Type eventHandlerType, Type eventArgsType, Action<object, object?> handler)
    {
        try
        {
            // Usar método de ajuda tipado para criar o delegate
            var dynamicHandlerMethod = GetType().GetMethod("DynamicEventHandler", 
                BindingFlags.NonPublic | BindingFlags.Instance)
                ?.MakeGenericMethod(eventArgsType);

            if (dynamicHandlerMethod != null)
                return Delegate.CreateDelegate(eventHandlerType, this, dynamicHandlerMethod);
            LogWarning("Método DynamicEventHandler não encontrado");
                
            // Fallback: usar method info direto para criar o delegate
            return Delegate.CreateDelegate(eventHandlerType, this, 
                GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                    .First(m => m.Name.Contains("DynamicEventHandler")));

        }
        catch (Exception ex)
        {
            LogWarning($"Erro ao criar handler de evento: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Método dinâmico para processar eventos de diferentes tipos
    /// </summary>
    private void DynamicEventHandler<T>(object sender, T? args)
    {
        if (args == null) return;
        
        var argsType = typeof(T);
        
        if (argsType.Name.Contains("RequestWillBeSent"))
            HandleRequestWillBeSent(args);
        else if (argsType.Name.Contains("ResponseReceived"))
            HandleResponseReceived(args);
        else if (argsType.Name.Contains("LoadingFailed"))
            HandleLoadingFailed(args);
        else if (argsType.Name.Contains("MessageAdded"))
            HandleConsoleMessageAdded(args);
        else if (argsType.Name.Contains("MetricsReceived"))
            HandleMetricsReceived(args);
    }

    /// <summary>
    /// Desativa todos os monitoramentos ativos
    /// </summary>
    public void DisableAllMonitoring()
    {
        DisableNetworkMonitoring();
        DisableConsoleMonitoring();
        DisablePerformanceMonitoring();
        LogInfo("Todos os monitoramentos foram desativados");
    }

    /// <summary>
    /// Captura screenshots de erros encontrados durante o monitoramento
    /// </summary>
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

    /// <summary>
    /// Adiciona informações de monitoramento ao relatório de testes
    /// </summary>
    public void AddInfoToReport()
    {
        if (_test == null) return;
        
        try
        {
            // Adicionar métricas de performance
            AddPerformanceMetricsToReport();
            
            // Adicionar resumo de requisições de rede
            AddNetworkRequestsToReport();
            
            // Adicionar mensagens de console
            AddConsoleMessagesToReport();
        }
        catch (Exception ex)
        {
            LogWarning($"Erro ao adicionar informações ao relatório: {ex.Message}");
        }
    }

    /// <summary>
    /// Adiciona métricas de performance ao relatório
    /// </summary>
    private void AddPerformanceMetricsToReport()
    {
        if (_test == null || _performanceMetrics.Count <= 0) return;
        
        var metrics = _performanceMetrics
            .GroupBy(m => m.Name)
            .Select(g => new
            {
                Name = g.Key,
                LastValue = g.OrderByDescending(m => m.Timestamp).First().Value,
                Min = g.Min(m => double.TryParse(g.First().Value, out var val) ? val : 0),
                Max = g.Max(m => double.TryParse(g.First().Value, out var val) ? val : 0),
                Count = g.Count()
            })
            .ToList();
        
        var metricsTable = metrics.Aggregate("<table border='1'><tr><th>Métrica</th><th>Valor</th><th>Min</th><th>Max</th></tr>", 
            (current, metric) => current + $"<tr><td>{metric.Name}</td><td>{metric.LastValue}</td><td>{metric.Min}</td><td>{metric.Max}</td></tr>");
        metricsTable += "</table>";
        
        _test.Info(metricsTable);
    }

    /// <summary>
    /// Adiciona resumo de requisições de rede ao relatório
    /// </summary>
    private void AddNetworkRequestsToReport()
    {
        if (_test == null || _networkRequests.Count <= 0) return;
        
        var totalRequests = _networkRequests.Count;
        var successRequests = _networkRequests.Count(r => int.TryParse(r.Status, out var status) && status is >= 200 and < 400);
        var failedRequests = _networkRequests.Count(r => r.Status == "Failed" || 
            (int.TryParse(r.Status, out int status) && status >= 400));
        var pendingRequests = totalRequests - successRequests - failedRequests;
        
        _test.Info($"<div>Requisições: {totalRequests} total | {successRequests} sucesso | " +
                   $"{failedRequests} falhas | {pendingRequests} pendentes</div>");
        
        // Detalhar falhas se houver
        if (failedRequests <= 0) return;
        {
            var failedRequestsList = _networkRequests
                .Where(r => r.Status == "Failed" || 
                            (int.TryParse(r.Status, out int statusCode) && statusCode >= 400))
                .OrderByDescending(r => r.Timestamp)
                .ToList();
            
            var failuresTable = failedRequestsList.Aggregate("<table border='1'><tr><th>URL</th><th>Status</th><th>Erro</th></tr>", 
                (current, request) => current + $"<tr><td>{request.Url}</td><td>{request.Status}</td><td>{request.StatusText}</td></tr>");
            failuresTable += "</table>";
            
            _test.Warning(failuresTable);
        }
    }

    /// <summary>
    /// Adiciona mensagens de console ao relatório
    /// </summary>
    private void AddConsoleMessagesToReport()
    {
        if (_test == null || _consoleMessages.Count <= 0) return;
        
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
            
            var errorsTable = errorMessages.Aggregate("<table border='1'><tr><th>Mensagem</th><th>URL</th><th>Linha</th></tr>", 
                (current, message) => current + $"<tr><td>{message.Text}</td><td>{message.Url}</td><td>{message.LineNumber}</td></tr>");
            errorsTable += "</table>";
            
            _test.Error(errorsTable);
        }
    }

    /// <summary>
    /// Ativa uma implementação simplificada quando o DevTools não está disponível
    /// </summary>
    public void UseSimpleImplementation()
    {
        if (_driver == null) return;
        
        try
        {
            // Implementar captura de erros de JavaScript usando o executor JavaScript padrão
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
                    Text = error["message"].ToString() ?? "Unknown error",
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
            DisableAllMonitoring();
            _metricsTimer?.Dispose();
            _session = null;
        }
        catch (Exception ex)
        {
            LogWarning($"Erro ao liberar recursos: {ex.Message}");
        }
    }

    #endregion

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