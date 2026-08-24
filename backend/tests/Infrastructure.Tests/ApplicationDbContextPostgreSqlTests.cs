using Microsoft.EntityFrameworkCore;
using StudyOrganizer.Domain.Modules;

namespace StudyOrganizer.Infrastructure.Tests;

public sealed class ApplicationDbContextPostgreSqlTests
    : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;

    public ApplicationDbContextPostgreSqlTests(
        PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        return _fixture.ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task SaveChangesAsync_PersistsModule_InPostgreSql()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var module = new StudyModule(
            Guid.NewGuid(),
            "PostgreSQL smoke test");

        // Act
        context.Modules.Add(module);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Assert
        var savedModule = await context.Modules
            .SingleAsync(candidate => candidate.Id == module.Id);

        Assert.Equal(module.Name, savedModule.Name);
    }

    [Fact]
    public async Task ResetDatabaseAsync_RemovesPersistedModules()
    {
        // Arrange
        await using (var context = _fixture.CreateDbContext())
        {
            context.Modules.Add(
                new StudyModule(
                    Guid.NewGuid(),
                    "Must be removed"));

            await context.SaveChangesAsync();
        }

        // Act
        await _fixture.ResetDatabaseAsync();

        // Assert
        await using var cleanContext = _fixture.CreateDbContext();
        Assert.Empty(await cleanContext.Modules.ToListAsync());
    }
}
