// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServerClient.Razor.Debugging;
using Microsoft.VisualStudio.LanguageServerClient.Razor.Dialogs;
using Microsoft.VisualStudio.Test;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Moq;
using Xunit;
using Position = Microsoft.VisualStudio.LanguageServer.Protocol.Position;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    public class RazorLanguageService_IVsLanguageDebugInfoTest
    {
        private TextSpan[] TextSpans { get; } = new[]
        {
            new TextSpan()
        };

        [Fact]
        public void ValidateBreakpointLocation_CanNotGetBackingTextBuffer_ReturnsNotImpl()
        {
            // Arrange
            var languageService = CreateLanguageServiceWith(editorAdaptersFactory: Mock.Of<IVsEditorAdaptersFactoryService>());

            // Act
            var result = languageService.ValidateBreakpointLocation(Mock.Of<IVsTextBuffer>(), 0, 0, TextSpans);

            // Assert
            Assert.Equal(VSConstants.E_NOTIMPL, result);
        }

        [Fact]
        public void ValidateBreakpointLocation_InvalidLocation_ReturnsEFail()
        {
            // Arrange
            var languageService = CreateLanguageServiceWith();

            // Act
            var result = languageService.ValidateBreakpointLocation(Mock.Of<IVsTextBuffer>(), int.MaxValue, 0, TextSpans);

            // Assert
            Assert.Equal(VSConstants.E_FAIL, result);
        }

        [Fact]
        public void ValidateBreakpointLocation_NullBreakpointRange_ReturnsEFail()
        {
            // Arrange
            var languageService = CreateLanguageServiceWith();

            // Act
            var result = languageService.ValidateBreakpointLocation(Mock.Of<IVsTextBuffer>(), 0, 0, TextSpans);

            // Assert
            Assert.Equal(VSConstants.E_FAIL, result);
        }

        [Fact]
        public void ValidateBreakpointLocation_ValidBreakpointRange_ReturnsSOK()
        {
            // Arrange
            var breakpointRange = new Range()
            {
                Start = new Position(2, 4),
                End = new Position(3, 5),
            };
            var breakpointResolver = Mock.Of<RazorBreakpointResolver>(resolver => resolver.TryResolveBreakpointRangeAsync(It.IsAny<ITextBuffer>(), 0, 0, It.IsAny<CancellationToken>()) == Task.FromResult(breakpointRange));
            var languageService = CreateLanguageServiceWith(breakpointResolver);

            // Act
            var result = languageService.ValidateBreakpointLocation(Mock.Of<IVsTextBuffer>(), 0, 0, TextSpans);

            // Assert
            Assert.Equal(VSConstants.S_OK, result);
            var span = Assert.Single(TextSpans);
            Assert.Equal(2, span.iStartLine);
            Assert.Equal(4, span.iStartIndex);
            Assert.Equal(3, span.iEndLine);
            Assert.Equal(5, span.iEndIndex);
        }

        [Fact]
        public void ValidateBreakpointLocation_CanNotCreateDialog_ReturnsEFail()
        {
            // Arrange
            var languageService = CreateLanguageServiceWith(waitDialogFactory: Mock.Of<WaitDialogFactory>());

            // Act
            var result = languageService.ValidateBreakpointLocation(Mock.Of<IVsTextBuffer>(), 0, 0, TextSpans);

            // Assert
            Assert.Equal(VSConstants.E_FAIL, result);
        }

        [Fact]
        public void GetProximityExpressions_CanNotGetBackingTextBuffer_ReturnsNotImpl()
        {
            // Arrange
            var languageService = CreateLanguageServiceWith(editorAdaptersFactory: Mock.Of<IVsEditorAdaptersFactoryService>());

            // Act
            var result = languageService.GetProximityExpressions(Mock.Of<IVsTextBuffer>(), 0, 0, 0, out _);

            // Assert
            Assert.Equal(VSConstants.E_NOTIMPL, result);
        }

        [Fact]
        public void GetProximityExpressions_InvalidLocation_ReturnsEFail()
        {
            // Arrange
            var languageService = CreateLanguageServiceWith();

            // Act
            var result = languageService.GetProximityExpressions(Mock.Of<IVsTextBuffer>(), int.MaxValue, 0, 0, out _);

            // Assert
            Assert.Equal(VSConstants.E_FAIL, result);
        }

        [Fact]
        public void GetProximityExpressions_NullRange_ReturnsEFail()
        {
            // Arrange
            var languageService = CreateLanguageServiceWith();

            // Act
            var result = languageService.GetProximityExpressions(Mock.Of<IVsTextBuffer>(), 0, 0, 0, out _);

            // Assert
            Assert.Equal(VSConstants.E_FAIL, result);
        }

        [Fact]
        public void GetProximityExpressions_ValidRange_ReturnsSOK()
        {
            // Arrange
            IReadOnlyList<string> expressions = new[] { "something" };
            var resolver = Mock.Of<RazorProximityExpressionResolver>(resolver => resolver.TryResolveProximityExpressionsAsync(It.IsAny<ITextBuffer>(), 0, 0, It.IsAny<CancellationToken>()) == Task.FromResult(expressions));
            var languageService = CreateLanguageServiceWith(proximityExpressionResolver: resolver);

            // Act
            var result = languageService.GetProximityExpressions(Mock.Of<IVsTextBuffer>(), 0, 0, 0, out var resolvedExpressions);

            // Assert
            Assert.Equal(VSConstants.S_OK, result);
            var concreteResolvedExpressions = Assert.IsType<VsEnumBSTR>(resolvedExpressions);
            Assert.Equal(expressions, concreteResolvedExpressions._values);
        }

        [Fact]
        public void GetProximityExpressions_CanNotCreateDialog_ReturnsEFail()
        {
            // Arrange
            var languageService = CreateLanguageServiceWith(waitDialogFactory: Mock.Of<WaitDialogFactory>());

            // Act
            var result = languageService.GetProximityExpressions(Mock.Of<IVsTextBuffer>(), 0, 0, 0, out _);

            // Assert
            Assert.Equal(VSConstants.E_FAIL, result);
        }

        private RazorLanguageService CreateLanguageServiceWith(
            RazorBreakpointResolver breakpointResolver = null,
            RazorProximityExpressionResolver proximityExpressionResolver = null,
            WaitDialogFactory waitDialogFactory = null,
            IVsEditorAdaptersFactoryService editorAdaptersFactory = null)
        {
            breakpointResolver ??= Mock.Of<RazorBreakpointResolver>();
            proximityExpressionResolver ??= Mock.Of<RazorProximityExpressionResolver>();
            waitDialogFactory ??= new TestWaitDialogFactory();
            editorAdaptersFactory ??= Mock.Of<IVsEditorAdaptersFactoryService>(service => service.GetDataBuffer(It.IsAny<IVsTextBuffer>()) == new TestTextBuffer(new StringTextSnapshot(Environment.NewLine)));

            var languageService = new RazorLanguageService(breakpointResolver, proximityExpressionResolver, waitDialogFactory, editorAdaptersFactory);
            return languageService;
        }

        private class TestWaitDialogFactory : WaitDialogFactory
        {
            public override WaitDialogResult<TResult> TryCreateWaitDialog<TResult>(string title, string message, Func<WaitDialogContext, Task<TResult>> onWaitAsync)
            {
                var context = new DefaultWaitDialogContext();
                var result = onWaitAsync(context).Result;

                var dialogResult = new WaitDialogResult<TResult>(result, context.CancellationToken.IsCancellationRequested);
                return dialogResult;
            }
        }
    }
}
