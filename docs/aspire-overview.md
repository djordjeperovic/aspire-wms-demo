# .NET Aspire Overview

This document explains how .NET Aspire is used in the Aspire WMS project.

## What is .NET Aspire?

.NET Aspire is an opinionated, cloud-ready stack for building observable, production-ready, distributed applications. It provides:

- **Orchestration**: Coordinate multiple services and resources
- **Components**: Pre-configured integrations for databases, caches, messaging
- **Tooling**: Enhanced development experience with the Aspire Dashboard

## How Aspire is Used in This Project

### AppHost (Orchestration)

The `AspireWms.AppHost` project orchestrates all services and infrastructure:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure
var postgres = builder.AddPostgres("postgres").WithDataVolume();
var inventoryDb = postgres.AddDatabase("inventorydb");
var redis = builder.AddRedis("redis").WithDataVolume();

// Services reference infrastructure
var inventoryApi = builder.AddProject<Projects.AspireWms_Inventory_Api>("inventory-api")
    .WithReference(inventoryDb)
    .WithReference(redis);

builder.Build().Run();
```

### Service Defaults

The `AspireWms.ServiceDefaults` project provides shared configuration:

- **OpenTelemetry**: Automatic tracing, metrics, and logging
- **Health Checks**: `/health` and `/alive` endpoints
- **Service Discovery**: Automatic service-to-service communication
- **Resilience**: Retry policies and circuit breakers

### Benefits in This Project

| Benefit | How It Helps |
|---------|--------------|
| **Local Development** | Run entire stack with `dotnet run` |
| **Observability** | Built-in tracing across all services |
| **Configuration** | Connection strings auto-injected |
| **Containers** | PostgreSQL/Redis run in Docker automatically |

## Aspire Dashboard

Access at `https://localhost:17225` to view:

- **Resources**: All services and their status
- **Console**: Live logs from all services
- **Traces**: Distributed traces across services
- **Metrics**: Performance metrics

## Learn More

- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Aspire Samples](https://github.com/dotnet/aspire-samples)
