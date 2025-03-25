using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.eShopWeb.Web.Extensions;

public static class DbMigrationsExtensions
{
    public static IServiceCollection ConfigureDatabaseFilters(this IServiceCollection services)
    {
        services.AddDatabaseDeveloperPageExceptionFilter();
        return services;
    }
    
    public static IApplicationBuilder ConfigureDatabaseFilters(this IApplicationBuilder app)
    {
        return app;
    }
}