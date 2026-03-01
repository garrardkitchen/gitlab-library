using Garrard.GitLab.McpServer.Authentication;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

var transport = (Environment.GetEnvironmentVariable("MCP_TRANSPORT") ?? "stdio")
    .Trim()
    .ToLowerInvariant();

if (transport == "http")
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Services
        .AddMcpServer()
        .WithHttpTransport()
        .WithToolsFromAssembly(typeof(Program).Assembly);

    var app = builder.Build();

    // Require API key for all HTTP MCP requests when MCP_API_KEY is set.
    app.UseMiddleware<ApiKeyMiddleware>();
    app.MapMcp();
    app.Run();
}
else
{
    // stdio transport – redirect all logs to stderr so stdout is reserved for MCP protocol messages.
    var builder = Host.CreateApplicationBuilder(args);
    builder.Logging.AddConsole(options =>
    {
        options.LogToStandardErrorThreshold = LogLevel.Trace;
    });

    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithToolsFromAssembly(typeof(Program).Assembly);

    await builder.Build().RunAsync();
}
