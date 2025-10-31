// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DataIngestion;

public sealed class IngestionPipelineOptions
{
    private string _activitySourceName = DiagnosticsConstants.ActivitySourceName;

    public string ActivitySourceName
    {
        get => _activitySourceName;
        set => _activitySourceName = string.IsNullOrWhiteSpace(value) ? throw new ArgumentNullException(nameof(value)) : value;
    }
}
