using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Engine>("engine")
    .WithHttpEndpoint(5001, name: "engine-http", isProxied: false);


builder.AddProject<StreamHub>("streamhub")
    .WithHttpEndpoint(9000, name: "streamhub-http");

builder.Build().Run();