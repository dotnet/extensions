// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Extensions.AI.Evaluation.Reporting.Utilities;
using Xunit;

namespace Microsoft.Extensions.AI.Evaluation.Reporting.Tests;

public class PathValidationTests
{
    // ──────────────────────────────────────────────
    //  ValidatePathSegment – valid inputs
    // ──────────────────────────────────────────────

    [Fact]
    public void ValidatePathSegment_Null_DoesNotThrow()
    {
        PathValidation.ValidatePathSegment(null, "param");
    }

    [Theory]
    [InlineData("simple")]
    [InlineData("My Scenario")]
    [InlineData("run-2024-01-01")]
    [InlineData("iteration_0")]
    [InlineData("a")]
    [InlineData("...")]
    [InlineData("..x")]
    [InlineData("x..")]
    public void ValidatePathSegment_ValidNames_DoesNotThrow(string segment)
    {
        PathValidation.ValidatePathSegment(segment, "param");
    }

    // ──────────────────────────────────────────────
    //  ValidatePathSegment – invalid inputs
    // ──────────────────────────────────────────────

    [Fact]
    public void ValidatePathSegment_EmptyString_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            PathValidation.ValidatePathSegment("", "param"));
    }

    [Theory]
    [InlineData("..")]
    [InlineData(".")]
    public void ValidatePathSegment_TraversalLiterals_Throws(string segment)
    {
        Assert.Throws<ArgumentException>(() =>
            PathValidation.ValidatePathSegment(segment, "param"));
    }

    [Theory]
    [InlineData("foo/bar")]
    [InlineData("foo\\bar")]
    [InlineData("../secret")]
    [InlineData("..\\secret")]
    public void ValidatePathSegment_ContainsPathSeparators_Throws(string segment)
    {
        Assert.Throws<ArgumentException>(() =>
            PathValidation.ValidatePathSegment(segment, "param"));
    }

    [Theory]
    [InlineData(" leading")]
    [InlineData("trailing ")]
    [InlineData(" both ")]
    public void ValidatePathSegment_WhitespacePadded_Throws(string segment)
    {
        Assert.Throws<ArgumentException>(() =>
            PathValidation.ValidatePathSegment(segment, "param"));
    }

    [Fact]
    public void ValidatePathSegment_NullCharacter_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            PathValidation.ValidatePathSegment("foo\0bar", "param"));
    }

    // ──────────────────────────────────────────────
    //  EnsureWithinRoot – paths inside root
    // ──────────────────────────────────────────────

    [Fact]
    public void EnsureWithinRoot_ChildPath_ReturnsResolvedPath()
    {
        string root = Path.Combine(Path.GetTempPath(), "testroot");
        string child = Path.Combine(root, "sub", "file.txt");

        string result = PathValidation.EnsureWithinRoot(root, child);

        Assert.Equal(Path.GetFullPath(child), result);
    }

    [Fact]
    public void EnsureWithinRoot_DeeplyNested_ReturnsResolvedPath()
    {
        string root = Path.Combine(Path.GetTempPath(), "testroot");
        string child = Path.Combine(root, "a", "b", "c", "d.json");

        string result = PathValidation.EnsureWithinRoot(root, child);

        Assert.Equal(Path.GetFullPath(child), result);
    }

    [Fact]
    public void EnsureWithinRoot_RootWithTrailingSeparator_Works()
    {
        string root = Path.Combine(Path.GetTempPath(), "testroot") + Path.DirectorySeparatorChar;
        string child = Path.Combine(root, "file.txt");

        string result = PathValidation.EnsureWithinRoot(root, child);

        Assert.Equal(Path.GetFullPath(child), result);
    }

    // ──────────────────────────────────────────────
    //  EnsureWithinRoot – paths escaping root
    // ──────────────────────────────────────────────

    [Fact]
    public void EnsureWithinRoot_DotDotEscapes_Throws()
    {
        string root = Path.Combine(Path.GetTempPath(), "testroot");
        string escaped = Path.Combine(root, "..", "outside");

        Assert.Throws<InvalidOperationException>(() =>
            PathValidation.EnsureWithinRoot(root, escaped));
    }

    [Fact]
    public void EnsureWithinRoot_MultipleDotDots_Throws()
    {
        string root = Path.Combine(Path.GetTempPath(), "testroot", "nested");
        string escaped = Path.Combine(root, "..", "..", "outside");

        Assert.Throws<InvalidOperationException>(() =>
            PathValidation.EnsureWithinRoot(root, escaped));
    }

    [Fact]
    public void EnsureWithinRoot_CompletelyDifferentPath_Throws()
    {
        string root = Path.Combine(Path.GetTempPath(), "testroot");
        string other = Path.Combine(Path.GetTempPath(), "other", "file.txt");

        Assert.Throws<InvalidOperationException>(() =>
            PathValidation.EnsureWithinRoot(root, other));
    }

    [Fact]
    public void EnsureWithinRoot_SiblingWithPrefix_Throws()
    {
        // Verifies that "testroot-sibling" is NOT treated as being inside "testroot".
        string root = Path.Combine(Path.GetTempPath(), "testroot");
        string sibling = Path.Combine(Path.GetTempPath(), "testroot-sibling", "file.txt");

        Assert.Throws<InvalidOperationException>(() =>
            PathValidation.EnsureWithinRoot(root, sibling));
    }

    [Fact]
    public void EnsureWithinRoot_PathEqualsRoot_DoesNotThrow()
    {
        string root = Path.Combine(Path.GetTempPath(), "testroot");

        string result = PathValidation.EnsureWithinRoot(root, root);

        Assert.Equal(Path.GetFullPath(root), result);
    }

    // ──────────────────────────────────────────────
    //  Integration: DiskBasedResultStore rejects traversal
    // ──────────────────────────────────────────────

    [Fact]
    public async void DiskBasedResultStore_DeleteWithTraversal_Throws()
    {
        string storagePath = Path.Combine(Path.GetTempPath(), "M.E.AI.Eval.PathTests", Path.GetRandomFileName());

        try
        {
            Directory.CreateDirectory(storagePath);
            var store = new Storage.DiskBasedResultStore(storagePath);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                store.DeleteResultsAsync(executionName: "..", scenarioName: "../sentinel").AsTask());
        }
        finally
        {
            try { Directory.Delete(storagePath, true); }
            catch { /* best effort */ }
        }
    }

    [Fact]
    public async void DiskBasedResponseCacheProvider_TraversalInScenarioName_Throws()
    {
        string storagePath = Path.Combine(Path.GetTempPath(), "M.E.AI.Eval.PathTests", Path.GetRandomFileName());

        try
        {
            Directory.CreateDirectory(storagePath);
            var provider = new Storage.DiskBasedResponseCacheProvider(storagePath);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                provider.GetCacheAsync("..", "..").AsTask());
        }
        finally
        {
            try { Directory.Delete(storagePath, true); }
            catch { /* best effort */ }
        }
    }
}
