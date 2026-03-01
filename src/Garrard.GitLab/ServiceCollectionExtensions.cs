using System.Net.Http.Headers;
using Garrard.GitLab.Library.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Garrard.GitLab.Library;

/// <summary>
/// Extension methods for registering Garrard.GitLab services with the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Garrard.GitLab services, including a connection-pooled named
    /// <see cref="HttpClient"/> pre-configured with Bearer auth from <see cref="GitLabOptions"/>,
    /// and all client classes as singletons.
    /// </summary>
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

        // Register instance client classes
        services.AddSingleton<GroupClient>();
        services.AddSingleton<ProjectClient>();
        services.AddSingleton<GroupVariableClient>();
        services.AddSingleton<GitClient>();
        services.AddSingleton<FileClient>();
        services.AddSingleton<SummaryClient>();

        return services;
    }
}
