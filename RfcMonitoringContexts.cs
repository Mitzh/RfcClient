using SAP.Middleware.Connector;

namespace mitzh;

/// <summary>
///   包含 RFC 目标（Destination）解析完成后的上下文信息。
/// </summary>
public class RfcDestinationResolvedContext
{
    /// <summary>
    ///   初始化 <see cref="RfcDestinationResolvedContext"/> 类的新实例。
    /// </summary>
    /// <param name="configId">RFC 配置标识。</param>
    /// <param name="destination">解析后的 RFC 连接目标。</param>
    /// <param name="configParameter">对应的连接参数。</param>
    /// <param name="forceNew">是否强制创建了新连接。</param>
    public RfcDestinationResolvedContext(
        string configId,
        RfcDestination destination,
        RfcConfigParameter configParameter,
        bool forceNew)
    {
        ConfigId = configId;
        Destination = destination;
        ConfigParameter = configParameter;
        ForceNew = forceNew;
        ResolvedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    ///   获取 RFC 配置标识。
    /// </summary>
    public string ConfigId { get; }

    /// <summary>
    ///   获取解析后的 SAP NCo 连接目标。
    /// </summary>
    public RfcDestination Destination { get; }

    /// <summary>
    ///   获取该目标的连接参数配置。
    /// </summary>
    public RfcConfigParameter ConfigParameter { get; }

    /// <summary>
    ///   获取一个值，指示是否强制创建了新的连接实例。
    /// </summary>
    public bool ForceNew { get; }

    /// <summary>
    ///   获取目标解析完成的时间戳（UTC）。
    /// </summary>
    public DateTimeOffset ResolvedAt { get; }
}

/// <summary>
///   包含 RFC 函数调用的上下文信息，包括配置、函数名、请求/响应类型和耗时统计。
/// </summary>
public class RfcInvocationContext
{
    /// <summary>
    ///   初始化 <see cref="RfcInvocationContext"/> 类的新实例。
    /// </summary>
    /// <param name="configId">RFC 配置标识。</param>
    /// <param name="functionName">被调用的 RFC 函数名称。</param>
    /// <param name="requestType">请求参数的类型。</param>
    /// <param name="responseType">响应结果的类型。</param>
    public RfcInvocationContext(
        string configId,
        string functionName,
        Type requestType,
        Type responseType)
    {
        ConfigId = configId;
        FunctionName = functionName;
        RequestType = requestType;
        ResponseType = responseType;
        StartedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    ///   获取 RFC 配置标识。
    /// </summary>
    public string ConfigId { get; }

    /// <summary>
    ///   获取被调用的 RFC 函数名称。
    /// </summary>
    public string FunctionName { get; }

    /// <summary>
    ///   获取请求参数的类型。
    /// </summary>
    public Type RequestType { get; }

    /// <summary>
    ///   获取响应结果的类型。
    /// </summary>
    public Type ResponseType { get; }

    /// <summary>
    ///   获取调用开始的时间戳（UTC）。
    /// </summary>
    public DateTimeOffset StartedAt { get; }

    /// <summary>
    ///   获取调用耗时。
    /// </summary>
    public TimeSpan Elapsed { get; private set; }

    /// <summary>
    ///   完成调用并记录耗时。
    /// </summary>
    /// <param name="elapsed">调用耗时。</param>
    public void Complete(TimeSpan elapsed)
    {
        Elapsed = elapsed;
    }
}
