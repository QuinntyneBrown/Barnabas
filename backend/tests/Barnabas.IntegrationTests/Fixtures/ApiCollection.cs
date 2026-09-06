namespace Barnabas.IntegrationTests.Fixtures;

/// <summary>
/// One API and one database for the whole suite.
/// </summary>
/// <remarks>
/// Starting a container per test class would dominate the run time. Tests share the host and
/// reset the data between them instead, which is why they do not run in parallel with each
/// other - a shared database and parallel writers would make every assertion conditional on
/// what else happened to be running.
/// </remarks>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ApiCollection : ICollectionFixture<BarnabasApiFactory>
{
    public const string Name = "api";
}
