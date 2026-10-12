namespace Mitzh;

/// <summary>
///   SAP RFC 连接配置参数。
///   包含连接 SAP 系统所需的所有参数，包括服务器地址、登录凭据、连接池设置等。
/// </summary>
public class RfcConfigParameter
{
    /// <summary>
    ///   获取或设置 SAP 应用服务器的主机名或 IP 地址。
    /// </summary>
    public string AppServerHost { get; set; } = string.Empty;

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

    /// <summary>
    ///   获取或设置一个值，指示是否对传给 SAP 的数值类型自动补全前导零。
    ///   <para>
    ///   SAP 的字符型数值字段（如客户号 KUNNR、物料号 MATNR）通常要求固定长度，
    ///   启用此选项后，整型和纯数字字符串类型的值将根据 SAP 字段元数据的长度自动左补零。
    ///   例如字段长度为 10 时，<c>123</c> 将被转换为 <c>0000000123</c>。
    ///   </para>
    ///   <para>默认值为 <see langword="false"/>，即不补全前导零。</para>
    /// </summary>
    public bool PadLeadingZeros { get; set; }
}
