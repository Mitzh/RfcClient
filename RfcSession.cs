using System.Diagnostics;
using SAP.Middleware.Connector;
using RfcClient.Abstractions;

namespace RfcClient;

/// <summary>
///   RFC 会话。
///   代表一次 SAP RFC 调用会话，负责管理目标解析、请求验证、函数调用、结果转换和监视器通知。
///   此类型为内部类型，通过 <see cref="RfcClient"/> 使用。
/// </summary>
internal sealed class RfcSession : IDisposable
{
    private readonly IRfcDestinationRegistry _destinationRegistry;
    private readonly IRfcConnectionMonitor _monitor;
    private bool _disposed;

    /// <summary>
    ///   初始化 <see cref="RfcSession"/> 类的新实例。
    /// </summary>
    /// <param name="configId">RFC 配置标识。</param>
    /// <param name="destinationRegistry">RFC 目标注册表。</param>
    /// <param name="monitor">RFC 连接监视器。</param>
    public RfcSession(
        string configId,
        IRfcDestinationRegistry destinationRegistry,
        IRfcConnectionMonitor monitor)
    {
        if (string.IsNullOrWhiteSpace(configId))
        {
            throw new ArgumentNullException(nameof(configId));
        }

        ArgumentNullException.ThrowIfNull(destinationRegistry);
        ArgumentNullException.ThrowIfNull(monitor);

        ConfigId = configId;
        _destinationRegistry = destinationRegistry;
        _monitor = monitor;
    }

    /// <summary>
    ///   获取与此会话关联的 RFC 配置标识。
    /// </summary>
    public string ConfigId { get; }

    /// <summary>
    ///   在会话上下文中调用远程 SAP RFC 函数并获取返回值。
    ///   内部处理请求验证、函数调用、耗时统计和监视器事件通知。
    /// </summary>
    /// <typeparam name="TIn">请求参数的类型，必须为类类型。</typeparam>
    /// <typeparam name="TOut">响应结果的类型，必须包含无参构造函数。</typeparam>
    /// <param name="input">请求参数对象。</param>
    /// <param name="forceNew">是否强制使用新的连接目标（绕过缓存）。默认值为 false。</param>
    /// <returns>RFC 调用返回的结果对象。</returns>
    public TOut Invoke<TIn, TOut>(TIn input, bool forceNew = false)
        where TIn : class
        where TOut : new()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(RfcSession));
        }

        var rfcName = RfcRequestMetadata.GetFunctionName<TIn>();
        RfcRequestMetadata.Validate(input);

        var context = new RfcInvocationContext(ConfigId, rfcName, typeof(TIn), typeof(TOut));
        var stopwatch = Stopwatch.StartNew();
        _monitor.InvocationStarted(context);

        try
        {
            var destination = _destinationRegistry.GetDestination(ConfigId, forceNew);
            var repository = destination.Repository;
            var function = repository.CreateFunction(rfcName);
            function.SetInputValue(input);
            function.Invoke(destination);

            var result = function.GetOutputValue<TOut>();
            stopwatch.Stop();
            context.Complete(stopwatch.Elapsed);
            _monitor.InvocationSucceeded(context);
            return result;
        }
        catch (RfcBaseException ex)
        {
            stopwatch.Stop();
            context.Complete(stopwatch.Elapsed);

            var exception = new InvalidOperationException(
                $"Failed to invoke RFC function '{rfcName}' on config '{ConfigId}'.",
                ex);
            _monitor.InvocationFailed(context, exception);
            throw exception;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            context.Complete(stopwatch.Elapsed);
            _monitor.InvocationFailed(context, ex);
            throw;
        }
    }

    /// <summary>
    ///   释放会话资源。
    ///   将会话标记为已释放，后续调用 Invoke 将引发 ObjectDisposedException。
    /// </summary>
    public void Dispose()
    {
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
