// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Editor.Razor.Completion
{
    public class RazorDirectiveAttributeCompletionSourceTest : ForegroundDispatcherTestBase
    {
        [Fact]
        public void InitializeCompletion_EmptySnapshot_ReturnsDoesNotParticipate()
        {
            // Arrange
            var source = CreateCompletionSource();
            var emptySnapshot = new StringTextSnapshot(string.Empty);
            var trigger = new CompletionTrigger(CompletionTriggerReason.Invoke, emptySnapshot);
            var triggerLocation = new SnapshotPoint(emptySnapshot, 0);

            // Act
            var result = source.InitializeCompletion(trigger, triggerLocation, CancellationToken.None);

            // Assert
            Assert.Equal(CompletionStartData.DoesNotParticipateInCompletion, result);
        }

        [Fact]
        public void InitializeCompletion_TriggeredAtStartOfDocument_ReturnsDoesNotParticipate()
        {
            // Arrange
            var source = CreateCompletionSource();
            var snapshot = new StringTextSnapshot("<p class='foo'></p>");
            var trigger = new CompletionTrigger(CompletionTriggerReason.Invoke, snapshot);
            var triggerLocation = new SnapshotPoint(snapshot, 0);

            // Act
            var result = source.InitializeCompletion(trigger, triggerLocation, CancellationToken.None);

            // Assert
            Assert.Equal(CompletionStartData.DoesNotParticipateInCompletion, result);
        }

        [Fact]
        public void InitializeCompletion_TriggeredAtInvalidLocation_ReturnsDoesNotParticipate()
        {
            // Arrange
            var source = CreateCompletionSource();
            var snapshot = new StringTextSnapshot("<p class='foo'></p>");
            var trigger = new CompletionTrigger(CompletionTriggerReason.Invoke, snapshot);

            // Act & Assert
            for (var i = 0; i < snapshot.Length; i++)
            {
                var triggerLocation = new SnapshotPoint(snapshot, i);
                var result = source.InitializeCompletion(trigger, triggerLocation, CancellationToken.None);
                Assert.Equal(CompletionStartData.DoesNotParticipateInCompletion, result);
            }
        }

        [Fact]
        public void InitializeCompletion_TriggeredAtPossibleDirectiveAttribute_ReturnsParticipate()
        {
            // Arrange
            var source = CreateCompletionSource();
            var snapshot = new StringTextSnapshot("<input @bind='@foo' />");
            var trigger = new CompletionTrigger(CompletionTriggerReason.Invoke, snapshot);
            var triggerLocation = new SnapshotPoint(snapshot, 9);
            var expectedApplicableToSpan = new SnapshotSpan(snapshot, new Span(7, 5));

            // Act
            var result = source.InitializeCompletion(trigger, triggerLocation, CancellationToken.None);

            // Assert
            Assert.Equal(expectedApplicableToSpan, result.ApplicableToSpan);
        }

        [Fact]
        public void InitializeCompletion_TriggeredAtPossibleDirectiveWithAttributeParameter_ReturnsParticipate()
        {
            // Arrange
            var source = CreateCompletionSource();
            var snapshot = new StringTextSnapshot("<input @bind:format='@foo' />");
            var trigger = new CompletionTrigger(CompletionTriggerReason.Invoke, snapshot);
            var triggerLocation = new SnapshotPoint(snapshot, 9);
            var expectedApplicableToSpan = new SnapshotSpan(snapshot, new Span(7, 5));

            // Act
            var result = source.InitializeCompletion(trigger, triggerLocation, CancellationToken.None);

            // Assert
            Assert.Equal(expectedApplicableToSpan, result.ApplicableToSpan);
        }

        [Fact]
        public void InitializeCompletion_TriggeredAtPossibleDirectiveAttributeParameter_ReturnsParticipate()
        {
            // Arrange
            var source = CreateCompletionSource();
            var snapshot = new StringTextSnapshot("<input @bind:format='@foo' />");
            var trigger = new CompletionTrigger(CompletionTriggerReason.Invoke, snapshot);
            var triggerLocation = new SnapshotPoint(snapshot, 13);
            var expectedApplicableToSpan = new SnapshotSpan(snapshot, new Span(13, 6));

            // Act
            var result = source.InitializeCompletion(trigger, triggerLocation, CancellationToken.None);

            // Assert
            Assert.Equal(expectedApplicableToSpan, result.ApplicableToSpan);
        }

        private RazorDirectiveAttributeCompletionSource CreateCompletionSource()
        {
            var source = new RazorDirectiveAttributeCompletionSource(
                Dispatcher,
                Mock.Of<VisualStudioRazorParser>(),
                Mock.Of<RazorCompletionFactsService>(),
                Mock.Of<ICompletionBroker>());
            return source;
        }
    }
}
