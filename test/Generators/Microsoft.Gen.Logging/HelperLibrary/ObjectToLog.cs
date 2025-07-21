// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.Gen.Logging.Test;

public class ObjectToLog
{
    [LogPropertyIgnore]
    public string? PropertyToIgnore { get; set; }

    public string? PropertyToLog { get; set; }

    [LogProperties]
    public FieldToLog? FieldToLog { get; set; }
}
