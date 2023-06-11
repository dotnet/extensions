// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    /// The default value is 0.
    /// </value>
    /// <remarks>
    /// You typically pick a unique value for your application and don't change it afterwards. You'll want a different value for
    /// different deployment environments in order to prevent identifiers from one environment being redacted to the same
    /// value across environments.
    /// </remarks>
    public ulong HashSeed { get; set; }
}
