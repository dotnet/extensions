// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    internal static class RazorLSPConstants
    {
        public const string RazorLSPContentTypeName = "RazorLSP";

        public const string CSHTMLFileExtension = ".cshtml";

        public const string RazorFileExtension = ".razor";

        public const string CSharpContentTypeName = "CSharp";

        public const string HtmlLSPContentTypeName = "html-delegation";

        public const string VirtualCSharpFileNameSuffix = ".g.cs";

        public const string VirtualHtmlFileNameSuffix = "__virtual.html";

        public const string VSProjectItemsIdentifier = "CF_VSSTGPROJECTITEMS";

        public static readonly Guid RazorActiveUIContextGuid = new Guid("3c5ded8f-72c7-4b1f-af2d-099ceeb935b8");
    }
}
