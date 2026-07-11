using mitzh;

namespace mitzh.Abstractions;

/// <summary>
///   SAP RFC 配置提供程序接口。
///   用于获取默认配置标识和具体的连接参数。
/// </summary>
public interface IRfcConfigProvider
{
    /// <summary>
    ///   获取默认的 RFC 配置标识。
    /// </summary>
    /// <returns>默认配置标识字符串。</returns>
    string GetDefaultConfigId();

    /// <summary>
    ///   根据配置标识获取对应的 RFC 连接参数。
    /// </summary>
    /// <param name="configId">RFC 配置标识。</param>
    /// <returns>对应配置标识的 RfcConfigParameter 对象。</returns>
    RfcConfigParameter GetConfigParameter(string configId);

    /// <summary>
    ///   获取所有可用的 RFC 配置参数的只读字典。
    /// </summary>
    /// <returns>包含所有配置信息的只读字典。</returns>
    IReadOnlyDictionary<string, RfcConfigParameter> GetConfigParameters();
}
