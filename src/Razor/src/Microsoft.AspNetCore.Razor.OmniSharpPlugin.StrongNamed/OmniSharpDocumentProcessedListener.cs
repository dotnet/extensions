namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin.StrongNamed
{
    public abstract class OmniSharpDocumentProcessedListener
    {
        public abstract void Initialize(OmniSharpProjectSnapshotManager projectManager);

        public abstract void DocumentProcessed(OmniSharpDocumentSnapshot document);
    }
}
