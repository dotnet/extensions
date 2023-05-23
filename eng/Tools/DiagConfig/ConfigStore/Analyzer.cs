// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace DiagConfig.ConfigStore;

internal sealed class Analyzer
{
    public Origin? Origin { get; set; }
    public IDictionary<string, Diagnostic> Diagnostics { get; set; } = new SortedDictionary<string, Diagnostic>();
}
