using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Routing;

namespace InnovatEpam.Portal.Api.Conventions;

/// <summary>
/// Applies a global <c>api/v1</c> prefix to every controller (T030,
/// Constitution Principle II). Controllers may still declare their own route
/// segment via <c>[Route("auth")]</c>; the convention prepends the version.
/// </summary>
public sealed class ApiVersionRouteConvention : IApplicationModelConvention
{
    private readonly AttributeRouteModel _prefix = new(new Microsoft.AspNetCore.Mvc.RouteAttribute("api/v1"));

    public void Apply(ApplicationModel application)
    {
        ArgumentNullException.ThrowIfNull(application);
        foreach (var controller in application.Controllers)
        {
            foreach (var selector in controller.Selectors)
            {
                if (selector.AttributeRouteModel is { } existing)
                {
                    selector.AttributeRouteModel = AttributeRouteModel.CombineAttributeRouteModel(_prefix, existing);
                }
                else
                {
                    selector.AttributeRouteModel = _prefix;
                }
            }
        }
    }
}
