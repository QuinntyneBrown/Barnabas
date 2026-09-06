using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Common.Tenancy;
using Barnabas.Domain.Access;
using Barnabas.Domain.Common;
using Barnabas.Domain.Congregations;
using Barnabas.Domain.Listings;
using Barnabas.Domain.Members;
using Barnabas.Domain.Messaging;
using Barnabas.Domain.Notifications;
using Barnabas.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Infrastructure.Persistence;

/// <summary>
/// The congregation boundary, placed underneath every query rather than inside each handler.
/// </summary>
/// <remarks>
/// Two mechanisms hold the boundary, and they answer different questions.
/// <list type="bullet">
/// <item>
/// The <em>set accessors</em> refuse outright when no congregation is in scope. Treating an
/// absent congregation as "no filter" would turn every anonymous endpoint into a cross-tenant
/// read, so the accessor raises <see cref="CongregationContextMissingException"/> naming the
/// entity that was asked for. That is what <c>L2-088</c> requires of an unresolved context.
/// </item>
/// <item>
/// The <em>global query filter</em> supplies the predicate once a congregation is known. It is
/// applied by reflection over the model, so an entity is covered the moment it implements
/// <see cref="ITenantOwned"/> rather than when somebody remembers to add it here.
/// </item>
/// </list>
/// A handler therefore writes <c>Listings.Where(l =&gt; l.Status == Active)</c> and receives only
/// its own congregation's rows. There is no <c>WithCongregation()</c> call to omit, which is
/// the point: the safe path is the only path.
/// </remarks>
public sealed class BarnabasDbContext : DbContext, IBarnabasDbContext
{
    internal const string CongregationIdProperty = nameof(ITenantOwned.CongregationId);

    private readonly ICongregationContext _congregation;

    public BarnabasDbContext(DbContextOptions<BarnabasDbContext> options, ICongregationContext congregation)
        : base(options)
    {
        _congregation = congregation;
    }

    /// <summary>
    /// The congregation every filtered query compares against.
    /// </summary>
    /// <remarks>
    /// Read as a property of the context instance rather than baked into the model, so the
    /// compiled model stays shared across requests while the value does not.
    /// </remarks>
    internal Guid CurrentCongregationId => _congregation.IsResolved ? _congregation.CongregationId : Guid.Empty;

    // Congregations are not themselves congregation-owned, so this set is not gated.
    public DbSet<Congregation> Congregations => Set<Congregation>();

    public DbSet<Member> Members => Scoped<Member>();

    public DbSet<InviteCode> InviteCodes => Scoped<InviteCode>();

    public DbSet<JoiningSession> JoiningSessions => Scoped<JoiningSession>();

    public DbSet<SignInToken> SignInTokens => Scoped<SignInToken>();

    public DbSet<Session> Sessions => Scoped<Session>();

    public DbSet<RefreshToken> RefreshTokens => Scoped<RefreshToken>();

    public DbSet<Listing> Listings => Scoped<Listing>();

    public DbSet<ListingRequest> ListingRequests => Scoped<ListingRequest>();

    public DbSet<MessageThread> MessageThreads => Scoped<MessageThread>();

    public DbSet<Message> Messages => Scoped<Message>();

    public DbSet<Notification> Notifications => Scoped<Notification>();

    public DbSet<NotificationPreference> NotificationPreferences => Scoped<NotificationPreference>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampCongregation();

        return base.SaveChangesAsync(cancellationToken);
    }

    Task<int> IBarnabasDbContext.SaveChangesAsync(CancellationToken cancellationToken) =>
        SaveChangesAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BarnabasDbContext).Assembly);

        ConfigureAssignedKeys(modelBuilder);

        var applyFilter = typeof(BarnabasDbContext)
            .GetMethod(nameof(ApplyCongregationFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.IsOwned() || entityType.FindProperty(CongregationIdProperty) is null)
            {
                continue;
            }

            applyFilter.MakeGenericMethod(entityType.ClrType).Invoke(this, [modelBuilder]);
        }
    }

    /// <summary>
    /// Declares that identifiers come from the code that creates the entity, never the store.
    /// </summary>
    /// <remarks>
    /// By convention a Guid key is treated as store-generated, and EF then reads a non-default
    /// key on an untracked entity as evidence that the row already exists. Every entity here is
    /// constructed with its identifier already set, so that inference is exactly wrong: a message
    /// appended to a loaded thread was being saved as an UPDATE against a row that had never been
    /// inserted, and the save failed having written nothing.
    /// <para>
    /// Saying so once, here, is better than remembering it per entity - the failure only appears
    /// for entities reached through a navigation, which is a thin slice of the model and an easy
    /// one to add to without noticing.
    /// </para>
    /// </remarks>
    private static void ConfigureAssignedKeys(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var key in entityType.GetKeys())
            {
                foreach (var property in key.Properties.Where(p => p.ClrType == typeof(Guid)))
                {
                    property.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
                }
            }
        }
    }

    private void ApplyCongregationFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class =>
        modelBuilder.Entity<TEntity>()
            .HasQueryFilter(e => EF.Property<Guid>(e, CongregationIdProperty) == CurrentCongregationId);

    /// <summary>
    /// Stamps the congregation on every new row from the context rather than from the payload.
    /// </summary>
    /// <remarks>
    /// The value is overwritten rather than defaulted, so supplying another congregation's
    /// identifier in a request body achieves nothing. When no congregation is in scope the
    /// stamp is skipped: the only writes on that path are sign-in records, whose congregation
    /// comes from the verified member record the authentication store returned.
    /// </remarks>
    private void StampCongregation()
    {
        if (!_congregation.IsResolved)
        {
            return;
        }

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Added)
            {
                continue;
            }

            var property = entry.Metadata.FindProperty(CongregationIdProperty);

            if (property is not null)
            {
                entry.Property(CongregationIdProperty).CurrentValue = _congregation.CongregationId;
            }
        }
    }

    private DbSet<TEntity> Scoped<TEntity>()
        where TEntity : class
    {
        if (!_congregation.IsResolved)
        {
            throw new CongregationContextMissingException(typeof(TEntity).Name);
        }

        return Set<TEntity>();
    }
}
