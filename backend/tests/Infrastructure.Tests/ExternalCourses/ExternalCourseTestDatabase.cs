using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StudyOrganizer.Infrastructure.Identity;
using StudyOrganizer.Infrastructure.Persistence;

namespace StudyOrganizer.Infrastructure.Tests.ExternalCourses;

public sealed class ExternalCourseTestDatabase : IAsyncDisposable
{
    private ExternalCourseTestDatabase(
        SqliteConnection connection,
        ApplicationDbContext context,
        TestTimeProvider timeProvider)
    {
        Connection = connection;
        Context = context;
        TimeProvider = timeProvider;
    }

    public SqliteConnection Connection { get; }

    public ApplicationDbContext Context { get; }

    public DateTimeOffset Now { get; } =
        new(2026, 8, 28, 8, 0, 0, TimeSpan.Zero);

    public TimeProvider TimeProvider { get; }

    public static async Task<ExternalCourseTestDatabase> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new ApplicationDbContext(options);
            var timeProvider = new TestTimeProvider(
                new DateTimeOffset(2026, 8, 28, 8, 0, 0, TimeSpan.Zero));
            var database = new ExternalCourseTestDatabase(
                connection,
                context,
                timeProvider);

            await context.Database.EnsureCreatedAsync();

            return database;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    public async Task<Guid> CreateUserAsync(string email)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();
        var user = new ApplicationUser
        {
            Email = email,
            NormalizedEmail = normalizedEmail,
            UserName = email,
            NormalizedUserName = normalizedEmail
        };

        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        return user.Id;
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await Connection.DisposeAsync();
    }
}
