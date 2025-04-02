// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;

namespace Microsoft.Extensions.AI;

internal static class ImageDataUri
{
    internal static Uri GetImageDataUri()
    {
        using Stream? s = typeof(ImageDataUri).Assembly.GetManifestResourceStream("Microsoft.Extensions.AI.Resources.dotnet.png");
        Assert.NotNull(s);
        MemoryStream ms = new();
        s.CopyTo(ms);
        return new Uri($"data:image/png;base64,{Convert.ToBase64String(ms.ToArray())}");
    }
}
