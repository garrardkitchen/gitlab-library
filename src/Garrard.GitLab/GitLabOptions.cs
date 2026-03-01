using System.ComponentModel.DataAnnotations;

namespace Garrard.GitLab;

/// <summary>
/// Configuration options for the Garrard.GitLab library.
/// Bind from the <c>GitLab</c> configuration section or configure directly via
/// <see cref="ServiceCollectionExtensions.AddGarrardGitLab(Microsoft.Extensions.DependencyInjection.IServiceCollection,System.Action{GitLabOptions}?)"/>.
/// </summary>
public sealed class GitLabOptions
{
    /// <summary>The configuration section name when binding from appsettings / environment variables.</summary>
    public const string SectionName = "GitLab";

    /// <summary>GitLab Personal Access Token. Maps to env var <c>GitLab__Pat</c>.</summary>
    [Required(AllowEmptyStrings = false)]
    public string Pat { get; set; } = string.Empty;

    /// <summary>GitLab domain (without scheme). Defaults to <c>gitlab.com</c>.</summary>
    public string Domain { get; set; } = "gitlab.com";
}
