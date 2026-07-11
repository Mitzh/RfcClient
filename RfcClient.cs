using mitzh.Abstractions;

using Microsoft.Extensions.Options;

namespace mitzh;

/// <summary>
///   SAP RFC 客户端的作用域实现。
///   在每个作用域内创建 RFC 会话来调用远程函数，并自动管理配置标识的解析。
///   此实现应通过依赖注入以作用域（Scoped）生命周期注册。
/// </summary>
public class RfcClient : IRfcClient
{
    private IRfcDestinationRegistry _destinationRegistry;
    private IRfcConfigProvider _configProvider;
    private IRfcConnectionMonitor _monitor;
    private bool _hasCustomDestinationRegistry;

    /// <summary>
    ///   初始化 <see cref="RfcClient"/> 类的新实例。
    ///   依赖项可通过属性注入；未注入时将使用项目内的默认实现。
    /// </summary>
    public RfcClient()
    {
    }

    /// <summary>
    ///   初始化 <see cref="RfcClient"/> 类的新实例。
    /// </summary>
    /// <param name="destinationRegistry">RFC 目标注册表。</param>
    /// <param name="configProvider">RFC 配置提供程序。</param>
    /// <param name="monitor">RFC 连接监视器。</param>
    public RfcClient(
        IRfcDestinationRegistry destinationRegistry,
        IRfcConfigProvider configProvider,
        IRfcConnectionMonitor monitor)
    {
        ArgumentNullException.ThrowIfNull(destinationRegistry);
        ArgumentNullException.ThrowIfNull(configProvider);
        ArgumentNullException.ThrowIfNull(monitor);

        _destinationRegistry = destinationRegistry;
        _configProvider = configProvider;
        _monitor = monitor;
        _hasCustomDestinationRegistry = true;
    }

    /// <summary>
    ///   获取或设置 RFC 连接监视器。未设置时使用 <see cref="RfcConnectionMonitor"/>。
    /// </summary>
    public virtual IRfcConnectionMonitor ConnectionMonitor
    {
        get => _monitor ??= new RfcConnectionMonitor();
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _monitor = value;
            ResetDefaultDestinationRegistry();
        }
    }

    /// <summary>
    ///   获取或设置 RFC 配置提供程序。未设置时使用基于空配置的 <see cref="RfcConfigProvider"/>。
    /// </summary>
    public virtual IRfcConfigProvider ConfigProvider
    {
        get => _configProvider ??= new RfcConfigProvider(Options.Create(new RfcOptions()));
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _configProvider = value;
            ResetDefaultDestinationRegistry();
        }
    }

    /// <summary>
    ///   获取或设置 RFC 目标注册表。未设置时使用 <see cref="RfcDestinationRegistry"/>。
    /// </summary>
    public virtual IRfcDestinationRegistry DestinationRegistry
    {
        get => _destinationRegistry ??= new RfcDestinationRegistry(ConfigProvider, ConnectionMonitor);
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _destinationRegistry = value;
            _hasCustomDestinationRegistry = true;
        }
    }

    /// <summary>
    ///   获取或设置当前使用的 RFC 配置标识。
    ///   如果未设置，则使用配置提供程序返回的默认配置标识。
    ///   若设置的 ConfigId 未在 RfcConnectionConfigs 中注册，将自动回退到默认配置。
    /// </summary>
    public virtual string ConfigId { get; set; } = string.Empty;

    /// <summary>
    ///   调用远程 SAP RFC 函数，输入可为请求类或字典。
    /// </summary>
    /// <typeparam name="TOut">响应结果的类型，必须包含无参构造函数。</typeparam>
    /// <param name="input">请求类或字典。字典键作为 SAP RFC 参数名。</param>
    /// <param name="functionName">RFC 函数名。字典输入时必填；类输入时优先于 TableAttribute。</param>
    /// <param name="forceNew">是否强制使用新的连接目标（绕过缓存）。</param>
    /// <param name="configId">本次调用使用的配置标识。优先于实例的 <see cref="ConfigId"/>。</param>
    /// <returns>RFC 调用返回的结果对象。</returns>
    public virtual TOut Invoke<TOut>(
        object input,
        string functionName = null,
        bool forceNew = false,
        string configId = null)
        where TOut : new()
    {
        var effectiveConfigId = !string.IsNullOrWhiteSpace(configId)
            ? configId
            : !string.IsNullOrWhiteSpace(ConfigId)
                ? ConfigId
                : ConfigProvider.GetDefaultConfigId();

        if (!string.IsNullOrWhiteSpace(effectiveConfigId)
            && !RfcConnectionManager.IsDestinationRegistered(effectiveConfigId))
        {
            effectiveConfigId = ConfigProvider.GetDefaultConfigId();
        }

        using var session = new RfcSession(effectiveConfigId, DestinationRegistry, ConnectionMonitor);
        return session.Invoke<TOut>(input, functionName, forceNew);
    }

    private void ResetDefaultDestinationRegistry()
    {
        if (!_hasCustomDestinationRegistry)
        {
            _destinationRegistry = null;
        }
    }
}
