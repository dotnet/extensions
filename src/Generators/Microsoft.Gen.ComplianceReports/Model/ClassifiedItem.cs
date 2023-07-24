// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Gen.ComplianceReports;

/// <summary>
/// A classified field or property.
/// </summary>
internal sealed class ClassifiedItem
{
    public string SourceFilePath = string.Empty;
    public int SourceLine;

    public string Name = string.Empty;
    public string TypeName = string.Empty;
    public List<Classification> Classifications = new();
}
