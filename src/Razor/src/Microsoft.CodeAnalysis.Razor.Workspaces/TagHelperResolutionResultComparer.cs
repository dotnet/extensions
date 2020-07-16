// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.Internal;

namespace Microsoft.CodeAnalysis.Razor
{
    internal sealed class TagHelperResolutionResultComparer : IEqualityComparer<TagHelperResolutionResult>
    {
        internal static readonly TagHelperResolutionResultComparer Default = new TagHelperResolutionResultComparer();

        public bool Equals(TagHelperResolutionResult x, TagHelperResolutionResult y)
        {
            if (x is null && y is null)
            {
                return true;
            }
            else if (x is null ^ y is null)
            {
                return false;
            }

            return x.Descriptors.SequenceEqual(y.Descriptors, TagHelperDescriptorComparer.Default) &&
                x.Diagnostics.SequenceEqual(y.Diagnostics);
        }

        public int GetHashCode(TagHelperResolutionResult obj)
        {
            var hashCodeCombiner = new HashCodeCombiner();
            hashCodeCombiner.Add(obj.Descriptors);
            hashCodeCombiner.Add(obj.Diagnostics);
            return hashCodeCombiner.CombinedHash;
        }
    }
}
