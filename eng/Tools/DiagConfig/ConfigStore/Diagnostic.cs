// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace DiagConfig.ConfigStore;

internal sealed class Diagnostic
{
    public Metadata Metadata { get; set; } = new Metadata();
    public int Tier { get; set; } = 1;
    public IDictionary<string, DiagnosticSetting?> Attributes { get; set; } = new Dictionary<string, DiagnosticSetting?>();
}
