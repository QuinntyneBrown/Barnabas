using Barnabas.Api.Controllers;
using Barnabas.Api.Errors;
using Barnabas.Api.Filters;
using Barnabas.Api.Middleware;
using Barnabas.Api.Observability;
using Barnabas.Api.Security;
using Barnabas.Api.Tenancy;
using Barnabas.Application.Common.Abuse;
using Barnabas.Application.Common.Tenancy;
using Barnabas.Application.DependencyInjection;
using Barnabas.Domain.Photos;
using Barnabas.Infrastructure.DependencyInjection;
using Barnabas.Infrastructure.Persistence;
using Barnabas.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.Unicode;

var builder = WebApplication.CreateBuilder(args);

// The largest any endpoint accepts. Kestrel cannot vary its limit per route, so it holds the
// ceiling and RequestBodyLimitMiddleware holds the per-endpoint floor of 1 MB.
builder.WebHost.ConfigureKestrel(kestrel => kestrel.Limits.MaxRequestBodySize = PhotoBounds.MaxUploadBytes);

builder.Services
    .AddControllers(options => options.Filters.Add<ForbiddenFieldInspector>())
    .AddJsonOptions(options =>
    {
        // The four kinds and the request states travel as their names. The web client's own
        // models are string unions, and a number on the wire would make the two disagree about
        // a domain the whole product is written in the vocabulary of.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

        // Declared rather than inherited. L2-097 AC1 requires member-supplied text to come back
        // encoded, and what the framework's default encoder escapes has changed between releases
        // - a requirement that holds only because of a default is one upgrade away from being
        // false.
        //
        // This escapes the HTML-sensitive characters - < > & ' " + - as escape sequences
        // whatever else it is given, so a description containing a script tag cannot close one
        // in any document this body is embedded in. UnicodeRanges.All is what stops it also
        // escaping every accented letter in a member's name, which would be noise rather than
        // safety.
        options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
    })
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

builder.Services.Configure<SecurityHeaderOptions>(
    builder.Configuration.GetSection(SecurityHeaderOptions.SectionName));

builder.Services.AddSingleton(TimeProvider.System);

// Observability. The meter is the platform's own, so a deployment that wants OpenTelemetry adds
// the exporter and points it at the same name; the snapshot exists so L2-118's "when metrics are
// scraped" is answerable without the product having chosen a monitoring vendor.
builder.Services.AddMetrics();
builder.Services.AddSingleton<BarnabasMetrics>();
builder.Services.AddSingleton<MetricsSnapshot>();

// One check per dependency, named. There is one dependency.
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICongregationContext, CongregationContext>();
builder.Services.AddScoped<ICallerSource, CallerSource>();

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

var security = app.Services.GetRequiredService<IOptions<SecurityHeaderOptions>>().Value;

// Outermost of all, so every entry any of the rest produces is inside the scope it opens.
app.UseMiddleware<CorrelationMiddleware>();

// Explicit, and first of the rest, for two reasons. The body-limit middleware reads the limit
// the endpoint declared, and the metrics middleware records the route pattern rather than the
// path - neither exists until routing has run.
app.UseRouting();

// After routing, so it can read the route on the way in; outside the exception handler, so the
// status it records on the way out is the one the caller was actually given. The handler clears
// the endpoint when it handles something, which is why the route is captured before `next` and
// not after.
app.UseMiddleware<MetricsMiddleware>();

app.UseExceptionHandler();

// Outside the handler, so an error response carries them too.
app.UseMiddleware<SecurityHeadersMiddleware>();

if (security.RequireHttps)
{
    app.UseHttpsRedirection();
}

app.UseMiddleware<RequestBodyLimitMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Anonymous, because a monitor cannot sign in - which is why the writer is careful about what
// it says. L2-116.
app.MapHealthChecks("/health", HealthReport.Options()).AllowAnonymous();

// Anonymous for the same reason a health endpoint is: a scraper holds no session. It carries
// counts and durations by endpoint and nothing about any member.
app.MapGet("/metrics", (MetricsSnapshot snapshot) =>
        Results.Text(snapshot.Render(), "text/plain; version=0.0.4"))
    .AllowAnonymous();

// Started here rather than lazily, so the listener is attached before the first request rather
// than before the first scrape - measurements taken in between would otherwise be lost.
app.Services.GetRequiredService<MetricsSnapshot>();

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<DatabaseInitialiser>().InitialiseAsync();
}

await app.RunAsync();

// The integration tests drive the API through WebApplicationFactory<Program>,
// which needs the implicitly generated Program class to be reachable.
public partial class Program;
