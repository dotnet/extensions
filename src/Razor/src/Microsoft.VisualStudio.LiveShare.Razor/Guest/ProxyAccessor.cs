// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.VisualStudio.LiveShare.Razor.Guest
{
    public abstract class ProxyAccessor : ILanguageService
    {
        public abstract IProjectSnapshotManagerProxy GetProjectSnapshotManagerProxy();

        public abstract IProjectHierarchyProxy GetProjectHierarchyProxy();
    }
}
