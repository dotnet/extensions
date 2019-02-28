// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.LanguageServer.Common.Serialization;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin.StrongNamed.Serialization
{
    public static class JsonConverterCollectionExtensions
    {
        public static void RegisterOmniSharpRazorConverters(this JsonConverterCollection collection)
        {
            collection.RegisterRazorConverters();
            collection.Add(OmniSharpProjectSnapshotHandleJsonConverter.Instance);
        }
    }
}
