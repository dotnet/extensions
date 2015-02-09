// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.FileSystemGlobbing.Internal.PathSegments;
using Xunit;

namespace Microsoft.Framework.FileSystemGlobbing.Tests.PatternSegments
{
    public class RecursiveWildcardSegmentTests
    {
        [Theory]
        [InlineData("anything", StringComparison.Ordinal)]
        [InlineData("anything", StringComparison.OrdinalIgnoreCase)]
        public void Match(string testSample, StringComparison comparerType)
        {
            var pathSegment = new RecursiveWildcardSegment();
            Assert.False(pathSegment.Match(testSample, comparerType));
        }
    }
}