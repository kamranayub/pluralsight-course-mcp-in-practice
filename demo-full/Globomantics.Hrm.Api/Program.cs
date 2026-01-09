using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System.Text.Json;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

if (DefaultOpenApiConfigurationOptions.IsFunctionsRuntimeEnvironmentDevelopment()) {
    builder.UseMiddleware<DevelopmentAuthMiddleware>();
}

builder.AddServiceDefaults();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights()
    .Configure<JsonSerializerOptions>(options =>
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    })
    .AddSingleton<IOpenApiConfigurationOptions>(_ =>
    {
        return new OpenApiConfigurationOptions
        {
            Info = new OpenApiInfo
            {
                Version = DefaultOpenApiConfigurationOptions.GetOpenApiDocVersion(),
                Title = "Globomantics HRM API",
                Description = "Demonstration API for Globomantics HRM based on Workday",                
            },
            Servers = DefaultOpenApiConfigurationOptions.GetHostNames(),
            OpenApiVersion = DefaultOpenApiConfigurationOptions.GetOpenApiVersion(),
            IncludeRequestingHostName = false,
            ForceHttps = DefaultOpenApiConfigurationOptions.IsHttpsForced(),
            ForceHttp = DefaultOpenApiConfigurationOptions.IsHttpForced(),
        };
    });

builder.Build().Run();
