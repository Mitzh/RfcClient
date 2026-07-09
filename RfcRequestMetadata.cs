using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace RfcClient;

/// <summary>
///   RFC 请求元数据工具类。
///   提供从请求类型中提取函数名称和验证请求参数的功能。
/// </summary>
internal static class RfcRequestMetadata
{
    /// <summary>
    ///   从请求类型中获取 TableAttribute 定义的 RFC 函数名称。
    /// </summary>
    /// <typeparam name="TIn">请求参数类型。</typeparam>
    /// <returns>RFC 函数名称。</returns>
    /// <exception cref="InvalidOperationException">当请求类型未定义 TableAttribute 或其名称为空时引发。</exception>
    public static string GetFunctionName<TIn>()
        where TIn : class
    {
        var functionName = typeof(TIn).GetCustomAttribute<TableAttribute>()?.Name;
        if (string.IsNullOrWhiteSpace(functionName))
        {
            throw new InvalidOperationException(
                $"Request type '{typeof(TIn).FullName}' must define a TableAttribute with the RFC function name.");
        }

        return functionName;
    }

    /// <summary>
    ///   验证请求参数对象。
    ///   使用 DataAnnotations 验证器的 ValidateObject 方法检查所有属性。
    /// </summary>
    /// <typeparam name="TIn">请求参数类型。</typeparam>
    /// <param name="input">要验证的请求参数对象。</param>
    public static void Validate<TIn>(TIn input)
        where TIn : class
    {
        ArgumentNullException.ThrowIfNull(input);

        var validationContext = new ValidationContext(input);
        Validator.ValidateObject(input, validationContext, validateAllProperties: true);
    }
}
