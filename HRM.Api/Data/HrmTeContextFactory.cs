using HRM.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HRM.Data;

public sealed class HrmTeContextFactory : IDesignTimeDbContextFactory<HrmTeContext>
{
    public HrmTeContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var settings = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true);
        var development = environment.Equals("Development", StringComparison.OrdinalIgnoreCase);
        if (development) settings.AddUserSecrets(typeof(HrmTeContextFactory).Assembly, optional: true);
        var configuration = settings.AddEnvironmentVariables().Build();
        var connection = HRM.Helpers.DatabaseConnectionPolicy.Validate(configuration.GetConnectionString("DefaultConnection"),
            development, "ConnectionStrings:DefaultConnection");
        return new HrmTeContext(new DbContextOptionsBuilder<HrmTeContext>().UseSqlServer(connection).Options);
    }
}
