// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PathSegments;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PatternContexts;
using Microsoft.Extensions.FileSystemGlobbing.Internal.Patterns;
using Microsoft.Extensions.FileSystemGlobbing.Tests.TestUtility;
using Xunit;

namespace Microsoft.Extensions.FileSystemGlobbing.Tests.PatternContexts
{
    public class PatternContextRaggedIncludeTests
    {
        [Fact]
        public void PredictBeforeEnterDirectoryShouldThrow()
        {
            var builder = new PatternBuilder();
            var pattern = builder.Build("**") as IRaggedPattern;
            var context = new PatternContextRaggedInclude(pattern);

            Assert.Throws<InvalidOperationException>(() =>
            {
                context.Declare((segment, last) =>
                {
                    Assert.False(true, "No segment should be declared.");
                });
            });
        }

        [Theory]
        [InlineData("/a/b/**/c/d", new string[] { "root" }, "a", false)]
        [InlineData("/a/b/**/c/d", new string[] { "root", "a" }, "b", false)]
        [InlineData("/a/b/**/c/d", new string[] { "root", "a", "b" }, null, false)]
        [InlineData("/a/b/**/c/d", new string[] { "root", "a", "b", "whatever" }, null, false)]
        [InlineData("/a/b/**/c/d", new string[] { "root", "a", "b", "whatever", "anything" }, null, false)]
        public void PredictReturnsCorrectResult(string patternString, string[] pushDirectory, string expectSegment, bool expectWildcard)
        {
            var builder = new PatternBuilder();
            var pattern = builder.Build(patternString) as IRaggedPattern;
            Assert.NotNull(pattern);

            var context = new PatternContextRaggedInclude(pattern);
            PatternContextHelper.PushDirectory(context, pushDirectory);

            context.Declare((segment, last) =>
            {
                if (expectSegment != null)
                {
                    var mockSegment = segment as LiteralPathSegment;

                    Assert.NotNull(mockSegment);
                    Assert.Equal(false, last);
                    Assert.Equal(expectSegment, mockSegment.Value);
                }
                else
                {
                    Assert.Equal(Microsoft.Extensions.FileSystemGlobbing.Internal.PathSegments.WildcardPathSegment.MatchAll, segment);
                }
            });
        }

        [Theory]
        [InlineData("/a/b/**/c/d", new string[] { "root", "b" })]
        [InlineData("/a/b/**/c/d", new string[] { "root", "a", "c" })]
        public void PredictNotCallBackWhenEnterUnmatchDirectory(string patternString, string[] pushDirectory)
        {
            var builder = new PatternBuilder();
            var pattern = builder.Build(patternString) as IRaggedPattern;
            var context = new PatternContextRaggedInclude(pattern);
            PatternContextHelper.PushDirectory(context, pushDirectory);

            context.Declare((segment, last) =>
            {
                Assert.False(true, "No segment should be declared.");
            });
        }
    }
}