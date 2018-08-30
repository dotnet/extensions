// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem
{
    internal abstract class ProjectResolver
    {
        public abstract bool TryResolvePotentialProject(string documentFilePath, out ProjectSnapshot projectSnapshot);

        public abstract ProjectSnapshot GetMiscellaneousProject();
    }
}
