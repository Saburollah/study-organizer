using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using StudyOrganizer.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace StudyOrganizer.Infrastructure.Tests;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:16-alpine")
            .WithCleanUp(true)
            .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();
    }

    public ApplicationDbContext CreateDbContext(
        params IInterceptor[] interceptors)
    {
        var optionsBuilder =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(_container.GetConnectionString());

        if (interceptors.Length > 0)
        {
            optionsBuilder.AddInterceptors(interceptors);
        }

        return new ApplicationDbContext(optionsBuilder.Options);
    }

    public async Task ResetDatabaseAsync()
    {
        await using var context = CreateDbContext();

        await context.Database.ExecuteSqlRawAsync(
            """
            DROP SCHEMA public CASCADE;
            CREATE SCHEMA public;
            """);

        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync()
    {
        return _container.DisposeAsync().AsTask();
    }
}
