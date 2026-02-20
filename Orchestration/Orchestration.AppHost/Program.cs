var builder = DistributedApplication.CreateBuilder(args);

var identity = builder.AddProject<Projects.IdentityService_Web>("identityservice");
var gateway = builder.AddProject<Projects.GatewayService>("gatewayservice");

var product = builder.AddProject<Projects.ProductService_Web>("productservice");

builder.Build().Run();
