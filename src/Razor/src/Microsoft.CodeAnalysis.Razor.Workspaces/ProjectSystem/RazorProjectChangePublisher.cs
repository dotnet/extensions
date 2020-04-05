// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Concurrent;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal abstract class RazorProjectChangePublisher : ProjectSnapshotChangeTrigger
    {
        protected ConcurrentDictionary<string, string> PublishFilePathMappings { get; } = new ConcurrentDictionary<string, string>(FilePathComparer.Instance);

        public abstract void SetPublishFilePath(string projectFilePath, string publishFilePath);

        public abstract void RemovePublishFilePath(string projectFilePath);
    }
}
