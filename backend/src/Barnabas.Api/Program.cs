using Barnabas.Api.Controllers;
using Barnabas.Api.Errors;
using Barnabas.Api.Filters;
using Barnabas.Api.Middleware;
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

app.UseExceptionHandler();

// Outermost, so an exception handler's response carries them too.
app.UseMiddleware<SecurityHeadersMiddleware>();

if (security.RequireHttps)
{
    app.UseHttpsRedirection();
}

// Explicit, and ahead of the body limit, because that middleware reads the endpoint's declared
// limit and there is no endpoint until routing has run.
app.UseRouting();

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
