// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion
{
    internal static class VSLspCompletionItemIcons
    {
        private const int XMLAttributeId = 3564;
        private const string ImageCatalogGuidString = "{ae27a6b0-e345-4288-96df-5eaf394ee369}";
        private static Guid ImageCatalogGuid = new Guid(ImageCatalogGuidString);

        static VSLspCompletionItemIcons()
        {
            var imageId = new PascalCasedSerializableImageId(ImageCatalogGuid, XMLAttributeId);
            TagHelper = new PascalCasedSerializableImageElement(imageId);
        }

        public static PascalCasedSerializableImageElement TagHelper { get; }
    }
}
