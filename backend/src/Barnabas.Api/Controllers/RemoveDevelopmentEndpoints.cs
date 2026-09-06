using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Barnabas.Api.Controllers;

/// <summary>
/// Takes the development-only endpoints out of the application entirely.
/// </summary>
/// <remarks>
/// Not a runtime check inside the action, and not a feature flag. The controller is removed
/// from the model, so outside Development the route does not exist to be probed at all - which
/// is a stronger statement than one that answers 404 by choice.
/// </remarks>
public sealed class RemoveDevelopmentEndpoints : IApplicationFeatureProvider<ControllerFeature>
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        ArgumentNullException.ThrowIfNull(feature);

        feature.Controllers.Remove(typeof(DevelopmentController).GetTypeInfo());
    }
}
