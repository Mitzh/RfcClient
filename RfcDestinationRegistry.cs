using SAP.Middleware.Connector;
using Mitzh.Abstractions;

namespace Mitzh;

/// <summary>
///   RFC 目标注册表的默认实现。
///   在初始化时从配置提供程序加载所有连接配置，并向 <see cref="RfcConnectionManager"/> 注册。
/// </summary>
public class RfcDestinationRegistry : IRfcDestinationRegistry
{
    private readonly IRfcConfigProvider _configProvider;
    private readonly IRfcConnectionMonitor _monitor;

    /// <summary>
    ///   初始化 <see cref="RfcDestinationRegistry"/> 类的新实例。
    ///   从配置提供程序获取所有连接参数并注册到连接管理器。
    /// </summary>
    /// <param name="configProvider">RFC 配置提供程序。</param>
    /// <param name="monitor">RFC 连接监视器。</param>
    public RfcDestinationRegistry(
        IRfcConfigProvider configProvider,
        IRfcConnectionMonitor monitor)
    {
        ArgumentNullException.ThrowIfNull(configProvider);
        ArgumentNullException.ThrowIfNull(monitor);

        _configProvider = configProvider;
        _monitor = monitor;

        foreach (var item in _configProvider.GetConfigParameters())
        {
            RfcConnectionManager.RegisterDestination(item.Key, item.Value);
        }
    }

    /// <summary>
    ///   获取指定配置标识对应的 RFC 连接目标。
    /// </summary>
    /// <param name="configId">RFC 配置标识。</param>
    /// <param name="forceNew">是否强制创建新的连接目标（绕过缓存）。默认值为 false。</param>
    /// <returns>对应的 RfcDestination 实例。</returns>
    public RfcDestination GetDestination(string configId, bool forceNew = false)
    {
        var destination = RfcConnectionManager.GetDestination(configId, forceNew);
        _monitor.DestinationResolved(
            new RfcDestinationResolvedContext(
                configId,
                destination,
                GetConfigParameter(configId),
                forceNew));

        return destination;
    }

    /// <summary>
    ///   获取指定配置标识对应的连接参数。
    /// </summary>
    /// <param name="configId">RFC 配置标识。</param>
    /// <returns>对应的 RfcConfigParameter 对象。</returns>
    public RfcConfigParameter GetConfigParameter(string configId)
    {
        return _configProvider.GetConfigParameter(configId);
    }

    /// <summary>
    ///   获取所有已注册的配置标识集合。
    /// </summary>
    /// <returns>配置标识的只读集合。</returns>
    public IReadOnlyCollection<string> GetConfigIds()
    {
        return _configProvider.GetConfigParameters().Keys.ToArray();
    }
}
