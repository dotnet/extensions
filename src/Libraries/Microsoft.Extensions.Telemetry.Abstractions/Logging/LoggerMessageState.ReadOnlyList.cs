// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Telemetry.Logging;

public sealed partial class LoggerMessageState : IReadOnlyList<KeyValuePair<string, object?>>
{
    /// <inheritdoc/>
    public KeyValuePair<string, object?> this[int index] => _properties[Throw.IfOutOfRange(index, 0, NumProperties)];

    /// <inheritdoc/>
    int IReadOnlyCollection<KeyValuePair<string, object?>>.Count => NumProperties;

    /// <inheritdoc/>
    IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator()
    {
        for (int i = 0; i < NumProperties; i++)
        {
            yield return _properties[i];
        }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        for (int i = 0; i < NumProperties; i++)
        {
            yield return _properties[i];
        }
    }
}
