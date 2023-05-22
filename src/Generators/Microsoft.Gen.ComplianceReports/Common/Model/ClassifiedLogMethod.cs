// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Gen.ComplianceReports;

/// <summary>
/// A log method containing classified members.
/// </summary>
internal sealed class ClassifiedLogMethod
{
    public string MethodName = string.Empty;
    public string LogMethodMessage = string.Empty;
    public List<ClassifiedItem> Parameters = new();
}
