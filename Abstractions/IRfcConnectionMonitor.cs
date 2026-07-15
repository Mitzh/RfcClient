using Mitzh;

namespace Mitzh.Abstractions;

/// <summary>
///   RFC 连接监视器接口。
///   用于跟踪目标解析、调用开始、成功和失败等事件。
/// </summary>
public interface IRfcConnectionMonitor
{
    /// <summary>
    ///   当 RFC 目标（Destination）被解析时触发。
    /// </summary>
    /// <param name="context">包含目标解析相关信息的上下文。</param>
    void DestinationResolved(RfcDestinationResolvedContext context);

    /// <summary>
    ///   当 RFC 调用开始时触发。
    /// </summary>
    /// <param name="context">包含调用起始信息的上下文。</param>
    void InvocationStarted(RfcInvocationContext context);

    /// <summary>
    ///   当 RFC 调用成功完成时触发。
    /// </summary>
    /// <param name="context">包含调用完成信息的上下文。</param>
    void InvocationSucceeded(RfcInvocationContext context);

    /// <summary>
    ///   当 RFC 调用失败时触发。
    /// </summary>
    /// <param name="context">包含调用信息的上下文。</param>
    /// <param name="exception">导致调用失败的异常对象。</param>
    void InvocationFailed(RfcInvocationContext context, Exception exception);
}
