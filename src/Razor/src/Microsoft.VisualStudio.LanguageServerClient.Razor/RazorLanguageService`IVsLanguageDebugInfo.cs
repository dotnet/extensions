// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServerClient.Razor.Dialogs;
using Microsoft.VisualStudio.TextManager.Interop;
using TextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    internal partial class RazorLanguageService : IVsLanguageDebugInfo
    {
        private readonly WaitDialogFactory _waitDialogFactory;

        [ImportingConstructor]
        public RazorLanguageService(WaitDialogFactory waitDialogFactory)
        {
            if (waitDialogFactory is null)
            {
                throw new ArgumentNullException(nameof(waitDialogFactory));
            }

            _waitDialogFactory = waitDialogFactory;
        }

        public int GetProximityExpressions(IVsTextBuffer pBuffer, int iLine, int iCol, int cLines, out IVsEnumBSTR ppEnum)
        {
            ppEnum = default;
            return VSConstants.E_NOTIMPL;
        }

        public int ValidateBreakpointLocation(IVsTextBuffer pBuffer, int iLine, int iCol, TextSpan[] pCodeSpan)
        {
            var dialogResult = _waitDialogFactory.TryCreateWaitDialog(
                title: "Determining breakpoint location...",
                message: "Razor Debugger",
                (context) =>
                {
                    return Task.FromResult(VSConstants.E_NOTIMPL);
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
    }
}
