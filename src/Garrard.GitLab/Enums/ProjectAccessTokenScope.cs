namespace Garrard.GitLab.Library.Enums;

/// <summary>Scopes that can be granted to a GitLab project access token.</summary>
[Flags]
public enum ProjectAccessTokenScope
{
    Api                  = 1 << 0,
    ReadApi              = 1 << 1,
    ReadRepository       = 1 << 2,
    WriteRepository      = 1 << 3,
    ReadRegistry         = 1 << 4,
    WriteRegistry        = 1 << 5,
    ReadPackageRegistry  = 1 << 6,
    WritePackageRegistry = 1 << 7,
    CreateRunner         = 1 << 8,
    ManageRunner         = 1 << 9,
    AiFeatures           = 1 << 10,
    K8sProxy             = 1 << 11,
    ReadObservability    = 1 << 12,
    WriteObservability   = 1 << 13
}
