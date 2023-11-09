// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Options to control redaction.
/// </summary>
public class LoggerRedactionOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether a discriminator value is applied to each redacted value.
    /// </summary>
    /// <value>
    /// Defaults to <see langword="true" />.
    /// </value>
    /// <remarks>
    /// The discriminator is used to constrain the ability to correlate data between pieces of state. When this property is <see langword="true" />,
    /// the tag name of each value is included as part of the redacted text, generally making it impossible to correlate data between
    /// tags of different names.
    /// </remarks>
    public bool ApplyDiscriminator { get; set; } = true;
}
