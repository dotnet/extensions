// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Gen.ComplianceReports;

/// <summary>
/// A type holding classified members and/or log methods.
/// </summary>
internal sealed class ClassifiedType
{
    public string TypeName = string.Empty;
    public List<ClassifiedItem>? Members;
    public List<ClassifiedLogMethod>? LogMethods;
}
