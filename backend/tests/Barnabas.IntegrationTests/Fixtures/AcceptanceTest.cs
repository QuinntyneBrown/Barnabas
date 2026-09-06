using Barnabas.IntegrationTests.Fixtures;

namespace Barnabas.IntegrationTests;

/// <summary>The shape every acceptance test in this suite shares.</summary>
[Collection(Fixtures.ApiCollection.Name)]
public abstract class AcceptanceTest : IAsyncLifetime
{
    protected AcceptanceTest(BarnabasApiFactory api) => Api = api;

    protected BarnabasApiFactory Api { get; }

    public async ValueTask InitializeAsync() => await Api.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
