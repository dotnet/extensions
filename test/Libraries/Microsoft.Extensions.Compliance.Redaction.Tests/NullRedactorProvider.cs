// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.Extensions.Compliance.Redaction.Tests;

internal sealed class NullRedactorProvider : IRedactorProvider
{
    private NullRedactorProvider()
    {
    }

    public static NullRedactorProvider Instance { get; } = new();
    public Redactor GetRedactor(DataClassification classification) => NullRedactor.Instance;
}
