// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.Http.Diagnostics;

internal sealed class DefaultDownstreamDependencyMetadataManager(
    IEnumerable<IDownstreamDependencyMetadata> downstreamDependencyMetadata)
    : DownstreamDependencyMetadataManager(downstreamDependencyMetadata);

