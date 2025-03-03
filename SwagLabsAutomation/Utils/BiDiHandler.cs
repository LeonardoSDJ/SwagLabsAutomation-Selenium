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
/// Handler for BiDi (Bidirectional) functionality of Selenium WebDriver.
/// Provides access to features like network monitoring, console, and JavaScript.
/// </summary>
public class BiDiHandler : IDisposable
{
    #region Properties and fields

    private readonly IWebDriver? _driver;
    private readonly ExtentTest? _test;
    private DevToolsSession? _session;
    private bool _isNetworkMonitoringEnabled;
    private bool _isConsoleMonitoringEnabled;
    private bool _isPerformanceMonitoringEnabled;
    
    // Collections for storing monitoring data
    private readonly ConcurrentBag<NetworkRequest> _networkRequests = new();
    private readonly ConcurrentBag<ConsoleMessage> _consoleMessages = new();
    private readonly ConcurrentBag<PerformanceMetric> _performanceMetrics = new();

    // Reflection information for DevTools version
    private Type? _networkAdapterType;
    private Type? _consoleAdapterType;
    private Type? _performanceAdapterType;
    private Type? _runtimeAdapterType;

    // Timer for periodic metrics collection
    private Timer? _metricsTimer;

    #endregion

    #region Constructor and initialization

    /// <summary>
    /// Constructor for BiDiHandler
    /// </summary>
    /// <param name="driver">Current WebDriver</param>
    /// <param name="test">ExtentTest for logging (optional)</param>
    public BiDiHandler(IWebDriver? driver, ExtentTest? test = null)
    {
        _driver = driver;
        _test = test;
        
        InitializeDevToolsSession();
    }

    /// <summary>
    /// Initializes the DevTools session if possible
    /// </summary>
    private void InitializeDevToolsSession()
    {
        if (_driver is not IDevTools devToolsDriver)
        {
            LogWarning("The provided driver does not support DevTools");
            return;
        }

        try
        {
            _session = devToolsDriver.GetDevToolsSession();
            LogInfo("DevTools session initialized successfully");
            
            DetectDevToolsVersions();
        }
        catch (Exception ex)
        {
            LogWarning($"Could not initialize DevTools session: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Detects available DevTools versions via reflection
    /// </summary>
    private void DetectDevToolsVersions()
    {
        try
        {
            var seleniumAssembly = typeof(IDevTools).Assembly;
            var devToolsVersions = GetDevToolsVersions(seleniumAssembly);
            
            if (devToolsVersions.Count == 0)
            {
                LogWarning("No DevTools versions found");
                return;
            }
            
            // Get latest version
            string latestVersion = devToolsVersions[0];
            LogInfo($"DevTools versions found: {string.Join(", ", devToolsVersions)}");
            LogInfo($"Using version: {latestVersion}");
            
            // Initialize adapter types
            InitializeAdapterTypes(seleniumAssembly, latestVersion);
        }
        catch (Exception ex)
        {
            LogError($"Error detecting DevTools versions: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets available DevTools versions in descending order
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
        
        // Sort versions in descending order
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
    /// Initializes adapter types for the specific DevTools version
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
            LogInfo($"All adapters found for version {version}");
            FindNetworkInterface(seleniumAssembly, version);
        }
        else
        {
            LogWarning($"Some adapters were not found for version {version}");
        }
    }

    #endregion

    #region Network Monitoring

    /// <summary>
    /// Starts monitoring network requests
    /// </summary>
    public void EnableNetworkMonitoring()
    {
        if (_session == null || _networkAdapterType == null)
        {
            LogWarning("DevTools session or network adapter not available");
            return;
        }

        try
        {
            _networkRequests.Clear();
            // Here's the fix: use INetwork interface
            var network = GetVersionSpecificDomain<object>(_session, _networkAdapterType);
            if (network == null) return;
            
            // Enable Network domain
            EnableNetworkDomain(network);
        
            // Register events
            if (RegisterNetworkEvents(network))
            {
                _isNetworkMonitoringEnabled = true;
                LogInfo("Network monitoring enabled successfully");
            }
        }
        catch (Exception ex)
        {
            LogError($"Error enabling network monitoring: {ex.Message}");
        }
    }
    
    private void FindNetworkInterface(Assembly seleniumAssembly, string version)
    {
        var networkNamespace = $"OpenQA.Selenium.DevTools.{version}.Network";

        foreach (var type in seleniumAssembly.GetTypes())
        {
            if (type.Namespace == null || !type.Namespace.StartsWith(networkNamespace) || !type.IsInterface ||
                !type.Name.StartsWith("I")) continue;
            LogInfo($"Network interface found: {type.FullName}");
            var interfaceType = type.FullName;
            if (interfaceType == null) continue;
            var genericDomain = GetVersionSpecificDomain<object>(_session, Type.GetType(interfaceType)!);
            if (genericDomain == null) continue;
            LogInfo($"Network domain initialized using {interfaceType}");
            EnableNetworkDomain(genericDomain);
        }
    }

    /// <summary>
    /// Enables the Network domain in DevTools
    /// </summary>
    private void EnableNetworkDomain(object network)
    {
        var enableMethod = _networkAdapterType!.GetMethod("Enable");
        var enableSettingsType = _networkAdapterType.Assembly.GetType(
            _networkAdapterType.Namespace + ".EnableCommandSettings");
        
        if (enableMethod == null || enableSettingsType == null)
        {
            LogWarning("Methods or types required for network monitoring not found");
            return;
        }
        
        var enableSettings = Activator.CreateInstance(enableSettingsType);
        enableMethod.Invoke(network, [enableSettings]);
    }

    /// <summary>
    /// Registers events for network monitoring
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
                LogWarning("One or more network events not found");
                return false;
            }
            
            // Get argument types for events
            var requestWillBeSentArgsType = requestWillBeSentEventInfo.EventHandlerType?.GetMethod("Invoke")?.GetParameters()[1].ParameterType;
            var responseReceivedArgsType = responseReceivedEventInfo.EventHandlerType?.GetMethod("Invoke")?.GetParameters()[1].ParameterType;
            var loadingFailedArgsType = loadingFailedEventInfo.EventHandlerType?.GetMethod("Invoke")?.GetParameters()[1].ParameterType;
            
            if (requestWillBeSentArgsType == null || responseReceivedArgsType == null || loadingFailedArgsType == null)
            {
                LogWarning("Could not determine event argument types");
                return false;
            }
            
            // Create and register handlers for events
            var requestWillBeSentHandler = CreateEventHandler(requestWillBeSentEventInfo.EventHandlerType!, requestWillBeSentArgsType, 
                (sender, args) => HandleRequestWillBeSent(args));
                
            var responseReceivedHandler = CreateEventHandler(responseReceivedEventInfo.EventHandlerType!, responseReceivedArgsType, 
                (sender, args) => HandleResponseReceived(args));
                
            var loadingFailedHandler = CreateEventHandler(loadingFailedEventInfo.EventHandlerType!, loadingFailedArgsType, 
                (sender, args) => HandleLoadingFailed(args));
            
            requestWillBeSentEventInfo.AddEventHandler(network, requestWillBeSentHandler);
            responseReceivedEventInfo.AddEventHandler(network, responseReceivedHandler);
            loadingFailedEventInfo.AddEventHandler(network, loadingFailedHandler);
            
            LogInfo("Network events registered successfully");
            return true;
        }
        catch (Exception ex)
        {
            LogError($"Error registering network events: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Handles RequestWillBeSent event from DevTools
    /// </summary>
    private void HandleRequestWillBeSent(object? args)
    {
        try
        {
            // Extract data from event via reflection
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
            LogInfo($"Request captured: {method} {url}");
        }
        catch (Exception ex)
        {
            LogWarning($"Error processing RequestWillBeSent: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles ResponseReceived event from DevTools
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
            
            LogInfo($"Response received: {request.Method} {request.Url} - Status: {status}");
        }
        catch (Exception ex)
        {
            LogWarning($"Error processing ResponseReceived: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles LoadingFailed event from DevTools
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
            
            LogWarning($"Loading failed: {request.Method} {request.Url} - Error: {errorText}");
        }
        catch (Exception ex)
        {
            LogWarning($"Error processing LoadingFailed: {ex.Message}");
        }
    }

    /// <summary>
    /// Disables network monitoring
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
            LogInfo("Network monitoring disabled successfully");
        }
        catch (Exception ex)
        {
            LogWarning($"Error disabling network monitoring: {ex.Message}");
        }
    }

    #endregion

    #region Console Monitoring

    /// <summary>
    /// Starts monitoring the browser console
    /// </summary>
    public void EnableConsoleMonitoring()
    {
        if (_session == null || _consoleAdapterType == null)
        {
            LogWarning("DevTools session or console adapter not available");
            return;
        }

        try
        {
            _consoleMessages.Clear();
            
            var console = GetVersionSpecificDomain<object>(_session, _consoleAdapterType);
            if (console == null) return;
            
            // Enable Console domain
            var enableMethod = _consoleAdapterType.GetMethod("Enable");
            if (enableMethod == null)
            {
                LogWarning("Enable method not found for console");
                return;
            }
            
            enableMethod.Invoke(console, null);
            
            // Register MessageAdded event
            if (!RegisterConsoleEvents(console)) return;
            _isConsoleMonitoringEnabled = true;
            LogInfo("Console monitoring enabled successfully");
        }
        catch (Exception ex)
        {
            LogError($"Error enabling console monitoring: {ex.Message}");
        }
    }

    /// <summary>
    /// Registers events for console monitoring
    /// </summary>
    private bool RegisterConsoleEvents(object console)
    {
        try
        {
            var messageAddedEvent = _consoleAdapterType!.GetEvent("MessageAdded");
            if (messageAddedEvent == null)
            {
                LogWarning("MessageAdded event not found");
                return false;
            }
            
            var messageAddedArgsType = messageAddedEvent.EventHandlerType?.GetMethod("Invoke")?.GetParameters()[1].ParameterType;
            if (messageAddedArgsType == null)
            {
                LogWarning("Argument type for MessageAdded event not found");
                return false;
            }
            
            var messageAddedHandler = CreateEventHandler(messageAddedEvent.EventHandlerType!, messageAddedArgsType,
                (sender, args) => HandleConsoleMessageAdded(args));
            
            messageAddedEvent.AddEventHandler(console, messageAddedHandler);
            return true;
        }
        catch (Exception ex)
        {
            LogWarning($"Error registering console events: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Handles MessageAdded event from console in DevTools
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
            
            // Log according to level
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
            LogWarning($"Error processing console message: {ex.Message}");
        }
    }

    /// <summary>
    /// Disables console monitoring
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
            LogInfo("Console monitoring disabled successfully");
        }
        catch (Exception ex)
        {
            LogWarning($"Error disabling console monitoring: {ex.Message}");
        }
    }

    #endregion

    #region Performance Monitoring

    /// <summary>
    /// Starts monitoring performance metrics
    /// </summary>
    public void EnablePerformanceMonitoring()
    {
        if (_session == null || _performanceAdapterType == null)
        {
            LogWarning("DevTools session or performance adapter not available");
            return;
        }

        try
        {
            _performanceMetrics.Clear();
            
            var performance = GetVersionSpecificDomain<object>(_session, _performanceAdapterType);
            if (performance == null) return;
            
            // Enable Performance domain
            var enableMethod = _performanceAdapterType.GetMethod("Enable");
            if (enableMethod == null)
            {
                LogWarning("Enable method not found for performance");
                return;
            }
            
            enableMethod.Invoke(performance, null);
            
            // Configure time domain
            ConfigurePerformanceTimeDomain(performance);
            
            // Register events
            if (RegisterPerformanceEvents(performance))
            {
                _isPerformanceMonitoringEnabled = true;
                LogInfo("Performance monitoring enabled successfully");
            }
        }
        catch (Exception ex)
        {
            LogError($"Error enabling performance monitoring: {ex.Message}");
        }
    }

    /// <summary>
    /// Configures the time domain for performance metrics
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
    /// Registers events for performance monitoring
    /// </summary>
    private bool RegisterPerformanceEvents(object performance)
    {
        try
        {
            var metricsReceivedEvent = _performanceAdapterType!.GetEvent("MetricsReceived");
            if (metricsReceivedEvent == null)
            {
                LogWarning("MetricsReceived event not found");
                return false;
            }
            
            var metricsReceivedArgsType = metricsReceivedEvent.EventHandlerType?.GetMethod("Invoke")?.GetParameters()[1].ParameterType;
            if (metricsReceivedArgsType == null)
            {
                LogWarning("Argument type for MetricsReceived event not found");
                return false;
            }
            
            var metricsReceivedHandler = CreateEventHandler(metricsReceivedEvent.EventHandlerType!, metricsReceivedArgsType,
                (sender, args) => HandleMetricsReceived(args));
            
            metricsReceivedEvent.AddEventHandler(performance, metricsReceivedHandler);
            
            // Start periodic metrics collection
            StartPeriodicMetricsCollection(performance);
            
            return true;
        }
        catch (Exception ex)
        {
            LogWarning($"Error registering performance events: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Starts periodic collection of metrics
    /// </summary>
    private void StartPeriodicMetricsCollection(object performance)
    {
        var getMetricsMethod = _performanceAdapterType!.GetMethod("GetMetrics");
        if (getMetricsMethod == null) return;
        
        // Create timer to collect metrics every 2 seconds
        _metricsTimer = new Timer(_ =>
        {
            try
            {
                getMetricsMethod.Invoke(performance, null);
            }
            catch (Exception ex)
            {
                LogWarning($"Error collecting metrics: {ex.Message}");
            }
        }, null, 0, 2000);
    }

    /// <summary>
    /// Handles MetricsReceived event from performance in DevTools
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
                LogInfo($"Metric: {name} = {value}");
            }
        }
        catch (Exception ex)
        {
            LogWarning($"Error processing metrics: {ex.Message}");
        }
    }

    /// <summary>
    /// Disables performance monitoring
    /// </summary>
    public void DisablePerformanceMonitoring()
    {
        if (!_isPerformanceMonitoringEnabled || _session == null || _performanceAdapterType == null) return;
        
        try
        {
            // Stop timer
            _metricsTimer?.Dispose();
            _metricsTimer = null;
            
            var performance = GetVersionSpecificDomain<object>(_session, _performanceAdapterType);
            if (performance == null) return;
            
            var disableMethod = _performanceAdapterType.GetMethod("Disable");
            if (disableMethod == null) return;
            
            disableMethod.Invoke(performance, null);
            _isPerformanceMonitoringEnabled = false;
            LogInfo("Performance monitoring disabled successfully");
        }
        catch (Exception ex)
        {
            LogWarning($"Error disabling performance monitoring: {ex.Message}");
        }
    }

    #endregion

    #region Utilities and helper methods

    /// <summary>
    /// Gets a specific domain version from DevTools
    /// </summary>
    private T? GetVersionSpecificDomain<T>(DevToolsSession? session, Type adapterType)
    {
        try
        {
            Debug.Assert(session != null, nameof(session) + " != null");
            var getVersionSpecificDomains = session.GetType().GetMethod("GetVersionSpecificDomains")
                ?.MakeGenericMethod(adapterType);

            if (getVersionSpecificDomains != null) return (T?)getVersionSpecificDomains.Invoke(session, null);
            LogWarning($"GetVersionSpecificDomains method not found for {adapterType.Name}");
            return default;

        }
        catch (Exception ex)
        {
            LogWarning($"Error getting specific version domain: {ex.Message}");
            return default;
        }
    }

    /// <summary>
    /// Creates a typed event handler using reflection
    /// </summary>
    private Delegate CreateEventHandler(Type eventHandlerType, Type eventArgsType, Action<object, object?> handler)
    {
        try
        {
            // Use typed helper method to create the delegate
            var dynamicHandlerMethod = GetType().GetMethod("DynamicEventHandler", 
                BindingFlags.NonPublic | BindingFlags.Instance)
                ?.MakeGenericMethod(eventArgsType);

            if (dynamicHandlerMethod != null)
                return Delegate.CreateDelegate(eventHandlerType, this, dynamicHandlerMethod);
            LogWarning("DynamicEventHandler method not found");
                
            // Fallback: use method info directly to create the delegate
            return Delegate.CreateDelegate(eventHandlerType, this, 
                GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                    .First(m => m.Name.Contains("DynamicEventHandler")));

        }
        catch (Exception ex)
        {
            LogWarning($"Error creating event handler: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Dynamic method to process events of different types
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
    /// Disables all active monitoring
    /// </summary>
    public void DisableAllMonitoring()
    {
        DisableNetworkMonitoring();
        DisableConsoleMonitoring();
        DisablePerformanceMonitoring();
        LogInfo("All monitoring disabled");
    }

    /// <summary>
    /// Captures screenshots of errors found during monitoring
    /// </summary>
    public void CaptureErrorScreenshots(string testName)
    {
        // Check for console errors
        var errors = _consoleMessages.Where(m => m.Level.ToLower() == "error").ToList();
    
        if (errors.Count > 0)
        {
            LogWarning($"Found {errors.Count} JavaScript/console errors");
            CaptureScreenshot($"{testName}_JSError");
        
            // Log error details
            foreach (var error in errors)
            {
                LogError($"JS Error: {error.Text} - {error.Url}:{error.LineNumber}");
            }
        }
    
        // Check for network failures
        var failedRequests = _networkRequests.Where(r => r.Status == "Failed" || 
                                                        (int.TryParse(r.Status, out int statusCode) && statusCode >= 400)).ToList();
    
        if (failedRequests.Count > 0)
        {
            LogWarning($"Found {failedRequests.Count} network errors");
            CaptureScreenshot($"{testName}_NetworkError");
        
            // Log failure details
            foreach (var request in failedRequests)
            {
                LogError($"Network Error: {request.Method} {request.Url} - Status: {request.Status} {request.StatusText}");
            }
        }
    }

    /// <summary>
    /// Adds monitoring information to the test report
    /// </summary>
    public void AddInfoToReport()
    {
        if (_test == null) return;
        
        try
        {
            // Add performance metrics
            AddPerformanceMetricsToReport();
            
            // Add network request summary
            AddNetworkRequestsToReport();
            
            // Add console messages
            AddConsoleMessagesToReport();
        }
        catch (Exception ex)
        {
            LogWarning($"Error adding information to report: {ex.Message}");
        }
    }

    /// <summary>
    /// Adds performance metrics to the report
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
        
        var metricsTable = metrics.Aggregate("<table border='1'><tr><th>Metric</th><th>Value</th><th>Min</th><th>Max</th></tr>", 
            (current, metric) => current + $"<tr><td>{metric.Name}</td><td>{metric.LastValue}</td><td>{metric.Min}</td><td>{metric.Max}</td></tr>");
        metricsTable += "</table>";
        
        _test.Info(metricsTable);
    }

    /// <summary>
    /// Adds network request summary to the report
    /// </summary>
    private void AddNetworkRequestsToReport()
    {
        if (_test == null || _networkRequests.Count <= 0) return;
        
        var totalRequests = _networkRequests.Count;
        var successRequests = _networkRequests.Count(r => int.TryParse(r.Status, out var status) && status is >= 200 and < 400);
        var failedRequests = _networkRequests.Count(r => r.Status == "Failed" || 
            (int.TryParse(r.Status, out int status) && status >= 400));
        var pendingRequests = totalRequests - successRequests - failedRequests;
        
        _test.Info($"<div>Requests: {totalRequests} total | {successRequests} success | " +
                   $"{failedRequests} failures | {pendingRequests} pending</div>");
        
        // Detail failures if any
        if (failedRequests <= 0) return;
        {
            var failedRequestsList = _networkRequests
                .Where(r => r.Status == "Failed" || 
                            (int.TryParse(r.Status, out int statusCode) && statusCode >= 400))
                .OrderByDescending(r => r.Timestamp)
                .ToList();
            
            var failuresTable = failedRequestsList.Aggregate("<table border='1'><tr><th>URL</th><th>Status</th><th>Error</th></tr>", 
                (current, request) => current + $"<tr><td>{request.Url}</td><td>{request.Status}</td><td>{request.StatusText}</td></tr>");
            failuresTable += "</table>";
            
            _test.Warning(failuresTable);
        }
    }

    /// <summary>
    /// Adds console messages to the report
    /// </summary>
    private void AddConsoleMessagesToReport()
    {
        if (_test == null || _consoleMessages.Count <= 0) return;
        
        var errorCount = _consoleMessages.Count(m => m.Level.ToLower() == "error");
        var warningCount = _consoleMessages.Count(m => m.Level.ToLower() == "warning");
        
        _test.Info($"<div>Console: {_consoleMessages.Count} messages | {errorCount} errors | {warningCount} warnings</div>");
        
        // Detail errors if any
        if (errorCount <= 0) return;
        {
            var errorMessages = _consoleMessages
                .Where(m => m.Level.ToLower() == "error")
                .OrderByDescending(m => m.Timestamp)
                .ToList();
            
            var errorsTable = errorMessages.Aggregate("<table border='1'><tr><th>Message</th><th>URL</th><th>Line</th></tr>", 
                (current, message) => current + $"<tr><td>{message.Text}</td><td>{message.Url}</td><td>{message.LineNumber}</td></tr>");
            errorsTable += "</table>";
            
            _test.Error(errorsTable);
        }
    }

    /// <summary>
    /// Activates a simplified implementation when DevTools is not available
    /// </summary>
    public void UseSimpleImplementation()
    {
        if (_driver == null) return;
        
        try
        {
            // Implement JavaScript error capture using standard JavaScript executor
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
            
            LogInfo("Simplified monitoring implementation activated");
        }
        catch (Exception ex)
        {
            LogWarning($"Error configuring simplified implementation: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Collects JavaScript errors using the simplified implementation
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
            LogWarning($"Error collecting JavaScript errors: {ex.Message}");
        }
        
        return errors;
    }

    /// <summary>
    /// Function to test connectivity with DevTools
    /// </summary>
    public bool TestDevToolsConnectivity()
    {
        if (_driver is not IDevTools tools) return false;
        
        try
        {
            using var session = tools.GetDevToolsSession();
            LogInfo("DevTools connectivity tested successfully");
            return true;
        }
        catch (Exception ex)
        {
            LogWarning($"Failed to connect to DevTools: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Captures screenshot in case of error
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
            LogInfo($"Screenshot saved at: {screenshotPath}");
            
            _test?.AddScreenCaptureFromPath(screenshotPath);
            
            return screenshotPath;
        }
        catch (Exception ex)
        {
            LogWarning($"Error capturing screenshot: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Releases resources
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
            LogWarning($"Error releasing resources: {ex.Message}");
        }
    }

    #endregion

    #region Helper classes for data storage

    /// <summary>
    /// Represents a captured network request
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
    /// Represents a captured console message
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
    /// Represents a captured performance metric
    /// </summary>
    private class PerformanceMetric
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    #endregion

    #region Logging methods

    private void LogInfo(string message)
    {
        Console.WriteLine($"[BiDiHandler] INFO: {message}");
        _test?.Info($"[BiDi] {message}");
    }

    private void LogWarning(string message)
    {
        Console.WriteLine($"[BiDiHandler] WARNING: {message}");
        _test?.Warning($"[BiDi] {message}");
    }

    private void LogError(string message)
    {
        Console.WriteLine($"[BiDiHandler] ERROR: {message}");
        _test?.Error($"[BiDi] {message}");
    }

    #endregion
}