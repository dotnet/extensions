// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.FileSystemGlobbing.Internal;
using Microsoft.Framework.FileSystemGlobbing.Internal.Patterns;
using Xunit;

namespace Microsoft.Framework.FileSystemGlobbing.Tests.Patterns
{
    public class PatternTests
    {
        [Theory]
        [InlineData("abc", 1)]
        [InlineData("/abc", 1)]
        [InlineData("/abc/", 1)]
        [InlineData("abc/", 1)]
        [InlineData("abc/efg", 2)]
        [InlineData("abc/efg/", 2)]
        [InlineData("abc/efg/h*j", 3)]
        [InlineData("abc/efg/h*j/*.*", 4)]
        [InlineData("abc/efg/h*j/*.*/", 4)]
        [InlineData("abc/efg/hij", 3)]
        [InlineData("abc/efg/hij/klm", 4)]
        [InlineData("../abc/efg/hij/klm", 5)]
        [InlineData("../../abc/efg/hij/klm", 6)]
        public void BuildLinearPattern(string sample, int segmentCount)
        {
            var pattern = PatternBuilder.Build(sample);

            Assert.True(pattern is ILinearPattern);
            Assert.Equal(segmentCount, (pattern as ILinearPattern).Segments.Count);
        }

        [Theory]
        [InlineData("abc/efg/**")]
        [InlineData("/abc/efg/**")]
        [InlineData("abc/efg/**/hij/klm")]
        [InlineData("abc/efg/**/hij/**/klm")]
        [InlineData("abc/efg/**/hij/**/klm/**")]
        [InlineData("abc/efg/**/hij/**/klm/**/")]
        [InlineData("**/hij/**/klm")]
        [InlineData("**/hij/**")]
        [InlineData("/**/hij/**")]
        [InlineData("**/**/hij/**")]
        [InlineData("ab/**/**/hij/**")]
        [InlineData("ab/**/**/hij/**/")]
        [InlineData("/ab/**/**/hij/**/")]
        [InlineData("/ab/**/**/hij/**")]
        public void BuildLinearPatternNegative(string sample)
        {
            var pattern = PatternBuilder.Build(sample) as ILinearPattern;

            Assert.Null(pattern);
        }


        [Theory]
        [InlineData("abc/efg/**", 3, 2, 0, 0)]
        [InlineData("/abc/efg/**", 3, 2, 0, 0)]
        [InlineData("abc/efg/**/hij/klm", 5, 2, 0, 2)]
        [InlineData("abc/efg/**/hij/**/klm", 6, 2, 1, 1)]
        [InlineData("abc/efg/**/hij/**/klm/**", 7, 2, 2, 0)]
        [InlineData("abc/efg/**/hij/**/klm/**/", 7, 2, 2, 0)]
        [InlineData("**/hij/**/klm", 4, 0, 1, 1)]
        [InlineData("**/hij/**", 3, 0, 1, 0)]
        [InlineData("/**/hij/**", 3, 0, 1, 0)]
        [InlineData("**/**/hij/**", 4, 0, 1, 0)]
        [InlineData("ab/**/**/hij/**", 5, 1, 1, 0)]
        [InlineData("ab/**/**/hij/**/", 5, 1, 1, 0)]
        [InlineData("/ab/**/**/hij/**/", 5, 1, 1, 0)]
        [InlineData("/ab/**/**/hij/**", 5, 1, 1, 0)]
        public void BuildRaggedPattern(string sample,
                             int segmentCount,
                             int startSegmentsCount,
                             int containSegmentCount,
                             int endSegmentCount)
        {
            var pattern = PatternBuilder.Build(sample) as IRaggedPattern;

            Assert.NotNull(pattern);
            Assert.Equal(segmentCount, pattern.Segments.Count);
            Assert.Equal(startSegmentsCount, pattern.StartsWith.Count);
            Assert.Equal(endSegmentCount, pattern.EndsWith.Count);
            Assert.Equal(containSegmentCount, pattern.Contains.Count);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("/abc")]
        [InlineData("/abc/")]
        [InlineData("abc/")]
        [InlineData("abc/efg")]
        [InlineData("abc/efg/")]
        [InlineData("abc/efg/h*j")]
        [InlineData("abc/efg/h*j/*.*")]
        [InlineData("abc/efg/h*j/*.*/")]
        [InlineData("abc/efg/hij")]
        [InlineData("abc/efg/hij/klm")]
        public void BuildRaggedPatternNegative(string sample)
        {
            var pattern = PatternBuilder.Build(sample) as IRaggedPattern;

            Assert.Null(pattern);
        }

        [Theory]
        [InlineData("a/../")]
        [InlineData("a/..")]
        [InlineData("/a/../")]
        [InlineData("./a/../")]
        [InlineData("**/../")]
        [InlineData("*.cs/../")]
        public void ThrowExceptionForInvalidParentsPath(string sample)
        {
            // parent segment is only allowed at the beginning of the pattern
            Assert.Throws<ArgumentException>(() => {
                var pattern = PatternBuilder.Build(sample);

                Assert.Null(pattern);
            });
        }

        [Fact]
        public void ThrowExceptionForNull()
        {
            Assert.Throws<ArgumentNullException>(() => {
                PatternBuilder.Build(null);
            });
        }
    }
}