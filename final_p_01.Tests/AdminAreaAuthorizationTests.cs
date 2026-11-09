using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace final_p_01.Tests;

public class AdminAreaAuthorizationTests
{
    [Fact]
    public void AdminControllers_MustHaveAuthorizeAttribute()
    {
        var assembly = typeof(Areas.Admin.Controllers.SettingsController).Assembly;
        var controllers = assembly.GetTypes()
            .Where(t => typeof(Controller).IsAssignableFrom(t) && !t.IsAbstract)
            .Where(t => t.Namespace != null && t.Namespace.StartsWith("final_p_01.Areas.Admin.Controllers"))
            .ToList();

        foreach (var controller in controllers)
        {
            if (controller.Name == "AuthController")
                continue;

            var hasAuthorize = controller.GetCustomAttributes<AuthorizeAttribute>(true).Any() ||
                               (controller.BaseType != null && controller.BaseType.GetCustomAttributes<AuthorizeAttribute>(true).Any());

            Assert.True(hasAuthorize);
        }
    }
}