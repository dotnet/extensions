// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    internal static class RangeExtensions
    {
        private static readonly Range UndefinedRange = new Range
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
