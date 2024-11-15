
var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Engine>("engine")
    .WithHttpEndpoint(port: 5001, name: "engine-http", isProxied: false);


builder.AddProject<Projects.StreamHub>("streamhub")
    .WithHttpEndpoint(port: 9000, name: "streamhub-http");

builder.Build().Run();