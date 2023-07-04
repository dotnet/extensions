// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Extensions.Telemetry.Logging;

internal sealed partial class ExtendedLogger
{
    /// <summary>
    /// Takes distinct dynamic and static properties and makes 'em look like a single IReadOnlyList.
    /// </summary>
    internal sealed class PropertyJoiner : IReadOnlyList<KeyValuePair<string, object?>>
    {
        public readonly KeyValuePair<string, object?>[] StaticProperties;
        public LoggerMessageState State = null!;
        public Func<LoggerMessageState, Exception?, string> Formatter = null!;

        public PropertyJoiner(KeyValuePair<string, object?>[] staticProperties)
        {
            StaticProperties = staticProperties;
        }

        public KeyValuePair<string, object?> this[int index]
            => index < State.NumProperties
                ? State.Properties[index]
                : StaticProperties[index - State.NumProperties];

        public int Count => State.NumProperties + StaticProperties.Length;

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
