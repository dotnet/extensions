// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Telemetry.Logging;

internal sealed partial class ExtendedLogger
{
    /// <summary>
    /// Takes distinct dynamic and static properties and makes 'em look like a single IReadOnlyList.
    /// </summary>
    private sealed class PropertyJoiner : IReadOnlyList<KeyValuePair<string, object?>>
    {
        public LoggerMessageState State = null!;
        public KeyValuePair<string, object?>[] StaticProperties = null!;
        public Func<LoggerMessageState, Exception?, string> Formatter = null!;

        public KeyValuePair<string, object?> this[int index]
            => index < State.NumProperties
                ? State.Properties[index]
                : StaticProperties[index - State.NumProperties];

        public int Count => State.NumProperties + StaticProperties.Length;

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            var count = State.NumProperties;
            for (int i = 0; i < count; i++)
            {
                yield return State.Properties[i];
            }

            foreach (var p in StaticProperties)
            {
                yield return p;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
