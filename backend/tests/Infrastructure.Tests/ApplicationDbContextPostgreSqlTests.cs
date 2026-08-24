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
    public async Task SaveChangesAsync_PersistsStudyModule_InPostgreSql()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var studyModule = new StudyModule(
            Guid.NewGuid(),
            "PostgreSQL smoke test");

        // Act
        context.Modules.Add(studyModule);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Assert
        var savedStudyModule = await context.Modules
            .SingleAsync(candidate => candidate.Id == studyModule.Id);

        Assert.Equal(studyModule.Name, savedStudyModule.Name);
    }

    [Fact]
    public async Task ResetDatabaseAsync_RemovesPersistedStudyModules()
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
