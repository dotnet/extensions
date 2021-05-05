// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Remote.Razor
{
    internal interface IRemoteTagHelperProviderService
    {
        ValueTask<TagHelperResolutionResult> GetTagHelpersAsync(RazorPinnedSolutionInfoWrapper solutionInfo, ProjectSnapshotHandle projectHandle, string factoryTypeName, CancellationToken cancellationToken);
    }
}
