using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StudyOrganizer.Api.Authentication;
using StudyOrganizer.Api.Modules;
using StudyOrganizer.Application.Modules;

namespace StudyOrganizer.Api.Tests.Modules;

public sealed class ModuleEndpointsTests
{
    private static readonly string SigningKey =
        new('a', 64);

    [Fact]
    public async Task GetModules_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();
        using var client = CreateClient(factory);

        var response = await client.GetAsync(
            "/api/modules/");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateModule_WithValidData_ReturnsCreated()
    {
        var ownerId = Guid.NewGuid();

        var module = new ModuleResult(
            Guid.NewGuid(),
            "Sichere Systeme",
            "SIS",
            "Vorlesung im 4. Semester",
            "#1E90FF",
            DateTimeOffset.UtcNow);

        var handler = new StubModuleHandler(module);

        using var factory = CreateFactory(handler);
        using var client = CreateClient(factory);

        AddAuthorization(client, ownerId);

        var response = await client.PostAsJsonAsync(
            "/api/modules/",
            new
            {
                name = "Sichere Systeme",
                code = "SIS",
                description =
                    "Vorlesung im 4. Semester",
                color = "#1E90FF"
            });

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var body =
            await response.Content.ReadFromJsonAsync<
                ModuleResponse>();

        Assert.NotNull(body);
        Assert.Equal(module.Id, body.Id);
        Assert.Equal(module.Name, body.Name);
        Assert.Equal(ownerId, handler.ReceivedOwnerId);
    }

    [Fact]
    public async Task CreateModule_WithInvalidColor_ReturnsBadRequest()
    {
        var module = new ModuleResult(
            Guid.NewGuid(),
            "Sichere Systeme",
            "SIS",
            null,
            "#1E90FF",
            DateTimeOffset.UtcNow);

        var handler = new StubModuleHandler(module);

        using var factory = CreateFactory(handler);
        using var client = CreateClient(factory);

        AddAuthorization(client, Guid.NewGuid());

        var response = await client.PostAsJsonAsync(
            "/api/modules/",
            new
            {
                name = "Sichere Systeme",
                code = "SIS",
                color = "blau"
            });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Null(handler.ReceivedOwnerId);
    }
    [Fact]
    public async Task GetModules_ReturnsOnlyAuthenticatedUsersModules()
    {
        var firstOwnerId = Guid.NewGuid();
        var secondOwnerId = Guid.NewGuid();

        var firstModule = new ModuleResult(
            Guid.NewGuid(),
            "Sichere Systeme",
            "SIS",
            null,
            "#1E90FF",
            DateTimeOffset.UtcNow);

        var secondModule = new ModuleResult(
            Guid.NewGuid(),
            "Datenbanken",
            "DB",
            null,
            "#FF8800",
            DateTimeOffset.UtcNow);

        var modulesByOwner =
            new Dictionary<
                Guid,
                IReadOnlyList<ModuleResult>>
            {
                [firstOwnerId] = new[] { firstModule },
                [secondOwnerId] = new[] { secondModule }
            };

        var handler = new StubModuleHandler(
            firstModule,
            modulesByOwner);

        using var factory = CreateFactory(handler);
        using var client = CreateClient(factory);

        AddAuthorization(client, firstOwnerId);

        var firstResponse = await client.GetAsync(
            "/api/modules/");

        Assert.Equal(
            HttpStatusCode.OK,
            firstResponse.StatusCode);

        var firstBody =
            await firstResponse.Content.ReadFromJsonAsync<
                List<ModuleResponse>>();

        Assert.NotNull(firstBody);
        Assert.Single(firstBody);
        Assert.Equal(firstModule.Id, firstBody[0].Id);

        AddAuthorization(client, secondOwnerId);

        var secondResponse = await client.GetAsync(
            "/api/modules/");

        var secondBody =
            await secondResponse.Content.ReadFromJsonAsync<
                List<ModuleResponse>>();

        Assert.NotNull(secondBody);
        Assert.Single(secondBody);
        Assert.Equal(secondModule.Id, secondBody[0].Id);

        Assert.Equal(
            new[] { firstOwnerId, secondOwnerId },
            handler.ReceivedGetOwnerIds);
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
                    Audience =
                        "StudyOrganizer.Clients",
                    SigningKey = SigningKey,
                    ExpiresInMinutes = 15
                },
                TimeProvider.System);

        var token = tokenService.Create(
            userId,
            "owner@example.com");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token.Value);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        IModuleHandler? handler = null)
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
                        services.RemoveAll<IModuleHandler>();
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
                BaseAddress =
                    new Uri("https://localhost"),
                AllowAutoRedirect = false
            });
    }

    private sealed class StubModuleHandler(
        ModuleResult createResult,
        IReadOnlyDictionary<
            Guid,
            IReadOnlyList<ModuleResult>>? modulesByOwner = null)
        : IModuleHandler
    {
        public Guid? ReceivedOwnerId { get; private set; }
        public List<Guid> ReceivedGetOwnerIds { get; } = [];

        public Task<ModuleResult> CreateAsync(
            Guid ownerId,
            string name,
            string? code,
            string? description,
            string? color,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReceivedOwnerId = ownerId;

            return Task.FromResult(createResult);
        }

        public Task<IReadOnlyList<ModuleResult>>
            GetByOwnerAsync(
                Guid ownerId,
                CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ReceivedGetOwnerIds.Add(ownerId);

            if (modulesByOwner is not null
                && modulesByOwner.TryGetValue(
                    ownerId,
                    out var modules))
            {
                return Task.FromResult(modules);
            }

            return Task.FromResult<
                IReadOnlyList<ModuleResult>>(
                    Array.Empty<ModuleResult>());
        }
    }
}

