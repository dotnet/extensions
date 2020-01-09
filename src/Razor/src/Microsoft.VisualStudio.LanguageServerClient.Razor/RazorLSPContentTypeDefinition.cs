using System.ComponentModel.Composition;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    internal sealed class RazorLSPContentTypeDefinition
    {
        public const string Name = "RazorLSP";
        public const string CSHTMLFileExtension = ".cshtml";
        public const string RazorFileExtension = ".razor";

        /// <summary>
        /// Exports the Razor LSP content type
        /// </summary>
        [Export]
        [Name(Name)]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteContentTypeName)]
        public ContentTypeDefinition RazorLSPContentType { get; set; }

        // We can't assocaite the Razor LSP content type with the above file extensions because there's already a content type
        // associated with them. Instead, we utilize our RazorEditorFactory to assign the RazorLSPContentType to .razor/.cshtml
        // files.
    }
}
