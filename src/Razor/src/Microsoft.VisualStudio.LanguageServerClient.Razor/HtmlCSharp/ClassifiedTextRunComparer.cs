// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.Internal;
using Microsoft.VisualStudio.Text.Adornments;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    internal class ClassifiedTextRunComparer : IEqualityComparer<ClassifiedTextRun>
    {
        public static ClassifiedTextRunComparer Default = new();

        public bool Equals(ClassifiedTextRun x, ClassifiedTextRun y)
        {
            if (x is null && y is null)
            {
                return true;
            }
            else if (x is null ^ y is null)
            {
                return false;
            }

            return x.ClassificationTypeName == y.ClassificationTypeName &&
                x.Text == y.Text;
        }

        public int GetHashCode(ClassifiedTextRun obj)
        {
            var hashCodeCombiner = new HashCodeCombiner();
            hashCodeCombiner.Add(obj.Text);
            hashCodeCombiner.Add(obj.ClassificationTypeName);
            return hashCodeCombiner.CombinedHash;
        }
    }
}
