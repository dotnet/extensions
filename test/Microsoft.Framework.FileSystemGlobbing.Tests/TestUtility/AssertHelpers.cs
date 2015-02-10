// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Microsoft.Framework.FileSystemGlobbing.Tests.TestUtility
{
    public static class AssertHelpers
    {
        public static void SortAndEqual<T>(IEnumerable<T> expect, IEnumerable<T> actual, IEqualityComparer<T> comparer)
        {
            var missing = expect.Except(actual, comparer);
            var extra = actual.Except(expect, comparer);
            var errorMessage = new StringBuilder("Two collections are not equal");

            if (missing.Any())
            {
                errorMessage.Append("\nMissing:\n");
                foreach (var missedElement in missing)
                {
                    errorMessage.AppendFormat("\t{0}\n", missedElement.ToString());
                }
            }

            if (extra.Any())
            {
                errorMessage.Append("\nExtra:\n");
                foreach (var extraElement in extra)
                {
                    errorMessage.AppendFormat("\t{0}\n", extraElement.ToString());
                }
            }

            Assert.True(!missing.Any() && !extra.Any(), errorMessage.ToString());
        }
    }
}