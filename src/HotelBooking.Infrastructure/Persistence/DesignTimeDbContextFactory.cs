using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HotelBooking.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var envPath = FindEnvFile();
        if (envPath is not null)
        {
            DotNetEnv.Env.Load(envPath);
        }

        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION")
                               ?? throw new InvalidOperationException(
                                   "DB_CONNECTION missing - add it to the .env file at the solution root");

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }

    private static string? FindEnvFile()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, ".env");
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }
        return null;
    }
}
