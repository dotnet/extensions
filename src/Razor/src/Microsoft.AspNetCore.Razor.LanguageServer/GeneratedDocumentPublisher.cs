// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal abstract class GeneratedDocumentPublisher : ProjectSnapshotChangeTrigger
    {
        public abstract void PublishCSharp(string filePath, SourceText sourceText, int hostDocumentVersion);

        public abstract void PublishHtml(string filePath, SourceText sourceText, int hostDocumentVersion);
    }
}
