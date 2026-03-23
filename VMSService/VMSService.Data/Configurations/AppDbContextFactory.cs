using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VMSService.Data.Configurations
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<VMSServiceDbContext>
    {
        public VMSServiceDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<VMSServiceDbContext>();

            optionsBuilder.UseSqlServer(
                "Server=(localdb)\\mssqllocaldb;Database=VMSCoreDB;Trusted_Connection=True;MultipleActiveResultSets=true");

            return new VMSServiceDbContext(optionsBuilder.Options);
        }
    }
}
