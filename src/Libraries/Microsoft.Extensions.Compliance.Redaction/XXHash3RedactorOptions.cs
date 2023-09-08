// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Compliance.Redaction;

/// <summary>
/// Options for the xxHash redactor.
/// </summary>
public class XXHash3RedactorOptions
{
    /// <summary>
    /// Gets or sets a hash seed used when computing hashes during redaction.
    /// </summary>
    /// <value>
    /// You are required to provide this value, as it defaults to 0, but 0 is not accepted at runtime.
    /// </value>
    /// <remarks>
    /// You typically pick a unique value for your application and don't change it afterwards. You'll want a different value for
    /// different deployment environments in order to prevent identifiers from one environment being redacted to the same
    /// value across environments.
    ///
    /// Treat this value as secret in order to reduce the risk of an attacker being able to reverse the redaction.
    /// </remarks>
    [Required]

    // Due to a bug in the code generator, DeniedValues(0) doesn't work. Until that bug is fixed,
    // we apply the above Required attribute. When the bug is fixed, removed Required and enable DeniedValues.
    // [DeniedValues(0)]
    public ulong HashSeed { get; set; }
}
