// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace DiagConfig.ConfigStore;

internal sealed class DiagnosticSetting
{
    public Severity Severity { get; set; }
    public string? Comment { get; set; }
    public string? Redundant { get; set; }
    public IList<string>? Options { get; set; }
}
