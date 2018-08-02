// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed;

namespace Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem
{
    public abstract class DocumentResolver
    {
        public abstract bool TryResolveDocument(string documentFilePath, out DocumentSnapshotShim document);
    }
}
