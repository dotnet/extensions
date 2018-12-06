// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Razor;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.LiveShare.Razor.Serialization
{
    internal static class LiveShareJsonConverterCollectionExtensions
    {
        public static void RegisterRazorLiveShareConverters(this JsonConverterCollection collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (collection.Contains(ProjectSnapshotHandleProxyJsonConverter.Instance))
            {
                // Already registered.
                return;
            }

            collection.Add(ProjectSnapshotHandleProxyJsonConverter.Instance);
            collection.RegisterRazorConverters();
        }
    }
}