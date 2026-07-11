# RfcClient 项目分析与维护指南

## 项目概览

`RfcClient` 是一个面向 .NET 10 的类库，用于在 SAP .NET Connector (NCo) 之上提供依赖注入、命名 RFC 配置、强类型请求/响应映射、作用域级配置切换，以及连接和调用监控钩子。

公开实现类型位于 `mitzh` 命名空间，抽象接口位于 `mitzh.Abstractions`。`RfcClient` 同时支持 Microsoft DI 构造注入和 Autofac Module 属性注入；当前调用入口为 `Invoke<TOut>(object input, string functionName = null, bool forceNew = false, string configId = null)`。

项目要求 SAP NCo 运行时文件位于 `libs/` 目录：

- `cpc4n.dll`
- `ijwhost.dll`
- `sapnco.dll`
- `sapnco_utils.dll`

## 架构说明

主要调用链如下：

```text
IRfcClient
  -> RfcClient
  -> RfcSession
  -> IRfcDestinationRegistry
  -> RfcConnectionManager
  -> SAP.Middleware.Connector.RfcDestination
```

核心组件职责：

- `RfcServiceCollectionExtensions`：注册依赖注入服务。
- `IRfcClient`：暴露 `ConfigId` 和强类型 RFC 调用。
- `RfcClient`：解析实际使用的 `ConfigId`，创建短生命周期 `RfcSession`，并委托执行调用。
- `RfcOptions`：校验配置并将连接字符串转换为 RFC 参数。
- `RfcConfigProvider`：提供 RFC 连接配置，并应用连接清理参数。
- `RfcDestinationRegistry`：注册和解析命名 SAP destination。
- `RfcConnectionManager`：管理 SAP NCo destination 配置、destination 缓存和空闲清理。
- `RfcSession`：执行强类型 RFC 调用，并发送监控事件。
- `RfcTypeConverter`：根据 `[Column]` 映射 SAP RFC 标量、结构和表数据。
- `RfcRequestMetadata`：集中处理请求模型校验和 RFC 函数名解析。

## 使用摘要

注册服务：

```csharp
builder.Services.AddRfcClient(builder.Configuration);
```

配置命名连接：

```json
{
  "RfcConnectionConfigs": [
    {
      "ConfigId": "Sap",
      "IsDefault": true,
      "ConnectionString": "ApplicationServer=192.168.1.65;SystemNumber=00;Client=800;UserName=DEVUSER;Password=******;Language=ZH;"
    }
  ]
}
```

调用 RFC：

```csharp
public sealed class SupplyDemandService
{
    private readonly IRfcClient _rfcClient;

    public SupplyDemandService(IRfcClient rfcClient)
    {
        _rfcClient = rfcClient;
    }

    public SupplyDemandResponse Query(SupplyDemandRequest request)
    {
        return _rfcClient.Invoke<SupplyDemandResponse>(request);
    }
}
```

在当前 scope 内切换 SAP 配置：

```csharp
_rfcClient.ConfigId = "Sap.JSY";
var response = _rfcClient.Invoke<SupplyDemandResponse>(request);
```

请求类型必须使用 `[Table("RFC_FUNCTION_NAME")]` 标记 RFC 函数名；请求和响应模型中需要映射的属性应使用 `[Column("SAP_FIELD_NAME")]`。

## 本次重构内容

本次维护调整了作用域级配置切换 API：

- 在 `IRfcClient` 上新增 `ConfigId`。
- 移除之前面向 DI 的配置访问器和会话工厂服务。
- 将实际 `ConfigId` 解析与 session 创建移动到 `RfcClient`。
- 保留 `RfcSession` 作为每次调用的内部执行对象。

项目中已有的维护内容还包括：

- 新增 `RfcRequestMetadata`，集中处理请求模型校验和 RFC 函数名解析。
- `RfcSession` 复用集中化的请求元数据逻辑。
- 修复 `AddRfcClient(RfcOptions)` 未复制连接清理参数的问题。
- 在 `RfcOptions` 中增加空 `ConfigId`、重复 `ConfigId` 和无效默认 `ConfigId` 校验。
- 在 `RfcConnectionManager` 中增加清理间隔参数校验。
- 简化空闲 destination 清理逻辑和 destination 缓存创建逻辑。
- 合并 `RfcTypeConverter` 中参数与字段转换的重复 switch 逻辑。
- 更新 README 中目标框架和运行时文件打包路径说明。

## 构建与验证

构建：

```bash
dotnet build .\RfcClient.sln
```

打包：

```bash
dotnet pack .\RfcClient.csproj -c Release
```

## 维护建议

- 保持目标框架、Microsoft.Extensions 依赖版本和 README 中的框架说明一致。
- 连接字符串和密码属于敏感信息，不要记录完整连接字符串。
- 扩展映射能力前，优先为 `RfcOptions`、`RfcClient` 和 `RfcTypeConverter` 增加单元测试。
- 如果需要支持旧版本业务系统，应评估多目标框架，而不是只修改 README 文档。
- SAP NCo 文件具有平台相关性，发布前应确认目标系统上的运行时文件布局和架构一致。
