using Ardalis.ListStartupServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Services;
using Microsoft.eShopWeb.Infrastructure.Data;
using Microsoft.eShopWeb.Infrastructure.Identity;
using Microsoft.eShopWeb.Infrastructure.Logging;
using Microsoft.eShopWeb.Infrastructure.Services;
using Microsoft.eShopWeb.Web.Interfaces;
using Microsoft.eShopWeb.Web.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;

namespace Microsoft.eShopWeb.Web;

public static class StartupExtensions
{
    public static IServiceCollection AddCookieSettings(this IServiceCollection services)
    {
        services.Configure<CookiePolicyOptions>(options =>
        {
            // This lambda determines whether user consent for non-essential cookies is needed for a given request.
            options.CheckConsentNeeded = context => true;
            options.MinimumSameSitePolicy = SameSiteMode.None;
        });
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.ExpireTimeSpan = TimeSpan.FromHours(1);
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.Cookie = new CookieBuilder
            {
                IsEssential = true // required for auth to work without explicit user consent; adjust to suit your privacy policy
            };
        });

        return services;
    }

    public static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddDefaultUI()
            .AddEntityFrameworkStores<AppIdentityDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }
}