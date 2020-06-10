using System;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
{
    public abstract class VirtualDocumentSnapshot
    {
        public abstract Uri Uri { get; }

        public abstract ITextSnapshot Snapshot { get; }

        public abstract long? HostDocumentSyncVersion { get; }
    }
}
