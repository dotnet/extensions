// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;

namespace Microsoft.Extensions.Http.Diagnostics.Bench.Benchmarks;

internal sealed class ErasingRedactorProvider : IRedactorProvider
{
    public static ErasingRedactorProvider Instance { get; } = new();

    public Redactor GetRedactor(DataClassificationSet classifications) => ErasingRedactor.Instance;
}
