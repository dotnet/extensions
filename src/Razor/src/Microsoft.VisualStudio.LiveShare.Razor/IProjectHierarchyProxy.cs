// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LiveShare.Razor
{
    public interface IProjectHierarchyProxy
    {
        Task<Uri> GetProjectPathAsync(Uri documentFilePath, CancellationToken cancellationToken);
    }
}
