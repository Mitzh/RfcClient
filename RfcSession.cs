using System.Diagnostics;
using System.Collections;
using SAP.Middleware.Connector;
using Mitzh.Abstractions;

namespace Mitzh;

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
    ///   使用请求类或字典调用远程 SAP RFC 函数。
    /// </summary>
    /// <param name="input">请求类或字典。字典键作为 SAP RFC 参数名。</param>
    /// <param name="functionName">RFC 函数名。字典输入时必填；类输入时优先于 TableAttribute。</param>
    /// <param name="forceNew">是否强制使用新的连接目标（绕过缓存）。</param>
    public TOut Invoke<TOut>(object input, string functionName = null, bool forceNew = false)
        where TOut : new()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(RfcSession));
        }

        ArgumentNullException.ThrowIfNull(input);

        var isDictionary = input is IDictionary;
        if (isDictionary && string.IsNullOrWhiteSpace(functionName))
        {
            throw new ArgumentException("Function name is required when input is a dictionary.", nameof(functionName));
        }

        var inputType = input.GetType();
        if (!isDictionary && !inputType.IsClass)
        {
            throw new ArgumentException("Input must be a class or dictionary.", nameof(input));
        }

        var rfcName = !string.IsNullOrWhiteSpace(functionName)
            ? functionName
            : RfcRequestMetadata.GetFunctionName(inputType);

        if (!isDictionary)
        {
            RfcRequestMetadata.Validate(input);
        }

        var context = new RfcInvocationContext(ConfigId, rfcName, inputType, typeof(TOut));
        var stopwatch = Stopwatch.StartNew();
        _monitor.InvocationStarted(context);

        try
        {
            var destination = _destinationRegistry.GetDestination(ConfigId, forceNew);
            var function = destination.Repository.CreateFunction(rfcName);
            if (input is IDictionary dictionary)
            {
                function.SetInputValue(dictionary);
            }
            else
            {
                function.SetInputValue(input);
            }

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
                $"Failed to invoke RFC function '{rfcName}' on config '{ConfigId}'.", ex);
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
