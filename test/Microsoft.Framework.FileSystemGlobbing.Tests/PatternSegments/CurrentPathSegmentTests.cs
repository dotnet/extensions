// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.FileSystemGlobbing.Internal.PathSegments;
using Xunit;

namespace Microsoft.Framework.FileSystemGlobbing.Tests.PatternSegments
{
    public class CurrentPathSegmentTests
    {
        [Theory]
        [InlineData("anything", StringComparison.Ordinal)]
        [InlineData("anything", StringComparison.OrdinalIgnoreCase)]
        [InlineData("anything", StringComparison.CurrentCulture)]
        [InlineData("anything", StringComparison.CurrentCultureIgnoreCase)]
        [InlineData("", StringComparison.Ordinal)]
        [InlineData(null, StringComparison.Ordinal)]
        public void Match(string testSample, StringComparison comparerType)
        {
            var pathSegment = new CurrentPathSegment();
            Assert.False(pathSegment.Match(testSample, comparerType));
        }
    }
}