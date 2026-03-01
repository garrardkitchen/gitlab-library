using Garrard.GitLab.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Garrard.GitLab;

/// <summary>
/// Extension methods for registering Garrard.GitLab services with the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IGitLabHttpClientFactory"/> with the DI container.
    /// Call this in your application's <c>AddServices</c> / <c>ConfigureServices</c>.
    /// </summary>
    public static IServiceCollection AddGarrardGitLab(this IServiceCollection services)
    {
        services.AddSingleton<IGitLabHttpClientFactory, DefaultGitLabHttpClientFactory>();
        return services;
    }
}
