using SAP.Middleware.Connector;
using mitzh;

namespace mitzh.Abstractions;

/// <summary>
///   RFC 目标注册表接口。
///   用于获取已配置的 RFC 连接目标及其关联的配置参数。
/// </summary>
public interface IRfcDestinationRegistry
{
    /// <summary>
    ///   获取指定配置标识对应的 RFC 连接目标。
    /// </summary>
    /// <param name="configId">RFC 配置标识。</param>
    /// <param name="forceNew">
    ///   是否强制创建新的连接目标（绕过缓存）。默认值为 false。
    /// </param>
    /// <returns>对应的 RfcDestination 实例。</returns>
    RfcDestination GetDestination(string configId, bool forceNew = false);

    /// <summary>
    ///   获取指定配置标识对应的连接参数。
    /// </summary>
    /// <param name="configId">RFC 配置标识。</param>
    /// <returns>对应的 RfcConfigParameter 对象。</returns>
    RfcConfigParameter GetConfigParameter(string configId);

    /// <summary>
    ///   获取所有已注册的配置标识集合。
    /// </summary>
    /// <returns>配置标识的只读集合。</returns>
    IReadOnlyCollection<string> GetConfigIds();
}
