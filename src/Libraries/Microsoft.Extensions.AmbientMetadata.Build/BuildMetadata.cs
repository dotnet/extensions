// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AmbientMetadata;

/// <summary>
/// Data model for the Build metadata.
/// </summary>
/// <remarks>
/// The values are automatically grabbed from environment variables at build time in CI pipeline and saved in generated code.
/// At startup time, the class properties will be initialized from the generated code.
/// Currently supported CI pipelines:
/// <list type="bullet">
///   <item><see href="https://learn.microsoft.com/azure/devops/pipelines/build/variables#build-variables-devops-services">Azure DevOps</see></item>
///   <item><see href="https://docs.github.com/en/actions/reference/variables-reference#default-environment-variables">GitHub Actions</see></item>
/// </list>
/// </remarks>
public class BuildMetadata
{
    /// <summary>
    /// Gets or sets the ID of the record for the build, also known as the run ID.
    /// </summary>
    public string? BuildId { get; set; }

    /// <summary>
    /// Gets or sets the name of the completed build, also known as the run number.
    /// </summary>
    public string? BuildNumber { get; set; }

    /// <summary>
    /// Gets or sets the name of the branch in the triggering repo the build was queued for, also known as the ref name.
    /// </summary>
    public string? SourceBranchName { get; set; }

    /// <summary>
    /// Gets or sets the latest version control change that is included in this build, also known as the commit SHA.
    /// </summary>
    public string? SourceVersion { get; set; }
}
