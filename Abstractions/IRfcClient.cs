namespace Mitzh.Abstractions;

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
    ///   调用远程 SAP RFC 函数，输入可为请求类或字典。
    /// </summary>
    /// <typeparam name="TOut">响应结果的类型，必须包含无参构造函数。</typeparam>
    /// <param name="input">请求类或字典。字典键作为 SAP RFC 参数名。</param>
    /// <param name="functionName">RFC 函数名。字典输入时必填；类输入时优先于 TableAttribute。</param>
    /// <param name="forceNew">是否强制使用新的连接目标（绕过缓存）。</param>
    /// <param name="configId">本次调用使用的配置标识，优先于实例级 ConfigId。</param>
    /// <returns>RFC 调用返回的结果对象。</returns>
    TOut Invoke<TOut>(object input, string functionName = null, bool forceNew = false, string configId = null)
        where TOut : new();
}
