// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed
{
    internal class DefaultHostDocumentShim : HostDocumentShim
    {
        public DefaultHostDocumentShim(HostDocument hostDocument)
        {
            if (hostDocument == null)
            {
                throw new ArgumentNullException(nameof(hostDocument));
            }

            InnerHostDocument = hostDocument;
            GeneratedCodeContainer = new GeneratedCodeContainerShim(hostDocument.GeneratedCodeContainer);
        }

        public HostDocument InnerHostDocument { get; }

        public override string FilePath => InnerHostDocument.FilePath;

        public override string TargetPath => InnerHostDocument.TargetPath;

        public override GeneratedCodeContainerShim GeneratedCodeContainer { get; }
    }
}
