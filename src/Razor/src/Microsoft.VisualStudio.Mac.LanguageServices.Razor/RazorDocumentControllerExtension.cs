using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Editor.Razor;
using Microsoft.VisualStudio.Editor.Razor.Documents;
using Microsoft.VisualStudio.Text;
using MonoDevelop.Core;
using MonoDevelop.Ide.Composition;
using MonoDevelop.Ide.Gui.Documents;

namespace Microsoft.VisualStudio.Mac.LanguageServices.Razor
{
    /// <summary>
    /// This is a way to notify the VisualStudioMacEditorDocumentManager when Razor documents are
    /// opened or closed.
    /// </summary>
    [ExportDocumentControllerExtension(MimeType = "text/x-cshtml-web")]
    internal class RazorDocumentControllerExtension : DocumentControllerExtension
    {
        private readonly VisualStudioWorkspaceAccessor _workspaceAccessor;
        private VisualStudioMacEditorDocumentManager _editorDocumentManager;

        public RazorDocumentControllerExtension()
        {
            _workspaceAccessor = CompositionManager.Instance.GetExportedValue<VisualStudioMacWorkspaceAccessor>();
        }

        public override Task Initialize(Properties status)
        {
            var controller = (FileDocumentController)Controller;
            var filePath = controller.FilePath.ToString();
            var textBuffer = controller.GetContent<ITextBuffer>();

            if (!_workspaceAccessor.TryGetWorkspace(textBuffer, out var workspace))
            {
                return Task.CompletedTask;
            }

            _editorDocumentManager = workspace.Services.GetRequiredService<EditorDocumentManager>() as VisualStudioMacEditorDocumentManager;

            Debug.Assert(_editorDocumentManager != null);

            _editorDocumentManager.HandleDocumentOpened(filePath, textBuffer);

            return Task.CompletedTask;
        }

        protected override void OnClosed()
        {
            if (_editorDocumentManager == null)
            {
                return;
            }

            var controller = (FileDocumentController)Controller;
            var filePath = controller.FilePath.ToString();

            _editorDocumentManager.HandleDocumentClosed(filePath);
        }

        public override Task<bool> SupportsController(DocumentController controller)
        {
            if (controller.GetContent<ITextBuffer>() == null || !(controller is FileDocumentController))
            {
                return Task.FromResult(false);
            }

            var supportsController = base.SupportsController(controller);
            return supportsController;
        }
    }
}