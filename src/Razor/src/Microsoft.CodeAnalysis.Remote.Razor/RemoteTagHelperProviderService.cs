// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Api;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.ServiceHub.Framework;

namespace Microsoft.CodeAnalysis.Remote.Razor
{
    internal sealed class RemoteTagHelperProviderService : RazorServiceBase, IRemoteTagHelperProviderService
    {
        internal RemoteTagHelperProviderService(IServiceBroker serviceBroker)
            : base(serviceBroker)
        {
        }

        public ValueTask<TagHelperResolutionResult> GetTagHelpersAsync(RazorPinnedSolutionInfoWrapper solutionInfo, ProjectSnapshotHandle projectHandle, string factoryTypeName, CancellationToken cancellationToken = default)
            => RazorBrokeredServiceImplementation.RunServiceAsync(cancellationToken => GetTagHelpersCoreAsync(solutionInfo, projectHandle, factoryTypeName, cancellationToken), cancellationToken);

        private async ValueTask<TagHelperResolutionResult> GetTagHelpersCoreAsync(RazorPinnedSolutionInfoWrapper solutionInfo, ProjectSnapshotHandle projectHandle, string factoryTypeName, CancellationToken cancellationToken)
        {
            if (projectHandle is null)
            {
                throw new ArgumentNullException(nameof(projectHandle));
            }

            if (string.IsNullOrEmpty(factoryTypeName))
            {
                throw new ArgumentException($"'{nameof(factoryTypeName)}' cannot be null or empty.", nameof(factoryTypeName));
            }

            var solution = await solutionInfo.GetSolutionAsync(ServiceBrokerClient, cancellationToken).ConfigureAwait(false);
            var projectSnapshot = await GetProjectSnapshotAsync(projectHandle, cancellationToken).ConfigureAwait(false);
            var workspaceProject = solution
                .Projects
                .FirstOrDefault(project => FilePathComparer.Instance.Equals(project.FilePath, projectSnapshot.FilePath));

            if (workspaceProject == null)
            {
                return TagHelperResolutionResult.Empty;
            }

            var resolutionResult = await RazorServices.TagHelperResolver.GetTagHelpersAsync(workspaceProject, projectHandle.Configuration, factoryTypeName, cancellationToken).ConfigureAwait(false);
            return resolutionResult;
        }
    }
}
