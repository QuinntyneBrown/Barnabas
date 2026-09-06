using Barnabas.Domain.Members;

namespace Barnabas.Infrastructure.Persistence.Seeding;

/// <summary>
/// The congregation, its codes, and its members, as fixed values.
/// </summary>
/// <remarks>
/// Feature slice 1 seeds these rather than provisioning them through the product. Two real
/// identities are what make authorisation and tenant scoping mean anything; the administrative
/// surface that creates them is a later slice, and building it first would prove nothing.
/// <para>
/// The identifiers are constants rather than generated, so an acceptance test can name a
/// member without first reading one back, and a failure names something a reader recognises.
/// St. Brigid's exists solely so that every cross-congregation assertion has a real other side.
/// </para>
/// </remarks>
public static class SeedData
{
    public static class StAidans
    {
        public static readonly Guid Id = new("a0000000-0000-0000-0000-00000000a1d5");

        public const string Name = "St. Aidan's";

        public static readonly string[] Neighbourhoods =
        [
            "Riverdale",
            "Leslieville",
            "The Danforth",
            "Scarborough",
            "The Beaches",
            "East York",
            "Cabbagetown",
            "North York",
        ];

        public static readonly Guid InviteCodeId = new("a0000000-0000-0000-0000-0000000c0de1");

        public const string InviteCode = "AIDAN-2026";
    }

    public static class StBrigids
    {
        public static readonly Guid Id = new("b0000000-0000-0000-0000-00000000b716");

        public const string Name = "St. Brigid's";

        public static readonly string[] Neighbourhoods = ["Parkdale", "High Park"];

        public static readonly Guid InviteCodeId = new("b0000000-0000-0000-0000-0000000c0de2");

        public const string InviteCode = "BRIGID-2026";
    }

    /// <summary>Owns the ladder. Every ownership assertion is written from her side.</summary>
    public static class Marion
    {
        public static readonly Guid Id = new("11111111-0000-0000-0000-000000000001");

        public const string EmailAddress = "marion@example.com";

        public const string DisplayName = "Marion T.";

        public const string Neighbourhood = "Riverdale";
    }

    /// <summary>Asks to borrow it.</summary>
    public static class Priya
    {
        public static readonly Guid Id = new("11111111-0000-0000-0000-000000000002");

        public const string EmailAddress = "priya@example.com";

        public const string DisplayName = "Priya K.";

        public const string Neighbourhood = "Leslieville";
    }

    /// <summary>Neither owner nor requester, which is what makes her useful.</summary>
    public static class Grace
    {
        public static readonly Guid Id = new("11111111-0000-0000-0000-000000000003");

        public const string EmailAddress = "grace@example.com";

        public const string DisplayName = "Grace Papadopoulos";

        public const string Neighbourhood = "The Danforth";
    }

    /// <summary>Belongs to the other congregation, and may see none of this.</summary>
    public static class Hank
    {
        public static readonly Guid Id = new("22222222-0000-0000-0000-000000000001");

        public const string EmailAddress = "hank@example.com";

        public const string DisplayName = "Hank W.";

        public const string Neighbourhood = "Parkdale";
    }

    public static class Listings
    {
        public static readonly Guid Ladder = new("33333333-0000-0000-0000-000000000001");

        public const string LadderTitle = "6ft aluminum step ladder";

        /// <summary>
        /// Seeded, not postable. The Sell form is out of slice, but L2-062 requires a request
        /// on a Sell listing sent to the loan endpoint to be refused for mismatched kind, and
        /// that needs a Sell listing to exist.
        /// </summary>
        public static readonly Guid Drill = new("33333333-0000-0000-0000-000000000002");

        public const string DrillTitle = "DeWalt cordless drill, two batteries";

        /// <summary>In St. Brigid's, so a member of St. Aidan's must receive 404 for it.</summary>
        public static readonly Guid Canoe = new("44444444-0000-0000-0000-000000000001");

        public const string CanoeTitle = "16ft Nova Craft canoe and paddles";
    }

    public const MemberRole DefaultRole = MemberRole.Member;
}
