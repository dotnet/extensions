// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.FileSystemGlobbing.Internal.PathSegments;
using Xunit;

namespace Microsoft.Framework.FileSystemGlobbing.Tests.PatternSegments
{
    public class ParentPathSegmentTests
    {
        [Theory]
        [InlineData(".", StringComparison.Ordinal, false)]
        [InlineData("..", StringComparison.Ordinal, true)]
        [InlineData("...", StringComparison.Ordinal, false)]
        [InlineData(".", StringComparison.OrdinalIgnoreCase, false)]
        [InlineData("..", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("...", StringComparison.OrdinalIgnoreCase, false)]
        public void Match(string testSample, StringComparison comparerType, bool expectation)
        {
            var pathSegment = new ParentPathSegment();
            Assert.Equal(expectation, pathSegment.Match(testSample, comparerType));
        }
    }
}