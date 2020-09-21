// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.Extensions.Internal;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    internal class ClassifiedTextElementComparer : IEqualityComparer<ClassifiedTextElement>
    {
        public static ClassifiedTextElementComparer Default = new ClassifiedTextElementComparer();

        public bool Equals(ClassifiedTextElement x, ClassifiedTextElement y)
        {
            if (x is null && y is null)
            {
                return true;
            }
            else if (x is null ^ y is null)
            {
                return false;
            }
            else if (x.Runs.Count() != y.Runs.Count())
            {
                return false;
            }

            return true;
        }

        public int GetHashCode(ClassifiedTextElement obj)
        {
            var hashCodeCombiner = new HashCodeCombiner();
            hashCodeCombiner.Add(obj.Runs);
            return hashCodeCombiner.CombinedHash;
        }
    }
}
