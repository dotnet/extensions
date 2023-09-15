// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Logging;

public sealed partial class LoggerMessageState : IReadOnlyList<KeyValuePair<string, object?>>
{
    /// <inheritdoc/>
    public KeyValuePair<string, object?> this[int index] => _tags[Throw.IfOutOfRange(index, 0, TagsCount)];

    /// <inheritdoc/>
    int IReadOnlyCollection<KeyValuePair<string, object?>>.Count => TagsCount;

    /// <inheritdoc/>
    IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator()
    {
        for (int i = 0; i < TagsCount; i++)
        {
            yield return _tags[i];
        }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        for (int i = 0; i < TagsCount; i++)
        {
            yield return _tags[i];
        }
    }
}
