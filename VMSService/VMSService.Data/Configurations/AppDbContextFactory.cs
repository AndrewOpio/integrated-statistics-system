using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace VMSService.Data.Configurations
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<VMSServiceDbContext>
    {
        public VMSServiceDbContext CreateDbContext(string[] args)
        {
            var basePath = ResolveSettingsDirectory()
                ?? throw new InvalidOperationException(
                    "Could not locate appsettings.json. Run EF commands with the startup project set to VMSService.Web.");

            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException($"Connection string 'DefaultConnection' not found in {basePath}.");

            var optionsBuilder = new DbContextOptionsBuilder<VMSServiceDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new VMSServiceDbContext(optionsBuilder.Options);
        }

        /// <summary>
        /// EF invokes this factory with the working directory set to the startup project, but that
        /// differs between the Package Manager Console and `dotnet ef`, so probe for appsettings.json.
        /// </summary>
        private static string? ResolveSettingsDirectory()
        {
            var current = new DirectoryInfo(Directory.GetCurrentDirectory());

            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "appsettings.json")))
                {
                    return current.FullName;
                }

                var webProject = Path.Combine(current.FullName, "VMSService");
                if (File.Exists(Path.Combine(webProject, "appsettings.json")))
                {
                    return webProject;
                }

                current = current.Parent;
            }

            return null;
        }
    }
}
