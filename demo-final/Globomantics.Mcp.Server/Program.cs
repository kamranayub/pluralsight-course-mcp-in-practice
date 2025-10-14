using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateEmptyApplicationBuilder(settings: null);

builder.Logging.AddConsole(consoleLogOptions =>
{
    // Configure all logs to go to stderr
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithResourcesFromAssembly()
    .WithToolsFromAssembly()
    .WithPromptsFromAssembly();

var app = builder.Build();

await app.RunAsync();
