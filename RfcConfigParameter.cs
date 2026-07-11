namespace mitzh;

/// <summary>
///   SAP RFC 连接配置参数。
///   包含连接 SAP 系统所需的所有参数，包括服务器地址、登录凭据、连接池设置等。
/// </summary>
public class RfcConfigParameter
{
    /// <summary>
    ///   获取或设置 SAP 应用服务器的主机名或 IP 地址。
    /// </summary>
    public string ApplicationServer { get; set; } = string.Empty;

    /// <summary>
    ///   获取或设置 SAP 系统编号（00-99）。默认值为 "00"。
    /// </summary>
    public string SystemNumber { get; set; } = "00";

    /// <summary>
    ///   获取或设置 SAP 系统标识（SID），如 "PRD"。
    /// </summary>
    public string SystemId { get; set; } = string.Empty;

    /// <summary>
    ///   获取或设置 SAP 客户端编号，如 "800"。
    /// </summary>
    public string Client { get; set; } = string.Empty;

    /// <summary>
    ///   获取或设置用于登录 SAP 的用户名。
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    ///   获取或设置用于登录 SAP 的密码。
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    ///   获取或设置登录语言代码。默认值为 "ZH"（中文）。
    /// </summary>
    public string Language { get; set; } = "ZH";

    /// <summary>
    ///   获取或设置连接池的初始大小。默认值为 5。
    /// </summary>
    public int PoolSize { get; set; } = 5;

    /// <summary>
    ///   获取或设置连接池的最大连接数。默认值为 10。
    /// </summary>
    public int MaxPoolSize { get; set; } = 10;

    /// <summary>
    ///   获取或设置连接超时时间（秒）。默认值为 30。
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30;

    /// <summary>
    ///   获取或设置通信超时时间（秒）。默认值为 60。
    /// </summary>
    public int CommunicationTimeout { get; set; } = 60;

    /// <summary>
    ///   获取或设置 SAP 消息服务器的主机名或 IP 地址。
    ///   当使用消息服务器（负载均衡）模式时必填。
    /// </summary>
    public string MessageServerHost { get; set; } = string.Empty;

    /// <summary>
    ///   获取或设置消息服务器的服务名称或端口号字符串。
    /// </summary>
    public string MessageServerService { get; set; } = string.Empty;

    /// <summary>
    ///   获取或设置消息服务器的端口号。默认值为 3600。
    /// </summary>
    public int MessageServerPort { get; set; } = 3600;
}
