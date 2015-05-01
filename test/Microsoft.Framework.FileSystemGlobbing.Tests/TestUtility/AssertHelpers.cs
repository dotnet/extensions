// Copyright (c) .NET Foundation. All rights reserved.
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
            var result = expect.OrderBy(elem => elem).SequenceEqual(actual.OrderBy(elem => elem), comparer);
            var message = string.Empty;

            if (!result)
            {
                var missing = expect.Except(actual, comparer);
                var extra = actual.Except(expect, comparer);
                var errorMessageBuilder = new StringBuilder("Two collections are not equal");

                if (missing.Any())
                {
                    errorMessageBuilder.Append("\nMissing:\n");
                    foreach (var missedElement in missing)
                    {
                        errorMessageBuilder.AppendFormat("\t{0}\n", missedElement.ToString());
                    }
                }

                if (extra.Any())
                {
                    errorMessageBuilder.Append("\nExtra:\n");
                    foreach (var extraElement in extra)
                    {
                        errorMessageBuilder.AppendFormat("\t{0}\n", extraElement.ToString());
                    }
                }

                message = errorMessageBuilder.ToString();
            }

            Assert.True(result, message);
        }
    }
}