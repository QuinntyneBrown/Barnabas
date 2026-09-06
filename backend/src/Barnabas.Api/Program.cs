using Barnabas.Api.Controllers;
using Barnabas.Api.Errors;
using Barnabas.Api.Filters;
using Barnabas.Api.Middleware;
using Barnabas.Api.Security;
using Barnabas.Api.Tenancy;
using Barnabas.Application.Common.Tenancy;
using Barnabas.Application.DependencyInjection;
using Barnabas.Infrastructure.DependencyInjection;
using Barnabas.Infrastructure.Persistence;
using Barnabas.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// The acceptance suites choose a provider without editing configuration, so that a machine
// with no container runtime can still run them.
var providerOverride = Environment.GetEnvironmentVariable("BARNABAS_TEST_DB");

if (!string.IsNullOrWhiteSpace(providerOverride))
{
    builder.Configuration["Database:Provider"] = providerOverride;
}

builder.WebHost.ConfigureKestrel(kestrel => kestrel.Limits.MaxRequestBodySize = RequestBodyLimitMiddleware.MaxBytes);

builder.Services
    .AddControllers(options => options.Filters.Add<ForbiddenFieldInspector>())
    .ConfigureApplicationPartManager(manager =>
    {
        if (!builder.Environment.IsDevelopment())
        {
            manager.FeatureProviders.Add(new RemoveDevelopmentEndpoints());
        }
    });

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

builder.Services.Configure<RefreshCookieOptions>(
    builder.Configuration.GetSection(RefreshCookieOptions.SectionName));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICongregationContext, CongregationContext>();

builder.Services.AddBarnabasApplication();
builder.Services.AddBarnabasInfrastructure(builder.Configuration);

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Read the claims back under the names they were issued with. The default inbound map
        // rewrites sub and sid to WS-Federation URIs, which would leave the session claim
        // unreadable and every authenticated request refused.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),

            // No leeway. L2-018 says a token past its expiry is refused, and the default five
            // minutes of clock skew would make that false for five minutes.
            ClockSkew = TimeSpan.Zero,
        };

        // A signature cannot express revocation, so the session is read on every authenticated
        // request. This is what makes sign-out immediate rather than eventual.
        options.Events = new JwtBearerEvents { OnTokenValidated = SessionValidator.ValidateAsync };
    });

builder.Services
    .AddSingleton<IPostConfigureOptions<JwtBearerOptions>, JwtBearerClock>();

// Authenticated by default. An endpoint is public only by saying so, which is the safer
// direction to forget in: a new endpoint that nobody thought about is closed, not open.
builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

var app = builder.Build();

app.UseExceptionHandler();
app.UseMiddleware<RequestBodyLimitMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<DatabaseInitialiser>().InitialiseAsync();
}

await app.RunAsync();

// The integration tests drive the API through WebApplicationFactory<Program>,
// which needs the implicitly generated Program class to be reachable.
public partial class Program;
