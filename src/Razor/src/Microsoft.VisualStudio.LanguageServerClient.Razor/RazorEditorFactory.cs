using System;
using System.Runtime.InteropServices;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    [Guid(EditorFactoryGuidString)]
    public class RazorEditorFactory : EditorFactory
    {
        private const string EditorFactoryGuidString = "3dfdce9e-1799-4372-8aa6-d8e65182fdfc";
        private const string RazorLSPEditorFeatureFlag = "Razor.LSP.Editor";
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

        private bool IsLSPRazorEditorEnabled
        {
            get
            {
                var lspRazorEnabledString = Environment.GetEnvironmentVariable(RazorLSPEditorFeatureFlag);
                bool.TryParse(lspRazorEnabledString, out var enabled);
                if (enabled)
                {
                    return true;
                }

                enabled = _featureFlags.Value.IsFeatureEnabled(RazorLSPEditorFeatureFlag, defaultValue: false);
                return enabled;
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
            if (!IsLSPRazorEditorEnabled)
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
