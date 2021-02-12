// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    internal record CompletionRequestContext(Uri HostDocumentUri, Uri ProjectedDocumentUri, LanguageServerKind LanguageServerKind);
}
