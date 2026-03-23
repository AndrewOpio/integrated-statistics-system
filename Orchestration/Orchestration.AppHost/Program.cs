var builder = DistributedApplication.CreateBuilder(args);

var identity = builder.AddProject<Projects.IdentityService_Web>("identityservice");
var gateway = builder.AddProject<Projects.GatewayService>("gatewayservice");
var stock = builder.AddProject<Projects.StockService_Web>("stockservice");
var vms = builder.AddProject<Projects.VMSService_Web>("vmsservice");


builder.Build().Run();
