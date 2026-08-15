using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudyOrganizer.Api.Users;
using StudyOrganizer.Application.Users;
using StudyOrganizer.Infrastructure.Identity;
using StudyOrganizer.Infrastructure.Persistence;
using StudyOrganizer.Infrastructure.Users;
using StudyOrganizer.Api.Authentication;
using StudyOrganizer.Application.Authentication;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using StudyOrganizer.Application.Modules;
using StudyOrganizer.Infrastructure.Modules;
using StudyOrganizer.Api.Modules;
using Microsoft.OpenApi.Models;
using StudyOrganizer.Application.Tasks;
using StudyOrganizer.Infrastructure.Tasks;
using StudyOrganizer.Api.Tasks;
using StudyOrganizer.Application.Profiles;
using StudyOrganizer.Infrastructure.Profiles;
using StudyOrganizer.Api.Profiles;

var builder = WebApplication.CreateBuilder(args);

const string FrontendCorsPolicy = "FrontendCors";

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        "JWT configuration was not found.");

if (string.IsNullOrWhiteSpace(jwtOptions.Issuer)
    || string.IsNullOrWhiteSpace(jwtOptions.Audience)
    || string.IsNullOrWhiteSpace(jwtOptions.SigningKey)
    || jwtOptions.SigningKey.Length < 32
    || jwtOptions.ExpiresInMinutes <= 0)
{
    throw new InvalidOperationException(
        "JWT configuration is incomplete or invalid.");
}

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 15;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredUniqueChars = 1;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager();

builder.Services.AddSingleton(jwtOptions);

builder.Services.AddSingleton<TimeProvider>(
    TimeProvider.System);

builder.Services.AddSingleton<
    IAccessTokenService,
    JwtAccessTokenService>();

builder.Services.AddScoped<
    IUserHandler,
    UserHandler>();

builder.Services.AddScoped<
    IProfileHandler,
    ProfileHandler>();

builder.Services.AddScoped<
    IModuleHandler,
    ModuleHandler>();

builder.Services.AddScoped<
    IStudyTaskHandler,
    StudyTaskHandler>();

builder.Services.AddCors(options =>
{
    var allowedOrigins =
        builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>()
        ?? [];

    options.AddPolicy(
        FrontendCorsPolicy,
        policy =>
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,

                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtOptions.SigningKey)),

                ValidateLifetime = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,

                ClockSkew = TimeSpan.FromSeconds(30)
            };
    });

// Add services to the container.
builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description =
                "Enter the JWT access token "
                + "without the Bearer prefix."
        });

    options.OperationFilter<
        AuthorizationOperationFilter>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapUserEndpoints();
app.MapProfileEndpoints();
app.MapModuleEndpoints();
app.MapStudyTaskEndpoints();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
