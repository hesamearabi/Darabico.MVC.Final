using Microsoft.Extensions.FileProviders;

namespace final_p_01.Extensions;

public static class StaticAssetExtensions
{
    public static IApplicationBuilder UseLegacyImagePaths(this IApplicationBuilder app, IWebHostEnvironment environment)
    {
        foreach (var folder in new[] { "H-IMG", "footerimg", "sliderIMG", "siteLOGO" })
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(environment.WebRootPath, "images", folder)),
                RequestPath = "/" + folder
            });
        }
        return app;
    }
}
