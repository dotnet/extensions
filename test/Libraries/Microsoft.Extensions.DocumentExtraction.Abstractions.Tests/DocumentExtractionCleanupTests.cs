// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DocumentExtraction;
using Microsoft.Extensions.Documents;
using Xunit;

namespace Microsoft.Extensions.DocumentExtraction.Tests;

public sealed class DocumentExtractionCleanupTests
{
    [Fact]
    public void Dispose_ThrowingArtifactStillDisposesOtherPendingArtifacts()
    {
        IOException failure = new("synthetic cleanup failure");
        CleanupStream throwing = new(failure);
        CleanupStream later = new();
        using DocumentArtifact first = new("image/png", throwing);
        using DocumentArtifact second = new("image/png", later);
        using DocumentExtractionDocumentBuilder builder = new("cleanup");
        builder.AddPage(new DocumentExtractionPage(1,
            [new DocumentExtractionImage("image", 0, "", artifacts: [first, second])]));

        AggregateException error = Assert.Throws<AggregateException>(() => builder.Dispose());
        Assert.Same(failure, Assert.Single(error.InnerExceptions));
        Assert.Equal(1, throwing.DisposeCount);
        Assert.Equal(1, later.DisposeCount);
        Assert.False(throwing.CanRead);
        Assert.False(later.CanRead);
        builder.Dispose();
        Assert.Equal(1, throwing.DisposeCount);
        Assert.Equal(1, later.DisposeCount);
    }

    [Fact]
    public void Build_FailurePreservesValidationAndAllCleanupFailures()
    {
        IOException ownedFailure = new("owned cleanup");
        IOException pendingFailure = new("pending cleanup");
        CleanupStream ownedThrowing = new(ownedFailure);
        CleanupStream ownedLater = new();
        CleanupStream pendingThrowing = new(pendingFailure);
        CleanupStream pendingLater = new();
        CleanupStream foreign = new();
        using DocumentArtifact ownedFirst = new("application/json", ownedThrowing);
        using DocumentArtifact ownedSecond = new("application/json", ownedLater);
        using DocumentArtifact pendingFirst = new("image/png", pendingThrowing);
        using DocumentArtifact pendingSecond = new("image/png", pendingLater);
        using DocumentArtifact foreignArtifact = new("image/png", foreign);
        using Document owner = new("other-owner");
        DocumentSection foreignSection = new("");
        DocumentImageElement foreignImage = new("");
        foreignImage.Artifacts.Add(foreignArtifact);
        foreignSection.Elements.Add(foreignImage);
        owner.Sections.Add(foreignSection);
        using DocumentExtractionDocumentBuilder builder = new("failure", true);
        builder.AddPage(new DocumentExtractionPage(1,
            [new DocumentExtractionText("duplicate", 0, DocumentTextKind.Paragraph, "one")],
            sourceOutputs:
            [
                new DocumentSourceOutput("owned-first", rawContent: ownedFirst),
                new DocumentSourceOutput("owned-second", rawContent: ownedSecond)
            ]));
        builder.AddPage(new DocumentExtractionPage(2,
            [new DocumentExtractionText("duplicate", 0, DocumentTextKind.Paragraph, "two")]));
        builder.AddPage(new DocumentExtractionPage(3,
            [new DocumentExtractionImage("pending", 0, "", artifacts: [pendingFirst, pendingSecond, foreignArtifact])]));

        AggregateException error = Assert.Throws<AggregateException>(() => builder.Build());
        ArgumentException original = Assert.IsType<ArgumentException>(error.InnerExceptions[0]);
        Assert.Contains("duplicate", original.Message);
        Assert.Equal(3, error.Flatten().InnerExceptions.Count);
        Assert.Contains(ownedFailure, error.Flatten().InnerExceptions);
        Assert.Contains(pendingFailure, error.Flatten().InnerExceptions);
        Assert.Equal(1, ownedThrowing.DisposeCount);
        Assert.Equal(1, ownedLater.DisposeCount);
        Assert.Equal(1, pendingThrowing.DisposeCount);
        Assert.Equal(1, pendingLater.DisposeCount);
        Assert.Equal(0, foreign.DisposeCount);
        Assert.True(foreign.CanRead);
        Assert.Same(foreignArtifact, Assert.Single(foreignImage.Artifacts));
        Assert.False(ownedThrowing.CanRead);
        Assert.False(pendingThrowing.CanRead);
        builder.Dispose();
        Assert.Equal(1, pendingLater.DisposeCount);
        owner.Dispose();
        Assert.Equal(1, foreign.DisposeCount);
        Assert.Equal(new Exception[] { original, pendingFailure, ownedFailure }, error.Flatten().InnerExceptions);
    }

    [Fact]
    public void Build_FailureWithoutCleanupErrorsPreservesOriginalException()
    {
        CleanupStream stream = new();
        using DocumentArtifact artifact = new("image/png", stream);
        using DocumentExtractionDocumentBuilder builder = new("duplicate");
        builder.AddPage(new DocumentExtractionPage(1,
            [new DocumentExtractionImage("image", 0, "", artifacts: [artifact])]));
        builder.AddPage(new DocumentExtractionPage(1, []));

        ArgumentException error = Assert.Throws<ArgumentException>(() => builder.Build());
        Assert.Contains("Page number 1 is duplicated.", error.Message);
        Assert.Null(error.InnerException);
        Assert.Equal(1, stream.DisposeCount);
    }

    private sealed class CleanupStream(Exception? failure = null) : MemoryStream
    {
        internal int DisposeCount { get; private set; }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeCount++;
            }

            base.Dispose(disposing);
            if (disposing && failure is not null)
            {
                throw failure;
            }
        }
    }

    [Fact]
    public void Dispose_MultipleCleanupFailures_AttemptsEveryOwnedResourceOnce()
    {
        IOException firstFailure = new("first");
        IOException lastFailure = new("last");
        CleanupStream first = new(firstFailure);
        CleanupStream middle = new();
        CleanupStream last = new(lastFailure);
        using DocumentArtifact a = new("application/json", first);
        using DocumentArtifact b = new("application/json", middle);
        using DocumentArtifact c = new("application/json", last);
        using DocumentExtractionDocumentBuilder builder = new("abandoned", true);
        builder.AddPage(new DocumentExtractionPage(7, [], sourceOutputs:
            [new("a", rawContent: a), new("b", rawContent: b), new("c", rawContent: c)]));

        AggregateException error = Assert.Throws<AggregateException>(() => builder.Dispose());
        Assert.Equal(new Exception[] { firstFailure, lastFailure }, error.InnerExceptions);
        builder.Dispose();
        Assert.All(new[] { first, middle, last }, static stream => Assert.Equal(1, stream.DisposeCount));
        Assert.All(new[] { first, middle, last }, static stream => Assert.False(stream.CanRead));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Finalization_EnumerationFailureAfterBodyAcquisition_PreservesPrimaryAndCleansBodies(bool asynchronous)
    {
        InvalidOperationException primary = new("provider failed");
        using CleanupFixture fixture = new();
        PageSequence sequence = new(fixture.Page, primary);
        Exception error = await FinalizeFailure(sequence, asynchronous);
        Assert.Same(primary, error);
        Assert.Equal(1, sequence.DisposeCount);
        fixture.AssertCleaned();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Finalization_EnumerationFailureWithThrowingCleanup_KeepsPrimaryErrorFirst(bool asynchronous)
    {
        InvalidOperationException primary = new("provider failed");
        IOException first = new("first body cleanup");
        IOException second = new("second body cleanup");
        using CleanupFixture fixture = new(first, second);
        PageSequence sequence = new(fixture.Page, primary);
        Exception error = await FinalizeFailure(sequence, asynchronous);
        Assert.Equal(1, sequence.DisposeCount);
        fixture.AssertCleaned();
        Assert.Equal(new Exception[] { primary, first, second }, Assert.IsType<AggregateException>(error).Flatten().InnerExceptions);
    }

    [Fact]
    public async Task ToDocumentExtractionResultAsync_PreCanceledToken_DoesNotAcquireBodies()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        using CleanupFixture fixture = new();
        PageSequence sequence = new(fixture.Page);
        OperationCanceledException error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            sequence.ToDocumentExtractionResultAsync("document", true, cancellation.Token));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(0, sequence.AcquireCount);
        Assert.Equal(0, sequence.DisposeCount);
        Assert.All(fixture.Streams, static stream => Assert.Equal(0, stream.DisposeCount));
        Assert.All(fixture.Streams, static stream => Assert.True(stream.CanRead));
        Assert.Equal(0, fixture.Native.DisposeCount);
        Assert.Equal(42, fixture.Input.ReadByte());
    }

    [Fact]
    public Task ToDocumentExtractionResultAsync_CancellationDuringYield_CleansAcquiredBodies() =>
        VerifyCancellation(afterLast: false, throwingCleanup: false);

    [Fact]
    public Task ToDocumentExtractionResultAsync_CancellationAfterLastYield_PreventsResultTransfer() =>
        VerifyCancellation(afterLast: true, throwingCleanup: false);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task ToDocumentExtractionResultAsync_UncooperativeIterator_StillHonorsCancellation(bool afterLast) =>
        VerifyCancellation(afterLast, throwingCleanup: false);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task ToDocumentExtractionResultAsync_CancellationWithThrowingCleanup_PreservesPrimaryTokenAndAttemptsAllCleanup(bool afterLast) =>
        VerifyCancellation(afterLast, throwingCleanup: true);

    private static async Task VerifyCancellation(bool afterLast, bool throwingCleanup)
    {
        using CancellationTokenSource cancellation = new();
        IOException first = new("first cleanup");
        IOException second = new("second cleanup");
        using CleanupFixture fixture = new(throwingCleanup ? first : null, throwingCleanup ? second : null);
        using SemaphoreSlim reached = new(0, 1);
        using SemaphoreSlim release = new(0, 1);
        PageSequence sequence = new(fixture.Page)
        {
            BeforeMoveAsync = async index =>
            {
                if (index == (afterLast ? 2 : 1))
                {
                    reached.Release();
                    await release.WaitAsync();
                }
            }
        };
        Task<Exception> pending = Assert.ThrowsAnyAsync<Exception>(() => sequence.ToDocumentExtractionResultAsync("document", true, cancellation.Token));
        await reached.WaitAsync();
        Assert.False(pending.IsCompleted);
        cancellation.Cancel();
        release.Release();
        Exception error = await pending;

        Assert.Equal(cancellation.Token, sequence.ReceivedToken);
        Assert.Equal(1, sequence.DisposeCount);
        fixture.AssertCleaned();
        IReadOnlyList<Exception> errors = error is AggregateException aggregate ? aggregate.Flatten().InnerExceptions : [error];
        OperationCanceledException primary = Assert.IsAssignableFrom<OperationCanceledException>(errors[0]);
        Assert.Equal(cancellation.Token, primary.CancellationToken);
        Assert.Equal(throwingCleanup ? 3 : 1, errors.Count);
        if (throwingCleanup)
        {
            Assert.Same(first, errors[1]);
            Assert.Same(second, errors[2]);
        }
    }

    private static async Task<Exception> FinalizeFailure(PageSequence sequence, bool asynchronous)
    {
        if (asynchronous)
        {
            return await Assert.ThrowsAnyAsync<Exception>(() => sequence.ToDocumentExtractionResultAsync("document", true));
        }

        return Assert.ThrowsAny<Exception>(() => sequence.ToDocumentExtractionResult("document", true));
    }

    private sealed class CleanupNative : IDisposable
    {
        internal int DisposeCount { get; private set; }
        public void Dispose() => DisposeCount++;
        public override string ToString() => throw new InvalidOperationException("Native must never be inspected.");
    }

    private sealed class CleanupFixture : IDisposable
    {
        internal CleanupFixture(Exception? first = null, Exception? second = null)
        {
            First = new(first);
            Second = new(second);
            Third = new();
            Streams = [First, Second, Third];
            Artifacts = Streams.Select(static stream => new DocumentArtifact("application/json", stream)).ToArray();
            Page = new(7, [], sourceOutputs: Artifacts.Select((artifact, index) => new DocumentSourceOutput("output:" + index, Native, artifact)).ToArray());
        }

        internal MemoryStream Input { get; } = new([42, 43]);
        internal CleanupNative Native { get; } = new();
        internal CleanupStream[] Streams { get; }
        private CleanupStream First { get; }
        private CleanupStream Second { get; }
        private CleanupStream Third { get; }
        internal DocumentArtifact[] Artifacts { get; }
        internal DocumentExtractionPage Page { get; }

        internal void AssertCleaned()
        {
            Assert.All(Streams, static stream => Assert.Equal(1, stream.DisposeCount));
            Assert.All(Streams, static stream => Assert.False(stream.CanRead));
            Assert.Equal(0, Native.DisposeCount);
            Assert.Equal(42, Input.ReadByte());
        }

        public void Dispose()
        {
            foreach (DocumentArtifact artifact in Artifacts)
            {
                artifact.Dispose();
            }

            Input.Dispose();
            Native.Dispose();
        }
    }

    private sealed class PageSequence(DocumentExtractionPage page, Exception? failure = null)
        : IEnumerable<DocumentExtractionPage>, IAsyncEnumerable<DocumentExtractionPage>
    {
        internal int AcquireCount { get; private set; }
        internal int DisposeCount { get; private set; }
        internal CancellationToken ReceivedToken { get; private set; }
        internal Func<int, Task>? BeforeMoveAsync { get; set; }
        private DocumentExtractionPage Page => page;

        private bool Move(int index)
        {
            if (index == 1)
            {
                return true;
            }

            if (failure is { } error)
            {
                throw error;
            }

            return false;
        }

        public IEnumerator<DocumentExtractionPage> GetEnumerator()
        {
            AcquireCount++;
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IAsyncEnumerator<DocumentExtractionPage> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            ReceivedToken = cancellationToken;
            AcquireCount++;
            return new Enumerator(this);
        }

        private sealed class Enumerator(PageSequence owner) : IEnumerator<DocumentExtractionPage>, IAsyncEnumerator<DocumentExtractionPage>
        {
            private int _index;
            public DocumentExtractionPage Current => owner.Page;
            object IEnumerator.Current => Current;

            public bool MoveNext() => owner.Move(++_index);

            public async ValueTask<bool> MoveNextAsync()
            {
                ++_index;
                if (owner.BeforeMoveAsync is { } before)
                {
                    await before(_index);
                }

                // Intentionally ignores ReceivedToken: the finalizer must enforce cancellation itself.
                return owner.Move(_index);
            }

            public void Reset() => throw new NotSupportedException();
            public void Dispose() => owner.DisposeCount++;
            public ValueTask DisposeAsync()
            {
                Dispose();
                return default;
            }
        }
    }
}
