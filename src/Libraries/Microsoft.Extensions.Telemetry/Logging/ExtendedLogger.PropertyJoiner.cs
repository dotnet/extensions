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
        public KeyValuePair<string, object?>[]? StaticProperties;
        public LoggerMessageState? IncomingProperties;
        public Func<LoggerMessageState, Exception?, string>? Formatter;

        public void Clear()
        {
            IncomingProperties = null;
            Formatter = null;
        }

        public KeyValuePair<string, object?> this[int index]
            => index < IncomingProperties!.NumProperties
                ? IncomingProperties.Properties[index]
                : StaticProperties![index - IncomingProperties.NumProperties];

        public int Count => IncomingProperties!.NumProperties + StaticProperties!.Length;

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
