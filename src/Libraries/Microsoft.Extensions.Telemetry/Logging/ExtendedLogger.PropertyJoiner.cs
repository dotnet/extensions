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
        public LoggerMessageState DynamicProperties = null!;
        public KeyValuePair<string, object?>[] StaticProperties = null!;
        public Func<LoggerMessageState, Exception?, string> Formatter = null!;

        public KeyValuePair<string, object?> this[int index]
        {
            get
            {
                _ = Throw.IfOutOfRange(index, 0, Count);

                return index < DynamicProperties.Properties.Count
                    ? DynamicProperties.Properties[index]
                    : StaticProperties[index - DynamicProperties.Properties.Count];
            }
        }

        public int Count => DynamicProperties.Properties.Count + StaticProperties.Length;

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            foreach (var p in DynamicProperties.Properties)
            {
                yield return p;
            }

            foreach (var p in StaticProperties)
            {
                yield return p;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
