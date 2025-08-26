// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Compliance.Redaction;

namespace Microsoft.Extensions.Http.Diagnostics;

internal sealed class DefaultHttpRouteFormatter(HttpRouteParser httpRouteParser, IRedactorProvider redactorProvider)
    : HttpRouteFormatter(httpRouteParser, redactorProvider);

