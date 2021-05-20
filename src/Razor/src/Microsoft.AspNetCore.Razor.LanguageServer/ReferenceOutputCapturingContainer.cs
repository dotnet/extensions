// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class ReferenceOutputCapturingContainer : GeneratedDocumentContainer
    {
        private readonly object _outputTaskLock = new object();
#pragma warning disable IDE0052 // Remove unread private members
        private Task<(RazorCodeDocument, VersionStamp, VersionStamp, VersionStamp)> _outputTask;
#pragma warning restore IDE0052 // Remove unread private members

        public async Task SetOutputAndCaptureReferenceAsync(DefaultDocumentSnapshot document)
        {
            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var generatedOutputTask = document.State.GetGeneratedOutputAndVersionAsync(document.ProjectInternal, document);
            var (codeDocument, inputVersion, outputCSharpVersion, outputHtmlVersion) = await generatedOutputTask;

            lock (_outputTaskLock)
            {
                if (TrySetOutput(document, codeDocument, inputVersion, outputCSharpVersion, outputHtmlVersion))
                {
                    _outputTask = generatedOutputTask;
                }
            }
        }
    }
}
