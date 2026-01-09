namespace Globomantics.Mcp.Server;

public static class FunctionsAppBuilderExtensions
{
    public static WebApplicationBuilder AddFunctionHostUrls(this WebApplicationBuilder builder)
    {
        var port = 
            int.TryParse(builder.Configuration["port"], out var cliPort) ? cliPort :
            int.TryParse(Environment.GetEnvironmentVariable("FUNCTIONS_CUSTOMHANDLER_PORT"), out var envPort) ? envPort :
            5000;

        var hostname = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME") ?? $"localhost:{port}";
        builder.WebHost.UseUrls($"http://localhost:{port}");

        Console.WriteLine($"Configuring MCP Server to use hostname: {hostname}");
        Console.WriteLine($"Configuring MCP Server to bind to: http://localhost:{port}");

        return builder;
    }
}