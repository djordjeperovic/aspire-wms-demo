using Aspire.Hosting.Yarp.Transforms;

var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure Resources
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

// Single shared database for all modules (each module uses its own schema)
var wmsDb = postgres.AddDatabase("wmsdb");

var redis = builder.AddRedis("redis")
    .WithDataVolume()
    .WithRedisCommander();

// API Service (Modular Monolith)
var api = builder.AddProject<Projects.AspireWms_Api>("api")
    .WithReference(wmsDb)
    .WithReference(redis)
    .WaitFor(wmsDb)
    .WaitFor(redis);

// YARP API Gateway - single entry point for all API requests
var gateway = builder.AddYarp("gateway")
    .WithHostPort(5000)
    .WithConfiguration(yarp =>
    {
        // Route module requests with path transforms
        // Gateway: /api/inventory/* -> API: /inventory/*
        yarp.AddRoute("/api/inventory/{**catch-all}", api)
            .WithTransformPathRemovePrefix("/api");

        // Gateway: /api/inbound/* -> API: /inbound/*
        yarp.AddRoute("/api/inbound/{**catch-all}", api)
            .WithTransformPathRemovePrefix("/api");

        // Gateway: /api/outbound/* -> API: /outbound/*
        yarp.AddRoute("/api/outbound/{**catch-all}", api)
            .WithTransformPathRemovePrefix("/api");

        // Catch-all for /api root
        yarp.AddRoute("/api", api);
    })
    .WaitFor(api);

builder.Build().Run();
