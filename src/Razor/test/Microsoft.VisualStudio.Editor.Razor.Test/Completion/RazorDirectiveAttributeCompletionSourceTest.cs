// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Editor.Razor.Completion
{
    public class RazorDirectiveAttributeCompletionSourceTest : ForegroundDispatcherTestBase
    {
        [Fact]
        public async Task GetDescriptionAsync_NoDescriptionData_ReturnsEmptyString()
        {
            // Arrange
            var source = CreateCompletionSource();
            var completionSessionSource = Mock.Of<IAsyncCompletionSource>();
            var completionItem = new CompletionItem("@random", completionSessionSource);

            // Act
            var result = await source.GetDescriptionAsync(session: null, completionItem, CancellationToken.None);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task GetDescriptionAsync_DescriptionData_AsksFactoryForDescription()
        {
            // Arrange
            var expectedResult = new ContainerElement(ContainerElementStyle.Wrapped);
            var description = new AttributeCompletionDescription(Array.Empty<AttributeDescriptionInfo>());
            var descriptionFactory = Mock.Of<VisualStudioDescriptionFactory>(factory => factory.CreateClassifiedDescription(description) == expectedResult);
            var source = new RazorDirectiveAttributeCompletionSource(
                Dispatcher,
                Mock.Of<VisualStudioRazorParser>(),
                Mock.Of<RazorCompletionFactsService>(),
                Mock.Of<ICompletionBroker>(),
                descriptionFactory);
            var completionSessionSource = Mock.Of<IAsyncCompletionSource>();
            var completionItem = new CompletionItem("@random", completionSessionSource);
            completionItem.Properties.AddProperty(RazorDirectiveAttributeCompletionSource.DescriptionKey, description);

            // Act
            var result = await source.GetDescriptionAsync(session: null, completionItem, CancellationToken.None);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void InitializeCompletion_PageDirective_ReturnsDoesNotParticipate()
        {
            // Arrange
            var source = CreateCompletionSource();
            var snapshot = new StringTextSnapshot("@page");
            var trigger = new CompletionTrigger(CompletionTriggerReason.Invoke, snapshot);
            var triggerLocation = new SnapshotPoint(snapshot, 1);
            var expectedApplicableToSpan = new SnapshotSpan(snapshot, new Span(0, 5));

            // Act
            var result = source.InitializeCompletion(trigger, triggerLocation, CancellationToken.None);

            // Assert
            Assert.Equal(expectedApplicableToSpan, result.ApplicableToSpan);
        }

        [Fact]
        public void InitializeCompletion_SingleTransition_ReturnsDoesNotParticipate()
        {
            // Arrange
            var source = CreateCompletionSource();
            var snapshot = new StringTextSnapshot("@");
            var trigger = new CompletionTrigger(CompletionTriggerReason.Invoke, snapshot);
            var triggerLocation = new SnapshotPoint(snapshot, 1);

            // Act
            var result = source.InitializeCompletion(trigger, triggerLocation, CancellationToken.None);

            // Assert
            Assert.Equal(CompletionStartData.DoesNotParticipateInCompletion, result);
        }

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
                Mock.Of<ICompletionBroker>(),
                Mock.Of<VisualStudioDescriptionFactory>());
            return source;
        }
    }
}
