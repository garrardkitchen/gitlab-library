using Garrard.GitLab;
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
    MapLegacyEnvVars(builder.Configuration);

    builder.Services
        .AddGarrardGitLab(opts => BindGitLabOptions(opts, builder.Configuration))
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

    MapLegacyEnvVars(builder.Configuration);

    builder.Services
        .AddGarrardGitLab(opts => BindGitLabOptions(opts, builder.Configuration))
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithToolsFromAssembly(typeof(Program).Assembly);

    await builder.Build().RunAsync();
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

static void MapLegacyEnvVars(IConfigurationManager config)
{
    // Allow both GitLab__Pat / GitLab__Domain (standard .NET env-var binding) AND
    // GL_PAT / GL_DOMAIN (original convention).  Standard binding takes precedence.
    if (string.IsNullOrWhiteSpace(config["GitLab:Pat"]))
        config["GitLab:Pat"] = config["GL_PAT"];

    if (string.IsNullOrWhiteSpace(config["GitLab:Domain"]))
        config["GitLab:Domain"] = config["GL_DOMAIN"];
}

static void BindGitLabOptions(GitLabOptions opts, IConfiguration config)
{
    opts.Pat = config["GitLab:Pat"] ?? string.Empty;
    opts.Domain = config["GitLab:Domain"] ?? "gitlab.com";
}
