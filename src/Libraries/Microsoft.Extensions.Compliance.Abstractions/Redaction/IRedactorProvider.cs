﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.Extensions.Compliance.Redaction;

/// <summary>
/// Provides redactors for different data classifications.
/// </summary>
public interface IRedactorProvider
{
    /// <summary>
    /// Gets the redactor configured to handle the specified data class.
    /// </summary>
    /// <param name="classifications">Data classifications of the data to redact.</param>
    /// <returns>A redactor suitable to redact data of the given class.</returns>
    Redactor GetRedactor(DataClassificationSet classifications);
}
