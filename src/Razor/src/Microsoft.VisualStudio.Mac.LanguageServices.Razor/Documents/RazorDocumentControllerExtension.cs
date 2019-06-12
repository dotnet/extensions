using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Documents;

namespace Microsoft.VisualStudio.Editor.Razor.Documents
{
    /// <summary>
    /// This is a way to notify the VisualStudioMacEditorDocumentManager when Razor documents are
    /// opened or closed.
    /// TODO: add support for an open document being renamed, when the MonoDevelop API is available.
    /// </summary>
    [ExportDocumentControllerExtension(MimeType = "text/x-cshtml-web")]
    class RazorDocumentControllerExtension : DocumentControllerExtension
    {
        public override Task Initialize(Properties status)
        {
            var controller = ((FileDocumentController)Controller);
            var filePath = controller.FilePath.ToString();
            var textBuffer = controller.GetContent<ITextBuffer>();

            VisualStudioMacEditorDocumentManagerFactory.Instance.HandleDocumentOpened(filePath, textBuffer);

            return base.Initialize(status);
        }

        protected override void OnClosed()
        {
            var controller = ((FileDocumentController)Controller);
            var filePath = controller.FilePath.ToString();

            VisualStudioMacEditorDocumentManagerFactory.Instance.HandleDocumentClosed(filePath);

            base.OnClosed();
        }

        public override Task<bool> SupportsController(DocumentController controller)
        {
            if (controller.GetContent<ITextBuffer>() == null || !(controller is FileDocumentController))
            {
                return Task.FromResult(false);
            }

            return base.SupportsController(controller);
        }
    }
}