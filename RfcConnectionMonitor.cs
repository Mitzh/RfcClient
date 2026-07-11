using mitzh.Abstractions;

namespace mitzh;

/// <summary>
///   RFC 连接监视器的默认空实现。
///   所有监视方法均为虚方法，默认不执行任何操作，子类可通过重写来添加自定义监测逻辑。
/// </summary>
public class RfcConnectionMonitor : IRfcConnectionMonitor
{
    /// <summary>
    ///   当 RFC 目标被解析时调用。默认不执行任何操作。
    /// </summary>
    /// <param name="context">包含目标解析相关信息的上下文。</param>
    public virtual void DestinationResolved(RfcDestinationResolvedContext context)
    {
    }

    /// <summary>
    ///   当 RFC 调用开始时调用。默认不执行任何操作。
    /// </summary>
    /// <param name="context">包含调用起始信息的上下文。</param>
    public virtual void InvocationStarted(RfcInvocationContext context)
    {
    }

    /// <summary>
    ///   当 RFC 调用成功完成时调用。默认不执行任何操作。
    /// </summary>
    /// <param name="context">包含调用完成信息的上下文。</param>
    public virtual void InvocationSucceeded(RfcInvocationContext context)
    {
    }

    /// <summary>
    ///   当 RFC 调用失败时调用。默认不执行任何操作。
    /// </summary>
    /// <param name="context">包含调用信息的上下文。</param>
    /// <param name="exception">导致调用失败的异常对象。</param>
    public virtual void InvocationFailed(RfcInvocationContext context, Exception exception)
    {
    }
}
