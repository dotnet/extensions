// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Razor.Editor;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    /// <summary>
    /// Keeps track of accurate settings on the client side so we can easily retrieve the
    /// options later when the server sends us a workspace/configuration request.
    /// </summary>
    [Shared]
    [Export(typeof(RazorLSPClientOptionsMonitor))]
    internal class RazorLSPClientOptionsMonitor
    {
        public EditorSettings EditorSettings { get; private set; }

        public void UpdateOptions(EditorSettings editorSettings)
        {
            EditorSettings = editorSettings;
        }
    }
}
