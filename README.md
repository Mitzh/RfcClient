# RfcClient

`RfcClient` is a DI-friendly SAP RFC client wrapper for SAP .NET Connector.

It targets `.NET 10.0`. The library supports multiple SAP RFC connection configs, request-scoped `ConfigId` switching, typed request/response mapping, and connection/invocation monitoring hooks.

Additional documentation:

- [Project analysis and maintenance guide](docs/PROJECT_ANALYSIS.en-US.md)
- [中文项目分析与维护指南](docs/PROJECT_ANALYSIS.zh-CN.md)

## Features

- Multiple SAP RFC connection configs by `ConfigId`
- Default config selection through `IsDefault`
- ASP.NET Core / generic host dependency injection support
- Request-scoped RFC session switching through `IRfcClient.ConfigId`
- Typed RFC input/output mapping with `[Table]` and `[Column]`
- Connection and invocation monitoring extension points
- SAP NCo runtime files included from project `libs`

## Installation

Install the NuGet package:

```bash
dotnet add package RfcClient
```

The package targets `net10.0`.

## Configuration

Add RFC connection configs to `appsettings.json`:

```json
{
  "RfcConnectionConfigs": [
    {
      "ConfigId": "Sap",
      "IsDefault": true,
      "ConnectionString": "ApplicationServer=192.168.1.65;SystemNumber=00;SystemId=DEV;Client=800;UserName=DEVUSER;Password=******;Language=ZH;PoolSize=5;MaxPoolSize=10;ConnectionTimeout=30;CommunicationTimeout=60;"
    },
    {
      "ConfigId": "Sap.JSY",
      "IsDefault": false,
      "ConnectionString": "MessageServerHost=192.168.1.65;MessageServerService=3600;SystemId=DEV;Client=800;UserName=DEVUSER;Password=******;Language=ZH;PoolSize=5;MaxPoolSize=10;"
    }
  ]
}
```

Supported `ConnectionString` parameters map to `RfcConfigParameter`:

```text
ApplicationServer
Server
SystemNumber
SystemId
Client
UserName
User ID
UserId
Password
Language
PoolSize
MaxPoolSize
Max Pool Size
ConnectionTimeout
CommunicationTimeout
MessageServerHost
MessageServerService
MessageServerPort
```

`ApplicationServer` / `Server` is used for direct application server connections. `MessageServerHost` is used for message server connections.

## Register Services

Register the client with `IServiceCollection`:

```csharp
using RfcClient;

builder.Services.AddRfcClient(builder.Configuration);
```

If your SAP RFC settings live under a section:

```csharp
builder.Services.AddRfcClient(
    builder.Configuration.GetSection("Rfc"));
```

Programmatic registration is also supported:

```csharp
using RfcClient;

builder.Services.AddRfcClient(options =>
{
    options.RfcConnectionConfigs.Add(new RfcConnectionConfig
    {
        ConfigId = "Sap",
        IsDefault = true,
        ConnectionString = "ApplicationServer=192.168.1.65;SystemNumber=00;Client=800;UserName=DEVUSER;Password=******;"
    });
});
```

## Define RFC Models

Use `[Table]` on the request type to declare the RFC function name. Use `[Column]` on properties to map SAP RFC parameter names.

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("ZFM_MM039")]
public class SupplyDemandRequest
{
    [Required]
    [Column("IV_MATNR")]
    public string MaterialCode { get; set; } = string.Empty;

    [Required]
    [Column("IV_BUKRS")]
    public string CompanyCode { get; set; } = string.Empty;

    [Column("IV_WERKS")]
    public string PlantCode { get; set; } = string.Empty;

    [Required]
    [Column("IV_SYSTEM")]
    public string Source { get; set; } = string.Empty;
}
```

```csharp
using System.ComponentModel.DataAnnotations.Schema;

public class SupplyDemandResponse
{
    [Column("EV_STATUS")]
    public string Status { get; set; } = string.Empty;

    [Column("EV_MESSAGE")]
    public string Message { get; set; } = string.Empty;

    [Column("ET_DATA")]
    public SupplyDemandRow[] Rows { get; set; } = Array.Empty<SupplyDemandRow>();
}

public class SupplyDemandRow
{
    [Column("MATNR")]
    public string MaterialCode { get; set; } = string.Empty;

    [Column("WERKS")]
    public string PlantCode { get; set; } = string.Empty;

    [Column("BDMNG")]
    public decimal Quantity { get; set; }
}
```

## Basic Call

Inject `IRfcClient` and invoke the RFC with typed request/response models:

```csharp
using RfcClient.Abstractions;

public class SupplyDemandService
{
    private readonly IRfcClient _rfcClient;

    public SupplyDemandService(IRfcClient rfcClient)
    {
        _rfcClient = rfcClient;
    }

    public SupplyDemandResponse Query()
    {
        var request = new SupplyDemandRequest
        {
            MaterialCode = "B0505XT-1WR3",
            CompanyCode = "1100",
            PlantCode = "",
            Source = "C"
        };

        return _rfcClient.Invoke<SupplyDemandRequest, SupplyDemandResponse>(request);
    }
}
```

`IRfcClient` exposes a scoped `ConfigId` property. If `ConfigId` is empty, the client uses the config marked with `IsDefault=true`. If no config is marked as default, it uses the first item in `RfcConnectionConfigs`.

## Switch ConfigId Per Request

Set `IRfcClient.ConfigId` inside the current request scope. After that, all `IRfcClient` calls in the same scope use that config automatically.

Example middleware:

```csharp
using RfcClient.Abstractions;

app.Use(async (context, next) =>
{
    var rfcClient = context.RequestServices.GetRequiredService<IRfcClient>();
    var configId = context.Request.Headers["X-Sap-Rfc-ConfigId"].FirstOrDefault();

    if (!string.IsNullOrWhiteSpace(configId))
    {
        rfcClient.ConfigId = configId;
    }

    await next();
});
```

Example controller:

```csharp
using Microsoft.AspNetCore.Mvc;
using RfcClient.Abstractions;

[ApiController]
[Route("api/supply-demand")]
public class SupplyDemandController : ControllerBase
{
    private readonly IRfcClient _rfcClient;

    public SupplyDemandController(IRfcClient rfcClient)
    {
        _rfcClient = rfcClient;
    }

    [HttpPost]
    public ActionResult<SupplyDemandResponse> Query(SupplyDemandRequest request)
    {
        var response = _rfcClient.Invoke<SupplyDemandRequest, SupplyDemandResponse>(request);
        return Ok(response);
    }
}
```

Calling the API with a header switches the SAP RFC config for that request:

```http
X-Sap-Rfc-ConfigId: Sap.JSY
```

## Call With Explicit ConfigId

Set `IRfcClient.ConfigId` before invoking when you need explicit control over the RFC config:

```csharp
using RfcClient.Abstractions;

public class ManualRfcService
{
    private readonly IRfcClient _rfcClient;

    public ManualRfcService(IRfcClient rfcClient)
    {
        _rfcClient = rfcClient;
    }

    public SupplyDemandResponse QueryWithJsy(SupplyDemandRequest request)
    {
        _rfcClient.ConfigId = "Sap.JSY";
        return _rfcClient.Invoke<SupplyDemandRequest, SupplyDemandResponse>(request);
    }
}
```

Set `ConfigId` back to an empty string to return to the default configured SAP RFC connection in the same scope.

## Monitor Connections And Calls

Implement `IRfcConnectionMonitor` to observe resolved destinations and RFC invocations:

```csharp
using Microsoft.Extensions.Logging;
using RfcClient;
using RfcClient.Abstractions;

public class LoggingRfcConnectionMonitor : IRfcConnectionMonitor
{
    private readonly ILogger<LoggingRfcConnectionMonitor> _logger;

    public LoggingRfcConnectionMonitor(ILogger<LoggingRfcConnectionMonitor> logger)
    {
        _logger = logger;
    }

    public void DestinationResolved(RfcDestinationResolvedContext context)
    {
        _logger.LogInformation(
            "SAP RFC destination resolved. ConfigId={ConfigId}, ForceNew={ForceNew}, PoolSize={PoolSize}, MaxPoolSize={MaxPoolSize}",
            context.ConfigId,
            context.ForceNew,
            context.ConfigParameter.PoolSize,
            context.ConfigParameter.MaxPoolSize);
    }

    public void InvocationStarted(RfcInvocationContext context)
    {
        _logger.LogInformation(
            "SAP RFC invocation started. ConfigId={ConfigId}, Function={Function}",
            context.ConfigId,
            context.FunctionName);
    }

    public void InvocationSucceeded(RfcInvocationContext context)
    {
        _logger.LogInformation(
            "SAP RFC invocation succeeded. ConfigId={ConfigId}, Function={Function}, Elapsed={ElapsedMs}ms",
            context.ConfigId,
            context.FunctionName,
            context.Elapsed.TotalMilliseconds);
    }

    public void InvocationFailed(RfcInvocationContext context, Exception exception)
    {
        _logger.LogError(
            exception,
            "SAP RFC invocation failed. ConfigId={ConfigId}, Function={Function}, Elapsed={ElapsedMs}ms",
            context.ConfigId,
            context.FunctionName,
            context.Elapsed.TotalMilliseconds);
    }
}
```

Register the monitor before or after `AddRfcClient`:

```csharp
using RfcClient.Abstractions;

builder.Services.AddSingleton<IRfcConnectionMonitor, LoggingRfcConnectionMonitor>();
builder.Services.AddRfcClient(builder.Configuration);
```

Do not log passwords or full connection strings.

## Runtime Files

The project expects SAP NCo runtime files in the package/library `libs` folder:

```text
libs/cpc4n.dll
libs/ijwhost.dll
libs/sapnco.dll
libs/sapnco_utils.dll
```

These files are copied to the build output and included in the NuGet package under `lib/net10.0/`.

Configuration options: the library exposes two timing options in `RfcOptions`:

- `CleanupInterval` (default `00:05:00`): how often the client checks for idle destinations to cleanup.
- `DestinationIdleTimeout` (default `00:10:00`): how long a destination can stay unused before being removed from the cache.

You can set these in `appsettings.json` or via code when registering the services.

## Build And Pack

```bash
dotnet build .\RfcClient.sln
dotnet pack .\RfcClient.csproj -c Release
```

The package is generated under:

```text
bin/Release/RfcClient.0.1.0.nupkg
```
