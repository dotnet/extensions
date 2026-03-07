// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace DiagConfig.ConfigStore;

internal sealed class Metadata
{
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? HelpLinkUri { get; set; }
    public IList<string>? CustomTags { get; set; }
    public Severity DefaultSeverity { get; set; }
}
