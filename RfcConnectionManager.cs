using System.Collections.Concurrent;
using System.Threading;
using SAP.Middleware.Connector;

namespace Mitzh;

/// <summary>
///   RFC 连接管理器。
///   管理 SAP RFC 目标的注册、缓存、生命周期和清理，实现 <see cref="IDestinationConfiguration"/> 以集成 SAP NCo 连接库。
/// </summary>
public class RfcConnectionManager : IDestinationConfiguration, IDisposable
{
    private static readonly ConcurrentDictionary<string, RfcConfigParameter> _destinations = new();
    private static readonly ConcurrentDictionary<string, RfcDestination> _destinationCache = new();
    private static readonly ConcurrentDictionary<string, DateTime> _lastAccessTime = new();
    private static bool _configurationRegistered;
    private static readonly object _lock = new();
    private static Timer _cleanupTimer;
    private static TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);
    private static TimeSpan _destinationIdleTimeout = TimeSpan.FromMinutes(10);
    private bool _disposed;

    /// <summary>
    ///   静态构造函数，启动连接清理定时器。
    /// </summary>
    static RfcConnectionManager()
    {
        StartCleanupTimer();
    }

    /// <summary>
    ///   启动空闲连接清理定时器。
    /// </summary>
    private static void StartCleanupTimer()
    {
        _cleanupTimer = new Timer(
            _ => CleanupIdleDestinations(),
            null,
            _cleanupInterval,
            _cleanupInterval
        );
    }

    /// <summary>
    ///   运行时配置清理间隔和目标空闲超时时间。
    ///   此方法会使用新值重新启动内部的清理定时器。
    /// </summary>
    /// <param name="cleanupInterval">清理定时器执行间隔。</param>
    /// <param name="destinationIdleTimeout">目标连接的空闲超时时间，超过此时间未使用将被清理。</param>
    public static void ConfigureCleanup(TimeSpan cleanupInterval, TimeSpan destinationIdleTimeout)
    {
        if (cleanupInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cleanupInterval),
                cleanupInterval,
                "Cleanup interval must be greater than zero.");
        }

        if (destinationIdleTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(destinationIdleTimeout),
                destinationIdleTimeout,
                "Destination idle timeout must be greater than zero.");
        }

        lock (_lock)
        {
            _cleanupInterval = cleanupInterval;
            _destinationIdleTimeout = destinationIdleTimeout;

            _cleanupTimer?.Dispose();
            StartCleanupTimer();
        }
    }

    /// <summary>
    ///   清理超过空闲超时时间未使用的缓存目标连接。
    ///   先快照待清理的键，再逐个移除，避免遍历时与其他线程的缓存写入产生竞态。
    /// </summary>
    private static void CleanupIdleDestinations()
    {
        var now = DateTime.UtcNow;
        var keysToRemove = new List<string>();

        foreach (var kvp in _lastAccessTime)
        {
            if (now - kvp.Value > _destinationIdleTimeout)
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            RemoveCachedDestination(key);
        }
    }

    /// <summary>
    ///   注册一个 RFC 目标配置。
    ///   将连接参数注册到内部字典，并在首次调用时向 <see cref="RfcDestinationManager"/> 注册配置。
    /// </summary>
    /// <param name="destinationName">目标名称。</param>
    /// <param name="options">RFC 连接参数。</param>
    public static void RegisterDestination(string destinationName, RfcConfigParameter options)
    {
        if (string.IsNullOrWhiteSpace(destinationName))
            throw new ArgumentNullException(nameof(destinationName));

        ArgumentNullException.ThrowIfNull(options);

        _destinations[destinationName] = options;

        lock (_lock)
        {
            if (!_configurationRegistered)
            {
                RfcDestinationManager.RegisterDestinationConfiguration(new RfcConnectionManager());
                _configurationRegistered = true;
            }
        }

        RemoveCachedDestination(destinationName);
    }

    /// <summary>
    ///   注销一个已注册的 RFC 目标配置。
    /// </summary>
    /// <param name="destinationName">要注销的目标名称。</param>
    public static void UnregisterDestination(string destinationName)
    {
        if (string.IsNullOrWhiteSpace(destinationName))
            throw new ArgumentNullException(nameof(destinationName));

        RemoveCachedDestination(destinationName);
        _destinations.TryRemove(destinationName, out _);
    }

    /// <summary>
    ///   从缓存中移除指定的目标连接引用。
    /// </summary>
    /// <param name="destinationName">目标名称。</param>
    private static void RemoveCachedDestination(string destinationName)
    {
        _destinationCache.TryRemove(destinationName, out _);
        _lastAccessTime.TryRemove(destinationName, out _);
    }

    /// <summary>
    ///   检查指定目标是否已注册。
    /// </summary>
    /// <param name="destinationName">目标名称。</param>
    /// <returns>如果目标已注册则返回 true；否则返回 false。</returns>
    public static bool IsDestinationRegistered(string destinationName)
    {
        return _destinations.ContainsKey(destinationName);
    }

    /// <summary>
    ///   释放指定的目标连接，更新其最后访问时间以延迟清理。
    /// </summary>
    /// <param name="destinationName">目标名称。</param>
    public static void ReleaseDestination(string destinationName)
    {
        _lastAccessTime.AddOrUpdate(destinationName, DateTime.UtcNow, (_, _) => DateTime.UtcNow);
    }

    /// <summary>
    ///   获取指定名称的 RFC 目标连接（使用缓存）。
    /// </summary>
    /// <param name="destinationName">目标名称。</param>
    /// <returns>对应的 RfcDestination 实例。</returns>
    public static RfcDestination GetDestination(string destinationName)
    {
        return GetDestination(destinationName, false);
    }

    /// <summary>
    ///   获取指定名称的 RFC 目标连接。
    ///   可通过 forceNew 参数控制是否绕过缓存强制创建新连接。
    /// </summary>
    /// <param name="destinationName">目标名称。</param>
    /// <param name="forceNew">是否强制创建新的连接实例（绕过缓存）。</param>
    /// <returns>对应的 RfcDestination 实例。</returns>
    public static RfcDestination GetDestination(string destinationName, bool forceNew)
    {
        if (!_destinations.ContainsKey(destinationName))
        {
            throw new ArgumentException($"Destination '{destinationName}' not found");
        }

        _lastAccessTime.AddOrUpdate(destinationName, DateTime.UtcNow, (_, _) => DateTime.UtcNow);

        if (forceNew)
        {
            _destinationCache.TryRemove(destinationName, out _);
            return RfcDestinationManager.GetDestination(destinationName);
        }

        return _destinationCache.GetOrAdd(destinationName, RfcDestinationManager.GetDestination);
    }

    /// <summary>
    ///   将 RfcConfigParameter 转换为 SAP NCo 库所需的 RfcConfigParameters 配置字典。
    /// </summary>
    /// <param name="options">RFC 连接参数。</param>
    /// <returns>SAP NCo 连接参数配置。</returns>
    private static RfcConfigParameters GetDestinationParameters(RfcConfigParameter options)
    {
        ValidateOptions(options);

        var parameters = new RfcConfigParameters
        {
            { RfcConfigParameters.Client, options.Client },
            { RfcConfigParameters.User, options.UserName },
            { RfcConfigParameters.Password, options.Password },
            { RfcConfigParameters.Language, options.Language },
            { RfcConfigParameters.PoolSize, options.PoolSize.ToString() },
            { RfcConfigParameters.PeakConnectionsLimit, options.MaxPoolSize.ToString() },
        };

        if (!string.IsNullOrWhiteSpace(options.SystemId))
        {
            parameters[RfcConfigParameters.SystemID] = options.SystemId;
        }

        if (!string.IsNullOrWhiteSpace(options.MessageServerHost))
        {
            parameters[RfcConfigParameters.MessageServerHost] = options.MessageServerHost;
            parameters[RfcConfigParameters.MessageServerService] = string.IsNullOrWhiteSpace(
                options.MessageServerService
            )
                ? options.MessageServerPort.ToString()
                : options.MessageServerService;
            if (!string.IsNullOrWhiteSpace(options.SystemId))
            {
                parameters[RfcConfigParameters.SystemID] = options.SystemId;
            }
        }
        else
        {
            parameters[RfcConfigParameters.AppServerHost] = options.ApplicationServer;
            parameters[RfcConfigParameters.SystemNumber] = options.SystemNumber.ToString();
        }

        return parameters;
    }

    /// <summary>
    ///   验证 RFC 连接参数是否完整有效。
    /// </summary>
    /// <param name="options">要验证的 RFC 连接参数。</param>
    private static void ValidateOptions(RfcConfigParameter options)
    {
        if (string.IsNullOrWhiteSpace(options.Client))
            throw new ArgumentException("SAP client is required.", nameof(options));

        if (string.IsNullOrWhiteSpace(options.UserName))
            throw new ArgumentException("SAP user name is required.", nameof(options));

        if (string.IsNullOrWhiteSpace(options.Password))
            throw new ArgumentException("SAP password is required.", nameof(options));

        if (
            string.IsNullOrWhiteSpace(options.MessageServerHost)
            && string.IsNullOrWhiteSpace(options.ApplicationServer)
        )
        {
            throw new ArgumentException(
                "Either message server host or application server host is required.",
                nameof(options));
        }
    }

    /// <summary>
    ///   IDestinationConfiguration 接口实现。根据目标名称返回对应的连接参数。
    /// </summary>
    /// <param name="destinationName">目标名称。</param>
    /// <returns>对应的 RfcConfigParameters 配置。</returns>
    RfcConfigParameters IDestinationConfiguration.GetParameters(string destinationName)
    {
        if (!_destinations.TryGetValue(destinationName, out var options))
        {
            throw new ArgumentException($"Destination '{destinationName}' not found");
        }

        return GetDestinationParameters(options);
    }

    /// <summary>
    ///   指示此配置实现是否支持更改事件。始终返回 false。
    /// </summary>
    /// <returns>始终返回 false。</returns>
    bool IDestinationConfiguration.ChangeEventsSupported() => false;

    /// <summary>
    ///   配置更改事件。此实现中的事件处理为空。
    /// </summary>
    event RfcDestinationManager.ConfigurationChangeHandler IDestinationConfiguration.ConfigurationChanged
    {
        add { }
        remove { }
    }

    /// <summary>
    ///   释放所有托管和非托管资源。
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///   释放由 RfcConnectionManager 占用的资源。
    /// </summary>
    /// <param name="disposing">是否同时释放托管资源。</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _cleanupTimer?.Dispose();
            _cleanupTimer = null;

            _destinations.Clear();
            _destinationCache.Clear();
            _lastAccessTime.Clear();
        }

        _disposed = true;
    }

    /// <summary>
    ///   析构函数。
    /// </summary>
    ~RfcConnectionManager()
    {
        Dispose(false);
    }
}
