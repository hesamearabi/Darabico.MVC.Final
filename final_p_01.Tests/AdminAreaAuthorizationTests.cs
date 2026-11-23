using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace final_p_01.Tests;

public class AdminAreaAuthorizationTests
{
    [Fact]
    public void SettingsFormsAreIndependent()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "final_p_01.sln"))) root = root.Parent;
        var view = File.ReadAllText(Path.Combine(root!.FullName, "final_p_01/Areas/Admin/Views/Settings/Index.cshtml"));
        var depth = 0;
        foreach (System.Text.RegularExpressions.Match tag in System.Text.RegularExpressions.Regex.Matches(view, @"</?form\b[^>]*>"))
        {
            depth += tag.Value.StartsWith("</", StringComparison.Ordinal) ? -1 : 1;
            Assert.InRange(depth, 0, 1);
        }
        Assert.Equal(0, depth);
        Assert.Contains("asp-action=\"SaveSeo\"", view);
        Assert.Contains("asp-action=\"Index\"", view);
    }

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