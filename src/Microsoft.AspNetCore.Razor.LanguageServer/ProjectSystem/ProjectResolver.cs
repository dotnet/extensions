// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed;

namespace Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem
{
    public abstract class ProjectResolver
    {
        public abstract bool TryResolveProject(string documentPath, out ProjectSnapshotShim projectSnapshot);

        public abstract ProjectSnapshotShim GetMiscellaneousProject();
    }
}
