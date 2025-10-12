using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights()
    .AddSingleton<IOpenApiConfigurationOptions>(_ =>
    {
        return new OpenApiConfigurationOptions
        {
            Info = new OpenApiInfo
            {
                Version = DefaultOpenApiConfigurationOptions.GetOpenApiDocVersion(),
                Title = "Globomantics HRM API",
                Description = "Demonstration API for Globomantics HRM based on Workday"
            },
            Servers = DefaultOpenApiConfigurationOptions.GetHostNames(),
            OpenApiVersion = DefaultOpenApiConfigurationOptions.GetOpenApiVersion(),
            IncludeRequestingHostName = DefaultOpenApiConfigurationOptions.IsFunctionsRuntimeEnvironmentDevelopment(),
            ForceHttps = DefaultOpenApiConfigurationOptions.IsHttpsForced(),
            ForceHttp = DefaultOpenApiConfigurationOptions.IsHttpForced(),
        };
    });

builder.Build().Run();
