// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Semantic
{
    internal class SemanticRange
    {
        public SemanticRange(int kind, Range range)
        {
            if (range is null)
            {
                throw new ArgumentNullException(nameof(range));
            }

            Kind = kind;
            Range = range;
        }

        public Range Range { get; set; }

        public int Kind { get; set; }
    }
}
