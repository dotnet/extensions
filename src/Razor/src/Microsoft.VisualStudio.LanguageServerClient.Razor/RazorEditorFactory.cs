// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    [Guid(EditorFactoryGuidString)]
    internal class RazorEditorFactory : EditorFactory
    {
        private const string EditorFactoryGuidString = "3dfdce9e-1799-4372-8aa6-d8e65182fdfc";
        private const string RazorLSPEditorFeatureFlag = "Razor.LSP.Editor";
        private static readonly Guid LiveShareHostUIContextGuid = Guid.Parse("62de1aa5-70b0-4934-9324-680896466fe1");
        private static readonly Guid LiveShareGuestUIContextGuid = Guid.Parse("fd93f3eb-60da-49cd-af15-acda729e357e");
        private readonly Lazy<IVsEditorAdaptersFactoryService> _adaptersFactory;
        private readonly Lazy<IContentType> _razorLSPContentType;
        private readonly Lazy<IVsFeatureFlags> _featureFlags;

        public RazorEditorFactory(AsyncPackage package) : base(package)
        {
            _adaptersFactory = new Lazy<IVsEditorAdaptersFactoryService>(() =>
            {
                var componentModel = (IComponentModel)AsyncPackage.GetGlobalService(typeof(SComponentModel));
                var adaptersFactory = componentModel.GetService<IVsEditorAdaptersFactoryService>();
                return adaptersFactory;
            });

            _razorLSPContentType = new Lazy<IContentType>(() =>
            {
                var componentModel = (IComponentModel)AsyncPackage.GetGlobalService(typeof(SComponentModel));
                var contentTypeService = componentModel.GetService<IContentTypeRegistryService>();
                var contentType = contentTypeService.GetContentType(RazorLSPContentTypeDefinition.Name);
                return contentType;
            });

            _featureFlags = new Lazy<IVsFeatureFlags>(() =>
            {
                var featureFlags = (IVsFeatureFlags)AsyncPackage.GetGlobalService(typeof(SVsFeatureFlags));
                return featureFlags;
            });
        }

        private bool IsRazorLSPEditorEnabled
        {
            get
            {
                var lspRazorEnabledString = Environment.GetEnvironmentVariable(RazorLSPEditorFeatureFlag);
                bool.TryParse(lspRazorEnabledString, out var enabled);
                if (enabled)
                {
                    return true;
                }

                if (_featureFlags.Value.IsFeatureEnabled(RazorLSPEditorFeatureFlag, defaultValue: false))
                {
                    return true;
                }

                if (IsVSServer())
                {
                    // We default to "on" in Visual Studio server cloud environments
                    return true;
                }

                if (IsVSRemoteClient())
                {
                    // We default to "on" in Visual Studio remotely joined cloud environment clients
                    return true;
                }

                if (IsLiveShareHost())
                {
                    // Placeholder for when we turn on LiveShare support by default
                    return false;
                }

                if (IsLiveShareGuest())
                {
                    // Placeholder for when we turn on LiveShare support by default
                    return false;
                }

                return false;
            }
        }

        public override int CreateEditorInstance(
            uint createDocFlags,
            string moniker,
            string physicalView,
            IVsHierarchy pHier,
            uint itemid,
            IntPtr existingDocData,
            out IntPtr docView,
            out IntPtr docData,
            out string editorCaption,
            out Guid cmdUI,
            out int cancelled)
        {
            if (!IsRazorLSPEditorEnabled)
            {
                docView = default;
                docData = default;
                editorCaption = null;
                cmdUI = default;
                cancelled = 0;

                // Razor LSP is not enabled, allow another editor to handle this document
                return VSConstants.VS_E_UNSUPPORTEDFORMAT;
            }

            var editorInstance = base.CreateEditorInstance(createDocFlags, moniker, physicalView, pHier, itemid, existingDocData, out docView, out docData, out editorCaption, out cmdUI, out cancelled);
            var textLines = (IVsTextLines)Marshal.GetObjectForIUnknown(docData);

            SetTextBufferContentType(textLines);

            return editorInstance;
        }

        private void SetTextBufferContentType(IVsTextLines textLines)
        {
            var textBufferDataEventsGuid = typeof(IVsTextBufferDataEvents).GUID;
            var connectionPointContainer = textLines as IConnectionPointContainer;
            connectionPointContainer.FindConnectionPoint(textBufferDataEventsGuid, out var connectionPoint);
            var contentTypeSetter = new TextBufferContentTypeSetter(
                textLines,
                _adaptersFactory.Value,
                _razorLSPContentType.Value);
            contentTypeSetter.Attach(connectionPoint);

            // Next, the editor typically resets the ContentType after TextBuffer creation. We need to let them know
            // to not update the content type.
            var userData = textLines as IVsUserData;
            var hresult = userData.SetData(VSConstants.VsTextBufferUserDataGuid.VsBufferDetectLangSID_guid, false);

            ErrorHandler.ThrowOnFailure(hresult);
        }

        private static bool IsVSServer()
        {
            var shell = AsyncPackage.GetGlobalService(typeof(SVsShell)) as IVsShell;
            var result = shell.GetProperty((int)__VSSPROPID11.VSSPROPID_ShellMode, out var mode);

            if (!ErrorHandler.Succeeded(result))
            {
                return false;
            }

            // VSSPROPID_ShellMode is set to VSSM_Server when /server is used in devenv command
            if ((int)mode != (int)__VSShellMode.VSSM_Server)
            {
                return false;
            }

            return true;
        }

        private static bool IsVSRemoteClient()
        {
            var context = UIContext.FromUIContextGuid(VSConstants.UICONTEXT.CloudEnvironmentConnected_guid);
            return context.IsActive;
        }

        private static bool IsLiveShareGuest()
        {
            var context = UIContext.FromUIContextGuid(LiveShareGuestUIContextGuid);
            return context.IsActive;
        }

        private bool IsLiveShareHost()
        {
            var context = UIContext.FromUIContextGuid(LiveShareHostUIContextGuid);
            return context.IsActive;
        }

        private class TextBufferContentTypeSetter : IVsTextBufferDataEvents
        {
            private readonly IVsTextLines _textLines;
            private readonly IVsEditorAdaptersFactoryService _adaptersFactory;
            private readonly IContentType _razorLSPContentType;
            private IConnectionPoint _connectionPoint;
            private uint _connectionPointCookie;

            public TextBufferContentTypeSetter(
                IVsTextLines textLines,
                IVsEditorAdaptersFactoryService adaptersFactory,
                IContentType razorLSPContentType)
            {
                _textLines = textLines;
                _adaptersFactory = adaptersFactory;
                _razorLSPContentType = razorLSPContentType;
            }

            public void Attach(IConnectionPoint connectionPoint)
            {
                if (connectionPoint is null)
                {
                    throw new ArgumentNullException(nameof(connectionPoint));
                }

                _connectionPoint = connectionPoint;

                connectionPoint.Advise(this, out _connectionPointCookie);
            }

            public void OnFileChanged(uint grfChange, uint dwFileAttrs)
            {
            }

            public int OnLoadCompleted(int fReload)
            {
                try
                {
                    var diskBuffer = _adaptersFactory.GetDocumentBuffer(_textLines);

                    if (IsVSRemoteClient() || IsLiveShareGuest())
                    {
                        // We purposefully do not set ClientName's in remote client scenarios because we don't want to boot 2 langauge servers (one for both host and client).
                        // The ClientName controls whether or not an ILanguageClient instantiates.
                    }
                    else
                    {
                        diskBuffer.Properties.AddProperty(LanguageClientConstants.ClientNamePropertyKey, RazorLanguageServerClient.ClientName);
                    }

                    diskBuffer.ChangeContentType(_razorLSPContentType, editTag: null);
                }
                finally
                {
                    _connectionPoint.Unadvise(_connectionPointCookie);
                }

                return VSConstants.S_OK;
            }
        }
    }
}
