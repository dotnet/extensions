// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal abstract class ProjectConfigurationFilePathStore
    {
        public abstract event EventHandler<ProjectConfigurationFilePathChangedEventArgs> Changed;

        public abstract IReadOnlyDictionary<string, string> GetMappings();

        public abstract void Set(string projectFilePath, string configurationFilePath);

        public abstract bool TryGet(string projectFilePath, out string configurationFilePath);

        public abstract void Remove(string projectFilePath);
    }
}
