namespace Garrard.GitLab.Library.Enums;

/// <summary>GitLab access level for a project access token.</summary>
public enum AccessLevel
{
    NoAccess = 0,
    Minimal = 5,
    Guest = 10,
    Reporter = 20,
    Developer = 30,
    Maintainer = 40,
    Owner = 50
}
