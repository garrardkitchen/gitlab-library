using Garrard.GitLab.Library;
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

    // Map legacy GL_PAT / GL_DOMAIN env vars into the GitLab options section so both
    // styles work: GitLab__Pat=... (standard .NET) and GL_PAT=... (legacy).
    if (string.IsNullOrWhiteSpace(builder.Configuration["GitLab:Pat"]))
        builder.Configuration["GitLab:Pat"] = builder.Configuration["GL_PAT"];

    if (string.IsNullOrWhiteSpace(builder.Configuration["GitLab:Domain"]))
        builder.Configuration["GitLab:Domain"] = builder.Configuration["GL_DOMAIN"];

    builder.Services
        .AddGarrardGitLab(opts =>
        {
            opts.Pat = builder.Configuration["GitLab:Pat"] ?? string.Empty;
            opts.Domain = builder.Configuration["GitLab:Domain"] ?? "gitlab.com";
        })
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

    // Map legacy GL_PAT / GL_DOMAIN env vars.
    if (string.IsNullOrWhiteSpace(builder.Configuration["GitLab:Pat"]))
        builder.Configuration["GitLab:Pat"] = builder.Configuration["GL_PAT"];

    if (string.IsNullOrWhiteSpace(builder.Configuration["GitLab:Domain"]))
        builder.Configuration["GitLab:Domain"] = builder.Configuration["GL_DOMAIN"];

    builder.Services
        .AddGarrardGitLab(opts =>
        {
            opts.Pat = builder.Configuration["GitLab:Pat"] ?? string.Empty;
            opts.Domain = builder.Configuration["GitLab:Domain"] ?? "gitlab.com";
        })
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithToolsFromAssembly(typeof(Program).Assembly);

    await builder.Build().RunAsync();
}
