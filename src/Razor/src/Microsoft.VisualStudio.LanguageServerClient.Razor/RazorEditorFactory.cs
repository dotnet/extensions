// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    [Guid(EditorFactoryGuidString)]
    internal class RazorEditorFactory : EditorFactory
    {
        private const string EditorFactoryGuidString = "3dfdce9e-1799-4372-8aa6-d8e65182fdfc";
        private readonly LSPEditorFeatureDetector _lspEditorFeatureDetector;

        public RazorEditorFactory(AsyncPackage package) : base(package)
        {
            var componentModel = (IComponentModel)AsyncPackage.GetGlobalService(typeof(SComponentModel));
            _lspEditorFeatureDetector = componentModel.GetService<LSPEditorFeatureDetector>();
        }

        public override int CreateEditorInstance(
            uint createDocFlags,
            string moniker,
            string physicalView,
            IVsHierarchy hierarchy,
            uint itemid,
            IntPtr existingDocData,
            out IntPtr docView,
            out IntPtr docData,
            out string editorCaption,
            out Guid cmdUI,
            out int cancelled)
        {
            if (!_lspEditorFeatureDetector.IsLSPEditorAvailable(moniker, hierarchy))
            {
                docView = default;
                docData = default;
                editorCaption = null;
                cmdUI = default;
                cancelled = 0;

                // Razor LSP is not enabled, allow another editor to handle this document
                return VSConstants.VS_E_UNSUPPORTEDFORMAT;
            }

            var editorInstance = base.CreateEditorInstance(createDocFlags, moniker, physicalView, hierarchy, itemid, existingDocData, out docView, out docData, out editorCaption, out cmdUI, out cancelled);
            var textLines = (IVsTextLines)Marshal.GetObjectForIUnknown(docData);

            // Next, the editor typically resets the ContentType after TextBuffer creation. We need to let them know
            // to not update the content type because we'll be taking care of the ContentType changing lifecycle.
            var userData = textLines as IVsUserData;
            var hresult = userData.SetData(VSConstants.VsTextBufferUserDataGuid.VsBufferDetectLangSID_guid, false);

            ErrorHandler.ThrowOnFailure(hresult);

            return editorInstance;
        }
    }
}
