// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Options to control redaction.
/// </summary>
public class LoggerRedactionOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether a salt value is applied to each redacted value.
    /// </summary>
    /// <value>
    /// Defaults to <see langword="true" />.
    /// </value>
    public bool ApplySalt { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to include the current day of the year in the salt value.
    /// </summary>
    /// <value>
    /// Defaults to <see langword="true" />.
    /// </value>
    public bool IncludeDayOfYearInSalt { get; set; } = true;
}
