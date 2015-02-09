// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.FileSystemGlobbing.Internal.PathSegments;
using Xunit;

namespace Microsoft.Framework.FileSystemGlobbing.Tests.PatternSegments
{
    public class WildcardPathSegmentTests
    {
        [Fact]
        public void DefaultConstructor()
        {
            var paramBegin = "begin";
            var paramContains = new List<string> { "1", "2", "three" };
            var paramEnd = "end";

            var segment = new WildcardPathSegment(paramBegin, paramContains, paramEnd);

            Assert.Equal(paramBegin, segment.BeginsWith);
            Assert.Equal<string>(paramContains, segment.Contains);
            Assert.Equal(paramEnd, segment.EndsWith);
        }

        [Theory]
        [MemberData("GetPositiveOrdinalIgnoreCaseDataSample")]
        public void PositiveOrdinalIgnoreCaseMatch(string testSample, object segment)
        {
            var wildcardPathSegment = (WildcardPathSegment)segment;
            Assert.True(
                wildcardPathSegment.Match(testSample, StringComparison.OrdinalIgnoreCase),
                string.Format("[TestSample: {0}] [Wildcard: {1}]", testSample, Serialize(wildcardPathSegment)));
        }

        [Theory]
        [MemberData("GetNegativeOrdinalCaseDataSample")]
        public void NegativeOrdinalIgnoreCaseMatch(string testSample, object segment)
        {
            var wildcardPathSegment = (WildcardPathSegment)segment;
            Assert.False(
                wildcardPathSegment.Match(testSample, StringComparison.OrdinalIgnoreCase),
                string.Format("[TestSample: {0}] [Wildcard: {1}]", testSample, Serialize(wildcardPathSegment)));
        }

        [Theory]
        [MemberData("GetPositiveOrdinalDataSample")]
        public void PositiveOrdinalMatch(string testSample, object segment)
        {
            var wildcardPathSegment = (WildcardPathSegment)segment;
            Assert.True(
                wildcardPathSegment.Match(testSample, StringComparison.Ordinal),
                string.Format("[TestSample: {0}] [Wildcard: {1}]", testSample, Serialize(wildcardPathSegment)));
        }

        [Theory]
        [MemberData("GetNegativeOrdinalDataSample")]
        public void NegativeOrdinalMatch(string testSample, object segment)
        {
            var wildcardPathSegment = (WildcardPathSegment)segment;
            Assert.False(
                wildcardPathSegment.Match(testSample, StringComparison.Ordinal),
                string.Format("[TestSample: {0}] [Wildcard: {1}]", testSample, Serialize(wildcardPathSegment)));
        }

        public static IEnumerable<object[]> GetPositiveOrdinalIgnoreCaseDataSample()
        {
            yield return WrapResult("abc", "a", "c");
            yield return WrapResult("abBb123c", "a", "c");
            yield return WrapResult("aaac", "a", "c");
            yield return WrapResult("acccc", "a", "c");
            yield return WrapResult("aacc", "a", "c");
            yield return WrapResult("aacc", "aa", "c");
            yield return WrapResult("acc", "ac", "c");
            yield return WrapResult("abcdefgh", "ab", "cd", "ef", "gh");
            yield return WrapResult("abCDEfgh", "ab", "cd", "ef", "gh");
            yield return WrapResult("ab123cd321ef123gh", "ab", "cd", "ef", "gh");
            yield return WrapResult("abcd321ef123gh", "ab", "cd", "ef", "gh");
            yield return WrapResult("ababcd321ef123gh", "ab", "cd", "ef", "gh");
        }

        public static IEnumerable<object[]> GetNegativeOrdinalCaseDataSample()
        {
            yield return WrapResult("aa", "a", "c");
            yield return WrapResult("cc", "a", "c");
            yield return WrapResult("ab", "a", "c");
            yield return WrapResult("ab", "a", "b", "c");
            yield return WrapResult("bc", "a", "b", "c");
            yield return WrapResult("ac", "a", "b", "c");
            yield return WrapResult("abc", "a", "b", "b", "c");
            yield return WrapResult("ab", "ab", "c");
            yield return WrapResult("ab", "abb", "c");
            yield return WrapResult("ac", "ac", "c");
        }

        public static IEnumerable<object[]> GetPositiveOrdinalDataSample()
        {
            yield return WrapResult("abc", "a", "c");
            yield return WrapResult("abBb123c", "a", "c");
            yield return WrapResult("aaac", "a", "c");
            yield return WrapResult("Aaac", "A", "c");
            yield return WrapResult("acccC", "a", "C");
            yield return WrapResult("aacc", "a", "c");
            yield return WrapResult("aAcc", "aA", "c");
            yield return WrapResult("acc", "ac", "c");
            yield return WrapResult("abcDefgh", "ab", "cD", "ef", "gh");
            yield return WrapResult("aB123cd321ef123gh", "aB", "cd", "ef", "gh");
            yield return WrapResult("abcd321ef123gh", "ab", "cd", "ef", "gh");
            yield return WrapResult("ababcdCD321ef123gh", "ab", "cd", "ef", "gh");
            yield return WrapResult("ababcdCD321ef123gh", "ab", "CD", "ef", "gh");
            yield return WrapResult("ababcd321eF123gh", "ab", "cd", "eF", "gh");
        }

        public static IEnumerable<object[]> GetNegativeOrdinalDataSample()
        {
            yield return WrapResult("aa", "a", "c");
            yield return WrapResult("abc", "A", "c");
            yield return WrapResult("cc", "a", "c");
            yield return WrapResult("ab", "a", "c");
            yield return WrapResult("ab", "a", "b", "c");
            yield return WrapResult("bc", "a", "b", "c");
            yield return WrapResult("ac", "a", "b", "c");
            yield return WrapResult("abc", "a", "b", "b", "c");
            yield return WrapResult("ab", "ab", "c");
            yield return WrapResult("ab", "abb", "c");
            yield return WrapResult("ac", "ac", "c");
            yield return WrapResult("abBb123C", "a", "c");
            yield return WrapResult("Aaac", "a", "c");
            yield return WrapResult("aAac", "A", "c");
            yield return WrapResult("aCc", "a", "C");
            yield return WrapResult("aacc", "aA", "c");
            yield return WrapResult("acc", "aC", "c");
            yield return WrapResult("abcDefgh", "ab", "cd", "ef", "gh");
            yield return WrapResult("aB123cd321ef123gh", "aB", "cd", "EF", "gh");
            yield return WrapResult("abcd321ef123gh", "ab", "cd", "efF", "gh");
            yield return WrapResult("ababcdCD321ef123gh", "AB", "cd", "ef", "gh");
            yield return WrapResult("ababcdCD321ef123gh", "ab", "CD", "EF", "gh");
        }

        private static object[] WrapResult(params string[] values)
        {
            if (values == null || values.Length < 3)
            {
                throw new InvalidOperationException("At least three values are required to create a data sample");
            }

            var beginWith = values[1];
            var endWith = values[values.Length - 1];
            var contains = values.Skip(2).Take(values.Length - 3);

            return new object[] { values[0], new WildcardPathSegment(beginWith, contains.ToList(), endWith) };
        }

        private static string Serialize(WildcardPathSegment segment)
        {
            return string.Format("{0}:{1}:{2}",
                segment.BeginsWith,
                string.Join(",", segment.Contains.ToArray()),
                segment.EndsWith);
        }
    }
}