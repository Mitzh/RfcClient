namespace RfcClient.Abstractions;

/// <summary>
///   SAP RFC 客户端接口。
///   提供统一的方式来调用 SAP RFC 并管理配置标识。
/// </summary>
public interface IRfcClient
{
    /// <summary>
    ///   获取或设置当前使用的 RFC 配置标识。
    /// </summary>
    string ConfigId { get; set; }

    /// <summary>
    ///   调用远程 SAP RFC 函数并获取返回值。
    /// </summary>
    /// <typeparam name="TIn">请求参数的类型，必须为类类型。</typeparam>
    /// <typeparam name="TOut">响应结果的类型，必须包含无参构造函数。</typeparam>
    /// <param name="input">请求参数对象。</param>
    /// <param name="forceNew">
    ///   是否强制使用新的连接目标（绕过缓存）。默认值为 false。
    /// </param>
    /// <returns>RFC 调用返回的结果对象。</returns>
    TOut Invoke<TIn, TOut>(TIn input, bool forceNew = false)
        where TIn : class
        where TOut : new();
}
