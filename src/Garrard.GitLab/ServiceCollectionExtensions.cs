using System.Net.Http.Headers;
using Garrard.GitLab.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Garrard.GitLab;

/// <summary>
/// Extension methods for registering Garrard.GitLab services with the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Garrard.GitLab services, including a connection-pooled named
    /// <see cref="HttpClient"/> pre-configured with Bearer auth from <see cref="GitLabOptions"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">
    /// Optional delegate to configure <see cref="GitLabOptions"/> directly (e.g. in tests or
    /// simple console apps). When omitted, options are expected to come from the host
    /// configuration (bind the <c>GitLab</c> section or set <c>GitLab__Pat</c> /
    /// <c>GitLab__Domain</c> environment variables).
    /// </param>
    public static IServiceCollection AddGarrardGitLab(
        this IServiceCollection services,
        Action<GitLabOptions>? configureOptions = null)
    {
        if (configureOptions is not null)
            services.Configure(configureOptions);

        // Named HttpClient reads options at first use so PAT rotation takes effect without restart.
        services.AddHttpClient(DefaultGitLabHttpClientFactory.ClientName, (sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<GitLabOptions>>().Value;
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", opts.Pat);
            client.BaseAddress = new Uri($"https://{opts.Domain}/api/v4/");
        });

        services.AddSingleton<IGitLabHttpClientFactory, DefaultGitLabHttpClientFactory>();
        return services;
    }
}
