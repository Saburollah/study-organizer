using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StudyOrganizer.Api.Authentication;
using StudyOrganizer.Api.Profiles;
using StudyOrganizer.Application.Profiles;
using StudyOrganizer.Domain.Users;

namespace StudyOrganizer.Api.Tests.Profiles;

public sealed class ProfileEndpointsTests
{
    private static readonly string SigningKey =
        new('a', 64);

    [Fact]
    public async Task GetProfile_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();
        using var client = CreateClient(factory);

        var response = await client.GetAsync("/api/profile/");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_WithToken_ReturnsCurrentUsersProfile()
    {
        var userId = Guid.NewGuid();

        var profile = new ProfileResult(
            userId,
            "test@example.com",
            "Saburo",
            "Safari",
            new DateOnly(2000, 1, 15),
            ProfileGender.Male);

        var handler = new StubProfileHandler(profile);

        using var factory = CreateFactory(handler);
        using var client = CreateClient(factory);

        AddAuthorization(client, userId);

        var response = await client.GetAsync("/api/profile/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body =
            await response.Content.ReadFromJsonAsync<ProfileResponse>();

        Assert.NotNull(body);
        Assert.Equal(userId, body.UserId);
        Assert.Equal("test@example.com", body.Email);
        Assert.Equal("Saburo", body.FirstName);
        Assert.Equal("Safari", body.LastName);
        Assert.Equal("Male", body.Gender);
        Assert.Equal(userId, handler.ReceivedGetUserId);
    }

    [Fact]
    public async Task UpdateProfile_WithValidData_ReturnsUpdatedProfile()
    {
        var userId = Guid.NewGuid();

        var updatedProfile = new ProfileResult(
            userId,
            "test@example.com",
            "Saburo",
            "Safari",
            new DateOnly(2000, 1, 15),
            ProfileGender.Male);

        var handler = new StubProfileHandler(
            getResult: null,
            updateResult: updatedProfile);

        using var factory = CreateFactory(handler);
        using var client = CreateClient(factory);

        AddAuthorization(client, userId);

        var response = await client.PutAsJsonAsync(
            "/api/profile/",
            new
            {
                firstName = "Saburo",
                lastName = "Safari",
                dateOfBirth = "2000-01-15",
                gender = "Male",
                email = "changed@example.com"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body =
            await response.Content.ReadFromJsonAsync<ProfileResponse>();

        Assert.NotNull(body);
        Assert.Equal("test@example.com", body.Email);
        Assert.Equal("Saburo", body.FirstName);
        Assert.Equal("Safari", body.LastName);
        Assert.Equal("Male", body.Gender);

        Assert.Equal(userId, handler.ReceivedUpdateUserId);
        Assert.Equal("Saburo", handler.ReceivedFirstName);
        Assert.Equal("Safari", handler.ReceivedLastName);
    }

    [Fact]
    public async Task UpdateProfile_WithDiverseGender_ReturnsBadRequest()
    {
        var userId = Guid.NewGuid();
        var handler = new StubProfileHandler(getResult: null);

        using var factory = CreateFactory(handler);
        using var client = CreateClient(factory);

        AddAuthorization(client, userId);

        var response = await client.PutAsJsonAsync(
            "/api/profile/",
            new
            {
                firstName = "Saburo",
                lastName = "Safari",
                gender = "Diverse"
            });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Null(handler.ReceivedUpdateUserId);
    }

    [Fact]
    public async Task UpdateProfile_WithFutureBirthDate_ReturnsBadRequest()
    {
        var userId = Guid.NewGuid();
        var handler = new StubProfileHandler(getResult: null);

        using var factory = CreateFactory(handler);
        using var client = CreateClient(factory);

        AddAuthorization(client, userId);

        var futureDate = DateOnly
            .FromDateTime(DateTime.UtcNow)
            .AddDays(1);

        var response = await client.PutAsJsonAsync(
            "/api/profile/",
            new
            {
                firstName = "Saburo",
                dateOfBirth = futureDate.ToString("yyyy-MM-dd"),
                gender = "Male"
            });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Null(handler.ReceivedUpdateUserId);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        IProfileHandler? handler = null)
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            "Host=localhost;Database=test;"
            + "Username=test;Password=test");

        Environment.SetEnvironmentVariable(
            "Jwt__SigningKey",
            SigningKey);

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                if (handler is not null)
                {
                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<IProfileHandler>();
                        services.AddSingleton(handler);
                    });
                }
            });
    }

    private static HttpClient CreateClient(
        WebApplicationFactory<Program> factory)
    {
        return factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });
    }

    private static void AddAuthorization(
        HttpClient client,
        Guid userId)
    {
        var tokenService =
            new JwtAccessTokenService(
                new JwtOptions
                {
                    Issuer = "StudyOrganizer.Api",
                    Audience = "StudyOrganizer.Clients",
                    SigningKey = SigningKey,
                    ExpiresInMinutes = 15
                },
                TimeProvider.System);

        var token = tokenService.Create(
            userId,
            "test@example.com");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token.Value);
    }

    private sealed class StubProfileHandler(
        ProfileResult? getResult,
        ProfileResult? updateResult = null)
        : IProfileHandler
    {
        public Guid? ReceivedGetUserId { get; private set; }

        public Guid? ReceivedUpdateUserId { get; private set; }

        public string? ReceivedFirstName { get; private set; }

        public string? ReceivedLastName { get; private set; }

        public Task<ProfileResult?> GetAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ReceivedGetUserId = userId;

            return Task.FromResult(getResult);
        }

        public Task<ProfileResult?> UpdateAsync(
            Guid userId,
            string? firstName,
            string? lastName,
            DateOnly? dateOfBirth,
            ProfileGender? gender,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ReceivedUpdateUserId = userId;
            ReceivedFirstName = firstName;
            ReceivedLastName = lastName;

            return Task.FromResult(updateResult);
        }
    }
}
