using Ardalis.ListStartupServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.eShopWeb;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Services;
using Microsoft.eShopWeb.Infrastructure.Data;
using Microsoft.eShopWeb.Infrastructure.Identity;
using Microsoft.eShopWeb.Infrastructure.Logging;
using Microsoft.eShopWeb.Infrastructure.Services;
using Microsoft.eShopWeb.Web;
using Microsoft.eShopWeb.Web.Extensions;
using Microsoft.eShopWeb.Web.Interfaces;
using Microsoft.eShopWeb.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
if (builder.Environment.IsDevelopment())
{
    // use in-memory database
    builder.Services.AddDbContext<CatalogContext>(c =>
        c.UseInMemoryDatabase("Catalog"));

    // Add Identity DbContext
    builder.Services.AddDbContext<AppIdentityDbContext>(options =>
        options.UseInMemoryDatabase("Identity"));
}
else
{
    // use real database
    builder.Services.AddDbContext<CatalogContext>(c =>
        c.UseSqlServer(builder.Configuration.GetConnectionString("CatalogConnection")));

    // Add Identity DbContext
    builder.Services.AddDbContext<AppIdentityDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("IdentityConnection")));
}

builder.Services.AddCookieSettings();
builder.Services.AddIdentityServices();
builder.Services.ConfigureDatabaseFilters();

builder.Services.AddScoped(typeof(IAsyncRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped<ICatalogViewModelService, CachedCatalogViewModelService>();
builder.Services.AddScoped<IBasketService, BasketService>();
builder.Services.AddScoped<IBasketViewModelService, BasketViewModelService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<CatalogViewModelService>();
builder.Services.Configure<CatalogSettings>(builder.Configuration);
builder.Services.AddSingleton<IUriComposer>(new UriComposer(builder.Configuration.Get<CatalogSettings>()));
builder.Services.AddScoped(typeof(IAppLogger<>), typeof(LoggerAdapter<>));
builder.Services.AddTransient<IEmailSender, EmailSender>();

// Add memory cache services
builder.Services.AddMemoryCache();

builder.Services.AddRouting(options =>
{
    // Replace the type and the name used to refer to it with your own
    // IOutboundParameterTransformer implementation
    options.ConstraintMap["slugify"] = typeof(SlugifyParameterTransformer);
});

builder.Services.AddMvc(options =>
{
    options.Conventions.Add(new RouteTokenTransformerConvention(
            new SlugifyParameterTransformer()));
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizePage("/Basket/Checkout");
});

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "My API", Version = "v1" }));
builder.Services.AddHealthChecks();

builder.Services.Configure<ServiceConfig>(config =>
{
    config.Services = new List<ServiceDescriptor>(builder.Services);
    config.Path = "/allservices";
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHealthChecks("/health",
    new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            var result = JsonConvert.SerializeObject(
                new
                {
                    status = report.Status.ToString(),
                    errors = report.Entries.Select(e => new
                    {
                        key = e.Key,
                        value = Enum.GetName(typeof(HealthStatus), e.Value.Status)
                    })
                });
            context.Response.ContentType = MediaTypeNames.Application.Json;
            await context.Response.WriteAsync(result);
        }
    });

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseShowAllServicesMiddleware();
    app.ConfigureDatabaseFilters();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();

// Enable middleware to serve generated Swagger as a JSON endpoint.
app.UseSwagger();

// Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), 
// specifying the Swagger JSON endpoint.
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
});

app.MapControllerRoute("default", "{controller:slugify=Home}/{action:slugify=Index}/{id?}");
app.MapRazorPages();
app.MapHealthChecks("home_page_health_check");
app.MapHealthChecks("api_health_check");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var loggerFactory = services.GetRequiredService<ILoggerFactory>();
    try
    {
        var catalogContext = services.GetRequiredService<CatalogContext>();
        await CatalogContextSeed.SeedAsync(catalogContext, loggerFactory);

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        await AppIdentityDbContextSeed.SeedAsync(userManager);
    }
    catch (Exception ex)
    {
        var logger = loggerFactory.CreateLogger<Program>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}

app.Run();