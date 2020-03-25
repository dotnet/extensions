using System;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    public abstract class VirtualDocumentSnapshot
    {
        public abstract Uri Uri { get; }

        public abstract ITextSnapshot Snapshot { get; }

        public abstract long? HostDocumentSyncVersion { get; }
    }
}
