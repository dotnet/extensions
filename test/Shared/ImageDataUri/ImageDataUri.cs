// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
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

    internal static Uri GetPdfDataUri()
    {
        using PdfDocumentBuilder builder = new PdfDocumentBuilder();
        PdfPageBuilder page = builder.AddPage(PageSize.A4);
        PdfDocumentBuilder.AddedFont font = builder.AddStandard14Font(Standard14Font.Helvetica);
        page.AddText("Hello World!", 12, new PdfPoint(25, 700), font);
        return new Uri($"data:application/pdf;base64,{Convert.ToBase64String(builder.Build())}");
    }
}
