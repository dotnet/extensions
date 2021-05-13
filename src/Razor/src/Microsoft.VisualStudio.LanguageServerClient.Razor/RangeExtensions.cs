// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    internal static class RangeExtensions
    {
        // Internal for testing only
        internal static readonly Range UndefinedRange = new()
        {
            Start = new Position(-1, -1),
            End = new Position(-1, -1)
        };

        public static bool IsUndefined(this Range range)
        {
            if (range is null)
            {
                throw new ArgumentNullException(nameof(range));
            }

            return range == UndefinedRange;
        }
    }
}
