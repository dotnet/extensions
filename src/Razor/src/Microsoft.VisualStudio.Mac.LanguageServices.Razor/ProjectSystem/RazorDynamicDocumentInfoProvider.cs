using System.ComponentModel.Composition;
using MonoDevelop.Ide.TypeSystem;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    [System.Composition.Shared]
    [ExportMetadata("Extensions", new string[] { "cshtml", "razor", })]
    [Export(typeof(RazorDynamicDocumentInfoProvider))]
    [Export(typeof(IDynamicDocumentInfoProvider))]
    internal class RazorDynamicDocumentInfoProvider : RazorDynamicDocumentInfoProviderBase, IDynamicDocumentInfoProvider
    {
    }
}