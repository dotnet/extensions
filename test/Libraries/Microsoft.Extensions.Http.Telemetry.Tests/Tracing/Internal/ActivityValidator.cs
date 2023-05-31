// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Xunit;

namespace Microsoft.Extensions.Http.Telemetry.Tracing.Test.Internal;

internal static class ActivityValidator
{
    public static void AssertSensitiveTagsAreNull(this Activity activity)
    {
        Assert.Null(activity.GetTagItem(Constants.AttributeUserAgent));
        Assert.Null(activity.GetTagItem(Constants.AttributeHttpTarget));
        Assert.Null(activity.GetTagItem(Constants.AttributeHttpPath));
        Assert.Null(activity.GetTagItem(Constants.AttributeHttpScheme));
        Assert.Null(activity.GetTagItem(Constants.AttributeHttpFlavor));
        Assert.Null(activity.GetTagItem(Constants.AttributeNetPeerName));
        Assert.Null(activity.GetTagItem(Constants.AttributeNetPeerPort));
    }
}
