using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StudyOrganizer.Api.Authentication;
using StudyOrganizer.Api.Users;
using StudyOrganizer.Application.Users;

namespace StudyOrganizer.Api.Tests.Users;

public sealed class UserEndpointsTests
{
    private static readonly string SigningKey =
        new('a', 64);

    [Fact]
    public async Task Register_WithValidData_ReturnsCreated()
    {
        var userId = Guid.NewGuid();
        var handler = new StubUserHandler(
            new UserResult(
                true,
                userId,
                Array.Empty<string>()));

        using var factory = CreateFactory(handler);
        using var client = CreateClient(factory);

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email = "new-user@example.com",
                password = "Registration-Test-2026"
            });

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var body =
            await response.Content.ReadFromJsonAsync<
                RegisterUserResponse>();

        Assert.NotNull(body);
        Assert.Equal(userId, body.UserId);
        Assert.Equal("new-user@example.com", body.Email);
        Assert.True(handler.WasCalled);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        var handler = new StubUserHandler(
            new UserResult(
                true,
                Guid.NewGuid(),
                Array.Empty<string>()));

        using var factory = CreateFactory(handler);
        using var client = CreateClient(factory);

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email = "invalid-email",
                password = "Registration-Test-2026"
            });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.False(handler.WasCalled);
    }

    [Fact]
    public async Task Register_WhenIdentityRejectsUser_ReturnsBadRequest()
    {
        var handler = new StubUserHandler(
            new UserResult(
                false,
                null,
                new[] { "Registration failed." }));

        using var factory = CreateFactory(handler);
        using var client = CreateClient(factory);

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email = "existing-user@example.com",
                password = "Registration-Test-2026"
            });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.True(handler.WasCalled);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkAndToken()
    {
        var userId = Guid.NewGuid();
        const string email = "existing-user@example.com";

        var handler = new StubUserHandler(
            new UserResult(
                false,
                null,
                Array.Empty<string>()),
            new UserLoginResult(
                true,
                userId,
                email));

        using var factory = CreateFactory(handler);
        using var client = CreateClient(factory);

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email,
                password = "Registration-Test-2026"
            });

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content.ReadFromJsonAsync<
                LoginUserResponse>();

        Assert.NotNull(body);
        Assert.False(
            string.IsNullOrWhiteSpace(body.AccessToken));
        Assert.True(
            body.ExpiresAtUtc > DateTimeOffset.UtcNow);
        Assert.True(handler.WasCalled);

        var token =
            new JwtSecurityTokenHandler()
                .ReadJwtToken(body.AccessToken);

        Assert.Equal(
            userId.ToString(),
            token.Subject);

        Assert.Equal(
            email,
            token.Claims.Single(
                claim =>
                    claim.Type
                    == JwtRegisteredClaimNames.Email).Value);

        Assert.Equal(
            "StudyOrganizer.Api",
            token.Issuer);

        Assert.Contains(
            "StudyOrganizer.Clients",
            token.Audiences);
    }

    [Fact]
    public async Task Login_WithInvalidEmail_ReturnsBadRequest()
{
    var handler = new StubUserHandler(
        new UserResult(
            false,
            null,
            Array.Empty<string>()),
        new UserLoginResult(
            true,
            Guid.NewGuid(),
            "existing-user@example.com"));

    using var factory = CreateFactory(handler);
    using var client = CreateClient(factory);

    var response = await client.PostAsJsonAsync(
        "/api/auth/login",
        new
        {
            email = "invalid-email",
            password = "Registration-Test-2026"
        });

    Assert.Equal(
        HttpStatusCode.BadRequest,
        response.StatusCode);

    Assert.False(handler.WasCalled);
}

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var handler = new StubUserHandler(
            new UserResult(
                false,
                null,
                Array.Empty<string>()),
            new UserLoginResult(
                false,
                null,
                null));

        using var factory = CreateFactory(handler);
        using var client = CreateClient(factory);

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email = "existing-user@example.com",
                password = "Wrong-Password-2026"
            });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        var body =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            "Invalid email or password.",
            body);

        Assert.DoesNotContain(
            "existing-user@example.com",
            body);

        Assert.True(handler.WasCalled);
    }

    [Fact]
    public async Task ChangePassword_WithoutToken_ReturnsUnauthorized()
    {
        var handler = new StubUserHandler(
            new UserResult(
                false,
                null,
                Array.Empty<string>()));

        using var factory = CreateFactory(handler);
        using var client = CreateClient(factory);

        var response = await client.PutAsJsonAsync(
            "/api/auth/password",
            new
            {
                currentPassword = "Current-Password-2026!",
                newPassword = "New-Secure-Password-2026!"
            });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        Assert.False(handler.WasCalled);
    }

    [Fact]
    public async Task ChangePassword_WithValidRequest_ReturnsNoContent()
    {
        var userId = Guid.NewGuid();

        var handler = new StubUserHandler(
            new UserResult(
                false,
                null,
                Array.Empty<string>()),
            changePasswordResult:
                new ChangePasswordResult(
                    true,
                    Array.Empty<string>()));

        using var factory = CreateFactory(handler);
        using var client = CreateClient(factory);

        AddAuthorization(client, userId);

        var response = await client.PutAsJsonAsync(
            "/api/auth/password",
            new
            {
                currentPassword = "Current-Password-2026!",
                newPassword = "New-Secure-Password-2026!"
            });

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        Assert.Equal(userId, handler.ReceivedUserId);
        Assert.Equal(
            "Current-Password-2026!",
            handler.ReceivedCurrentPassword);
        Assert.Equal(
            "New-Secure-Password-2026!",
            handler.ReceivedNewPassword);
    }

    [Fact]
    public async Task ChangePassword_WithEmptyPasswords_ReturnsBadRequest()
    {
        var userId = Guid.NewGuid();

        var handler = new StubUserHandler(
            new UserResult(
                false,
                null,
                Array.Empty<string>()));

        using var factory = CreateFactory(handler);
        using var client = CreateClient(factory);

        AddAuthorization(client, userId);

        var response = await client.PutAsJsonAsync(
            "/api/auth/password",
            new
            {
                currentPassword = string.Empty,
                newPassword = string.Empty
            });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.False(handler.WasCalled);
    }

    [Fact]
    public async Task ChangePassword_WhenRejected_ReturnsBadRequest()
    {
        var userId = Guid.NewGuid();

        var handler = new StubUserHandler(
            new UserResult(
                false,
                null,
                Array.Empty<string>()),
            changePasswordResult:
                new ChangePasswordResult(
                    false,
                    new[] { "Incorrect password." }));

        using var factory = CreateFactory(handler);
        using var client = CreateClient(factory);

        AddAuthorization(client, userId);

        var response = await client.PutAsJsonAsync(
            "/api/auth/password",
            new
            {
                currentPassword = "Wrong-Password-2026!",
                newPassword = "New-Secure-Password-2026!"
            });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("Incorrect password.", body);
        Assert.True(handler.WasCalled);
        Assert.Equal(userId, handler.ReceivedUserId);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        IUserHandler handler)
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

                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IUserHandler>();
                    services.AddSingleton(handler);
                });
            });
    }

    private static HttpClient CreateClient(
        WebApplicationFactory<Program> factory)
    {
        return factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress =
                    new Uri("https://localhost"),
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

    private sealed class StubUserHandler(
        UserResult result,
        UserLoginResult? loginResult = null,
        ChangePasswordResult? changePasswordResult = null)
        : IUserHandler
    {
        public bool WasCalled { get; private set; }

        public Guid? ReceivedUserId { get; private set; }

        public string? ReceivedCurrentPassword { get; private set; }

        public string? ReceivedNewPassword { get; private set; }

        public Task<UserResult> RegisterAsync(
            string email,
            string password,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WasCalled = true;

            return Task.FromResult(result);
        }

        public Task<UserLoginResult> LoginAsync(
            string email,
            string password,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WasCalled = true;

            return Task.FromResult(
                loginResult
                ?? new UserLoginResult(
                    false,
                    null,
                    null));
        }

        public Task<ChangePasswordResult> ChangePasswordAsync(
            Guid userId,
            string currentPassword,
            string newPassword,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WasCalled = true;
            ReceivedUserId = userId;
            ReceivedCurrentPassword = currentPassword;
            ReceivedNewPassword = newPassword;

            return Task.FromResult(
                changePasswordResult
                ?? new ChangePasswordResult(
                    false,
                    new[] { "Password change failed." }));
        }
    }
}
