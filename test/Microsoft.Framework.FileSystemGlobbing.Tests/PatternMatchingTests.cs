// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.FileSystemGlobbing.Tests.Infrastructure;
using Xunit;

namespace Microsoft.Framework.FileSystemGlobbing.Tests
{
    public class PatternMatchingTests
    {
        [Fact]
        public void EmptyCollectionWhenNoFilesPresent()
        {
            var scenario = new Scenario(@"c:\files\")
                .Include("alpha.txt")
                .Execute();

            scenario.AssertExact();
        }

        [Fact]
        public void MatchingFileIsFound()
        {
            var scenario = new Scenario(@"c:\files\")
                .Include("alpha.txt")
                .Files("alpha.txt")
                .Execute();

            scenario.AssertExact("alpha.txt");
        }

        [Fact]
        public void MismatchedFileIsIgnored()
        {
            var scenario = new Scenario(@"c:\files\")
                .Include("alpha.txt")
                .Files("omega.txt")
                .Execute();

            scenario.AssertExact();
        }

        [Fact]
        public void FolderNamesAreTraversed()
        {
            var scenario = new Scenario(@"c:\files\")
                .Include("beta/alpha.txt")
                .Files("beta/alpha.txt")
                .Execute();

            scenario.AssertExact("beta/alpha.txt");
        }

        [Theory]
        [InlineData(@"beta/alpha.txt", @"beta/alpha.txt")]
        [InlineData(@"beta\alpha.txt", @"beta/alpha.txt")]
        [InlineData(@"beta/alpha.txt", @"beta\alpha.txt")]
        [InlineData(@"beta\alpha.txt", @"beta\alpha.txt")]
        public void SlashPolarityIsIgnored(string includePattern, string filePath)
        {
            var scenario = new Scenario(@"c:\files\")
                .Include(includePattern)
                .Files("one/two.txt", filePath, "three/four.txt")
                .Execute();

            scenario.AssertExact("beta/alpha.txt");
        }

        [Theory]
        [InlineData(@"*.txt", new[] { "alpha.txt", "beta.txt" })]
        [InlineData(@"alpha.*", new[] { "alpha.txt" })]
        [InlineData(@"*.*", new[] { "alpha.txt", "beta.txt", "gamma.dat" })]
        [InlineData(@"*", new[] { "alpha.txt", "beta.txt", "gamma.dat" })]
        [InlineData(@"*et*", new[] { "beta.txt" })]
        [InlineData(@"b*et*t", new[] { "beta.txt" })]
        [InlineData(@"b*et*x", new string[0])]
        public void PatternMatchingWorks(string includePattern, string[] matchesExpected)
        {
            var scenario = new Scenario(@"c:\files\")
                .Include(includePattern)
                .Files("alpha.txt", "beta.txt", "gamma.dat")
                .Execute();

            scenario.AssertExact(matchesExpected);
        }

        [Theory]
        [InlineData(@"1234*5678", new[] { "12345678" })]
        [InlineData(@"12345*5678", new string[0])]
        [InlineData(@"12*3456*78", new[] { "12345678" })]
        [InlineData(@"12*23*", new string[0])]
        [InlineData(@"*67*78", new string[0])]
        [InlineData(@"*45*56", new string[0])]
        public void PatternBeginAndEndCantOverlap(string includePattern, string[] matchesExpected)
        {
            var scenario = new Scenario(@"c:\files\")
                .Include(includePattern)
                .Files("12345678")
                .Execute();

            scenario.AssertExact(matchesExpected);
        }


        [Theory]
        [InlineData(@"*mm*/*", new[] { "gamma/hello.txt" })]
        [InlineData(@"*alpha*/*", new[] { "alpha/hello.txt" })]
        [InlineData(@"*/*", new[] { "alpha/hello.txt", "beta/hello.txt", "gamma/hello.txt" })]
        [InlineData(@"*.*/*", new[] { "alpha/hello.txt", "beta/hello.txt", "gamma/hello.txt" })]
        public void PatternMatchingWorksInFolders(string includePattern, string[] matchesExpected)
        {
            var scenario = new Scenario(@"c:\files\")
                .Include(includePattern)
                .Files("alpha/hello.txt", "beta/hello.txt", "gamma/hello.txt")
                .Execute();

            scenario.AssertExact(matchesExpected);
        }

        [Fact]
        public void StarDotStarIsSameAsStar()
        {
            var scenario = new Scenario(@"c:\files\")
                .Include("*.*")
                .Files("alpha.txt", "alpha.", ".txt", ".", "alpha", "txt")
                .Execute();

            scenario.AssertExact("alpha.txt", "alpha.", ".txt", ".", "alpha", "txt");
        }

        [Fact]
        public void IncompletePatternsDoNotInclude()
        {
            var scenario = new Scenario(@"c:\files\")
                .Include("*/*.txt")
                .Files("one/x.txt", "two/x.txt", "x.txt")
                .Execute();

            scenario.AssertExact("one/x.txt", "two/x.txt");
        }

        [Fact]
        public void IncompletePatternsDoNotExclude()
        {
            var scenario = new Scenario(@"c:\files\")
                .Include("*/*.txt")
                .Exclude("one/hello.txt")
                .Files("one/x.txt", "two/x.txt")
                .Execute();

            scenario.AssertExact("one/x.txt", "two/x.txt");
        }

        [Fact]
        public void TrailingRecursiveWildcardMatchesAllFiles()
        {
            var scenario = new Scenario(@"c:\files\")
                .Include("one/**")
                .Files("one/x.txt", "two/x.txt", "one/x/y.txt")
                .Execute();

            scenario.AssertExact("one/x.txt", "one/x/y.txt");
        }

        [Fact]
        public void LeadingRecursiveWildcardMatchesAllLeadingPaths()
        {
            var scenario = new Scenario(@"c:\files\")
                .Include("**/*.cs")
                .Files("one/x.cs", "two/x.cs", "one/two/x.cs", "x.cs")
                .Files("one/x.txt", "two/x.txt", "one/two/x.txt", "x.txt")
                .Execute();

            scenario.AssertExact("one/x.cs", "two/x.cs", "one/two/x.cs", "x.cs");
        }

        [Fact]
        public void InnerRecursiveWildcardMuseStartWithAndEndWith()
        {
            var scenario = new Scenario(@"c:\files\")
                .Include("one/**/*.cs")
                .Files("one/x.cs", "two/x.cs", "one/two/x.cs", "x.cs")
                .Files("one/x.txt", "two/x.txt", "one/two/x.txt", "x.txt")
                .Execute();

            scenario.AssertExact("one/x.cs", "one/two/x.cs");
        }


        [Fact]
        public void ExcludeMayEndInDirectoryName()
        {
            var scenario = new Scenario(@"c:\files\")
                .Include("*.cs", "*/*.cs", "*/*/*.cs")
                .Exclude("bin", "one/two")
                .Files("one/x.cs", "two/x.cs", "one/two/x.cs", "x.cs", "bin/x.cs", "bin/two/x.cs")
                .Execute();

            scenario.AssertExact("one/x.cs", "two/x.cs", "x.cs");
        }


        [Fact]
        public void RecursiveWildcardSurroundingContainsWith()
        {
            var scenario = new Scenario(@"c:\files\")
                .Include("**/x/**")
                .Files("x/1", "1/x/2", "1/x", "x", "1", "1/2")
                .Execute();

            scenario.AssertExact("x/1", "1/x/2");
        }


        [Fact]
        public void SequentialFoldersMayBeRequired()
        {
            var scenario = new Scenario(@"c:\files\")
                .Include("a/b/**/1/2/**/2/3/**")
                .Files("1/2/2/3/x", "1/2/3/y", "a/1/2/4/2/3/b", "a/2/3/1/2/b")
                .Files("a/b/1/2/2/3/x", "a/b/1/2/3/y", "a/b/a/1/2/4/2/3/b", "a/b/a/2/3/1/2/b")
                .Execute();

            scenario.AssertExact("a/b/1/2/2/3/x", "a/b/a/1/2/4/2/3/b");
        }

        [Fact]
        public void RecursiveAloneIncludesEverything()
        {
            var scenario = new Scenario(@"c:\files\")
                .Include("**")
                .Files("1/2/2/3/x", "1/2/3/y")
                .Execute();

            scenario.AssertExact("1/2/2/3/x", "1/2/3/y");
        }

        [Fact]
        public void ExcludeCanHaveSurroundingRecursiveWildcards()
        {
            var scenario = new Scenario(@"c:\files\")
                .Include("**")
                .Exclude("**/x/**")
                .Files("x/1", "1/x/2", "1/x", "x", "1", "1/2")
                .Execute();

            scenario.AssertExact("1/x", "x", "1", "1/2");
        }

        // exclude: **/.*/**
        // exclude: node_modules/*
        // exclude: **/.cs
    }
}
