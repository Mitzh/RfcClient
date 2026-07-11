# RfcClient Project Analysis and Maintenance Guide

## Project Snapshot

`RfcClient` is a .NET 10 class library that wraps SAP .NET Connector (NCo) with dependency injection, named RFC configurations, typed request/response mapping, scoped configuration switching, and monitoring hooks.

Public implementation types use the `mitzh` namespace, while abstractions use `mitzh.Abstractions`. `RfcClient` supports Microsoft DI constructor injection and Autofac Module property injection. Its current invocation entry point is `Invoke<TOut>(object input, string functionName = null, bool forceNew = false)`.

The package expects SAP NCo runtime files under `libs/`:

- `cpc4n.dll`
- `ijwhost.dll`
- `sapnco.dll`
- `sapnco_utils.dll`

## Architecture

Main runtime flow:

```text
IRfcClient
  -> RfcClient
  -> RfcSession
  -> IRfcDestinationRegistry
  -> RfcConnectionManager
  -> SAP.Middleware.Connector.RfcDestination
```

Important components:

- `RfcServiceCollectionExtensions`: registers the DI services.
- `IRfcClient`: exposes `ConfigId` and typed RFC invocation.
- `RfcClient`: `IRfcClient` implementation, resolves the effective `ConfigId`, creates a short-lived `RfcSession`, and delegates the call.
- `RfcOptions`: validates and converts configured connection strings.
- `RfcConfigProvider`: exposes configured RFC destinations and applies cleanup settings.
- `RfcDestinationRegistry`: registers and resolves named SAP destinations.
- `RfcConnectionManager`: owns SAP NCo destination configuration, destination cache, and idle cleanup.
- `RfcSession`: invokes typed RFC calls and emits monitoring events.
- `RfcTypeConverter`: maps `[Column]` properties to SAP RFC scalar, structure, and table values.
- `RfcRequestMetadata`: centralizes request model validation and RFC function name lookup.

## Usage Summary

Register services:

```csharp
// Option 1: pass configuration explicitly
builder.Services.AddRfcClient(builder.Configuration);
// Option 2: auto-resolve IConfiguration from the DI container
builder.Services.AddRfcClient();
```

Configure named connections:

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

Invoke an RFC:

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

Switch the SAP configuration in the current scope:

```csharp
client.ConfigId = "Sap.JSY";
var response = client.Invoke<SupplyDemandResponse>(request);
```

The request type must use `[Table("RFC_FUNCTION_NAME")]`; mapped request and response properties must use `[Column("SAP_FIELD_NAME")]`.

## Refactoring Applied

Following changes were made in this maintenance pass:

- Renamed `ScopedRfcClient` class to `RfcClient`; the source file was renamed accordingly.
- Added a parameterless `AddRfcClient()` overload that auto-resolves `IConfiguration` from the DI container.
- Added Chinese XML documentation comments to all public types, methods, and properties across the project; enabled `GenerateDocumentationFile` to emit the doc XML file.
- Restructured build output: SAP NCo runtime DLLs are now copied to the output root directly; the `libs\` subdirectory under the output is no longer created.

This maintenance pass changed the scoped configuration API:

- Added `ConfigId` to `IRfcClient`.
- Removed the previous DI-facing scoped config accessor and session factory services.
- Moved effective `ConfigId` resolution and session creation into `RfcClient`.
- Kept `RfcSession` as the internal per-call execution object.

Existing maintenance work in the project also includes:

- Extracted shared request metadata and validation into `RfcRequestMetadata`
- Reused centralized request metadata from `RfcSession`
- Fixed `AddRfcClient(RfcOptions)` so cleanup settings are copied
- Added duplicate and missing `ConfigId` validation in `RfcOptions`
- Added validation for invalid cleanup intervals in `RfcConnectionManager`
- Simplified idle destination cleanup and destination cache creation
- Removed duplicate RFC value conversion switches in `RfcTypeConverter`
- Updated README target framework and package runtime path documentation

## Build And Verification

Build:

```bash
dotnet build .\RfcClient.sln
```

Package:

```bash
dotnet pack .\RfcClient.csproj -c Release
```

## Maintenance Notes

- Keep the target framework, Microsoft.Extensions package versions, and README framework text in sync.
- Treat connection strings and passwords as secrets; do not log full connection strings.
- Prefer adding tests around `RfcOptions`, `RfcClient`, and `RfcTypeConverter` before expanding supported mapping scenarios.
- If the library must support older applications, evaluate retargeting or multi-targeting instead of only changing README text.
- SAP NCo files are platform-specific. Keep package/runtime layout explicit and verify on the deployment OS.
