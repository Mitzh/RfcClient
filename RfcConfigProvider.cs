using Microsoft.Extensions.Options;
using RfcClient.Abstractions;

namespace RfcClient;

/// <summary>
///   RFC 配置提供程序的默认实现。
///   从 <see cref="RfcOptions"/> 中读取配置，并向连接管理器注册清理参数。
/// </summary>
public class RfcConfigProvider : IRfcConfigProvider
{
    private readonly RfcOptions _options;

    /// <summary>
    ///   初始化 <see cref="RfcConfigProvider"/> 类的新实例。
    /// </summary>
    /// <param name="options">包含 RFC 连接配置的选项对象。</param>
    public RfcConfigProvider(IOptions<RfcOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        // Apply cleanup configuration for connection manager from options
        RfcConnectionManager.ConfigureCleanup(_options.CleanupInterval, _options.DestinationIdleTimeout);
    }

    /// <summary>
    ///   获取默认的 RFC 配置标识。
    /// </summary>
    /// <returns>默认配置标识字符串。</returns>
    public string GetDefaultConfigId()
    {
        return _options.GetConfigId();
    }

    /// <summary>
    ///   根据配置标识获取对应的 RFC 连接参数。
    /// </summary>
    /// <param name="configId">RFC 配置标识。</param>
    /// <returns>对应配置标识的 RfcConfigParameter 对象。</returns>
    public RfcConfigParameter GetConfigParameter(string configId)
    {
        return _options.GetRfcOptions(configId);
    }

    /// <summary>
    ///   获取所有可用的 RFC 配置参数的只读字典。
    /// </summary>
    /// <returns>包含所有配置信息的只读字典。</returns>
    public IReadOnlyDictionary<string, RfcConfigParameter> GetConfigParameters()
    {
        return _options.GetRfcDestinations();
    }
}
