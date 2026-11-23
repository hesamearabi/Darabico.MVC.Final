using final_p_01.Application.Common.Interfaces;
using final_p_01.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace final_p_01.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFileStorage(this IServiceCollection services)
    {
        services.AddScoped<IImageService, Services.ImageService>();
        services.AddScoped<IInquiryAttachmentStorage, Services.InquiryAttachmentStorage>();
        return services;
    }

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        return services;
    }
}
