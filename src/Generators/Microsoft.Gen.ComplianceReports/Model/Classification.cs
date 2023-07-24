// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Gen.ComplianceReports;

/// <summary>
/// A classified field or property.
/// </summary>
internal sealed class Classification
{
    public string Name = string.Empty;
    public string? Notes;
}
