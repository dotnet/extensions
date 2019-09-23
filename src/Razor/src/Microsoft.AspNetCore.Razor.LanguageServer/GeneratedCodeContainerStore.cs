// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal abstract class GeneratedCodeContainerStore : ProjectSnapshotChangeTrigger
    {
        public abstract GeneratedCodeContainer Get(string physicalFilePath);
    }
}
