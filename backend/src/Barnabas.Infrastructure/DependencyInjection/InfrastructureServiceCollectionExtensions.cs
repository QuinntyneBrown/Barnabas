using Barnabas.Application.Common.Authorisation;
using Barnabas.Application.Common.Email;
using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Common.Security;
using Barnabas.Infrastructure.Email;
using Barnabas.Infrastructure.Persistence;
using Barnabas.Infrastructure.Persistence.Seeding;
using Barnabas.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Barnabas.Infrastructure.DependencyInjection;

/// <summary>Registers persistence, security, and delivery.</summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddBarnabasInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<SignInLinkOptions>(configuration.GetSection(SignInLinkOptions.SectionName));

        var database = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();

        var sqlite = string.Equals(database.Provider, DatabaseProviders.Sqlite, StringComparison.OrdinalIgnoreCase);

        var connectionString = string.IsNullOrWhiteSpace(database.ConnectionString)
            ? DefaultConnectionString(sqlite)
            : database.ConnectionString;

        services.AddDbContext<BarnabasDbContext>(options =>
        {
            if (sqlite)
            {
                options.UseSqlite(connectionString);

                return;
            }

            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IBarnabasDbContext>(provider => provider.GetRequiredService<BarnabasDbContext>());
        services.AddScoped<IAuthenticationStore, AuthenticationStore>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IOwnerLookup<>), typeof(OwnerLookup<>));

        services.AddScoped<CongregationSeeder>();
        services.AddScoped<DatabaseInitialiser>();

        services.AddSingleton<IAccessTokenIssuer, JwtIssuer>();
        services.AddSingleton<ISecretService, SecretService>();

        // One instance behind two interfaces: what the handler dispatched is what the
        // acceptance suite reads back.
        services.AddSingleton<InMemoryEmailSender>();
        services.AddSingleton<IEmailSender>(provider => provider.GetRequiredService<InMemoryEmailSender>());
        services.AddSingleton<IEmailOutbox>(provider => provider.GetRequiredService<InMemoryEmailSender>());

        return services;
    }

    private static string DefaultConnectionString(bool sqlite) => sqlite
        ? "Data Source=barnabas.db"
        : "Host=localhost;Database=barnabas;Username=barnabas;Password=barnabas";
}
