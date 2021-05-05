// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServerClient.Razor.Debugging;
using Microsoft.VisualStudio.LanguageServerClient.Razor.Dialogs;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using TextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    internal partial class RazorLanguageService : IVsLanguageDebugInfo
    {
        private readonly RazorBreakpointResolver _breakpointResolver;
        private readonly RazorProximityExpressionResolver _proximityExpressionResolver;
        private readonly WaitDialogFactory _waitDialogFactory;
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactory;

        public RazorLanguageService(
            RazorBreakpointResolver breakpointResolver,
            RazorProximityExpressionResolver proximityExpressionResolver,
            WaitDialogFactory waitDialogFactory,
            IVsEditorAdaptersFactoryService editorAdaptersFactory)
        {
            if (breakpointResolver is null)
            {
                throw new ArgumentNullException(nameof(breakpointResolver));
            }

            if (proximityExpressionResolver is null)
            {
                throw new ArgumentNullException(nameof(proximityExpressionResolver));
            }

            if (waitDialogFactory is null)
            {
                throw new ArgumentNullException(nameof(waitDialogFactory));
            }

            if (editorAdaptersFactory is null)
            {
                throw new ArgumentNullException(nameof(editorAdaptersFactory));
            }

            _breakpointResolver = breakpointResolver;
            _proximityExpressionResolver = proximityExpressionResolver;
            _waitDialogFactory = waitDialogFactory;
            _editorAdaptersFactory = editorAdaptersFactory;
        }

        public int GetProximityExpressions(IVsTextBuffer pBuffer, int iLine, int iCol, int cLines, out IVsEnumBSTR ppEnum)
        {
            var textBuffer = _editorAdaptersFactory.GetDataBuffer(pBuffer);
            if (textBuffer == null)
            {
                // Can't resolve the text buffer, let someone else deal with this breakpoint.
                ppEnum = null;
                return VSConstants.E_NOTIMPL;
            }

            var snapshot = textBuffer.CurrentSnapshot;
            if (!ValidateLocation(snapshot, iLine, iCol))
            {
                // The point disappeared between sessions. Do not evaluate proximity expressions here.
                ppEnum = null;
                return VSConstants.E_FAIL;
            }

            var dialogResult = _waitDialogFactory.TryCreateWaitDialog(
                title: "Determining proximity expressions...",
                message: "Razor Debugger",
                async (context) =>
                {
                    var proximityExpressions = await _proximityExpressionResolver.TryResolveProximityExpressionsAsync(textBuffer, iLine, iCol, context.CancellationToken).ConfigureAwait(false);
                    return proximityExpressions;
                });

            if (dialogResult == null)
            {
                // Failed to create the dialog at all.
                ppEnum = null;
                return VSConstants.E_FAIL;
            }

            if (dialogResult.Cancelled)
            {
                ppEnum = null;
                return VSConstants.E_FAIL;
            }

            if (dialogResult.Result == null)
            {
                ppEnum = null;
                return VSConstants.E_FAIL;
            }

            ppEnum = new VsEnumBSTR(dialogResult.Result);
            return VSConstants.S_OK;
        }

        public int ValidateBreakpointLocation(IVsTextBuffer pBuffer, int iLine, int iCol, TextSpan[] pCodeSpan)
        {
            var textBuffer = _editorAdaptersFactory.GetDataBuffer(pBuffer);
            if (textBuffer == null)
            {
                // Can't resolve the text buffer, let someone else deal with this breakpoint.
                return VSConstants.E_NOTIMPL;
            }

            var snapshot = textBuffer.CurrentSnapshot;
            if (!ValidateLocation(snapshot, iLine, iCol))
            {
                // The point disappeared between sessions. Do not allow a breakpoint here.
                return VSConstants.E_FAIL;
            }

            var dialogResult = _waitDialogFactory.TryCreateWaitDialog(
                title: "Determining breakpoint location...",
                message: "Razor Debugger",
                async (context) =>
                {
                    var breakpointRange = await _breakpointResolver.TryResolveBreakpointRangeAsync(textBuffer, iLine, iCol, context.CancellationToken).ConfigureAwait(false);
                    if (breakpointRange == null)
                    {
                        // No applicable breakpoint location.
                        return VSConstants.E_FAIL;
                    }

                    pCodeSpan[0] = new TextSpan()
                    {
                        iStartIndex = breakpointRange.Start.Character,
                        iStartLine = breakpointRange.Start.Line,
                        iEndIndex = breakpointRange.End.Character,
                        iEndLine = breakpointRange.End.Line,
                    };
                    return VSConstants.S_OK;
                });

            if (dialogResult == null)
            {
                // Failed to create the dialog at all.
                return VSConstants.E_FAIL;
            }

            if (dialogResult.Cancelled)
            {
                return VSConstants.E_FAIL;
            }

            return dialogResult.Result;
        }

        public int GetNameOfLocation(IVsTextBuffer pBuffer, int iLine, int iCol, out string pbstrName, out int piLineOffset)
        {
            pbstrName = default;
            piLineOffset = default;
            return VSConstants.E_NOTIMPL;
        }

        public int GetLocationOfName(string pszName, out string pbstrMkDoc, TextSpan[] pspanLocation)
        {
            pbstrMkDoc = default;
            return VSConstants.E_NOTIMPL;
        }

        public int ResolveName(string pszName, uint dwFlags, out IVsEnumDebugName ppNames)
        {
            ppNames = default;
            return VSConstants.E_NOTIMPL;
        }

        public int GetLanguageID(IVsTextBuffer pBuffer, int iLine, int iCol, out Guid pguidLanguageID)
        {
            pguidLanguageID = default;
            return VSConstants.E_NOTIMPL;
        }

        public int IsMappedLocation(IVsTextBuffer pBuffer, int iLine, int iCol)
        {
            return VSConstants.E_NOTIMPL;
        }

        private static bool ValidateLocation(ITextSnapshot snapshot, int lineNumber, int columnIndex)
        {
            if (lineNumber < 0 || lineNumber >= snapshot.LineCount)
            {
                return false;
            }

            var line = snapshot.GetLineFromLineNumber(lineNumber);
            if (columnIndex < 0 || columnIndex > line.Length)
            {
                return false;
            }

            return true;
        }
    }
}
