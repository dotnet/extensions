// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Serialization;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks
{
    public class MemoryCacheBenchmark
    {
        public MemoryCacheBenchmark()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null && !File.Exists(Path.Combine(current.FullName, "taghelpers.json")))
            {
                current = current.Parent;
            }

            var tagHelperFilePath = Path.Combine(current.FullName, "taghelpers.json");
            var tagHelperBuffer = File.ReadAllBytes(tagHelperFilePath);

            // Deserialize from json file.
            var serializer = new JsonSerializer();
            serializer.Converters.Add(new TagHelperDescriptorJsonConverter());
            using (var stream = new MemoryStream(tagHelperBuffer))
            using (var reader = new JsonTextReader(new StreamReader(stream)))
            {
                TagHelpers = serializer.Deserialize<IReadOnlyList<TagHelperDescriptor>>(reader);
                TagHelperHashes = TagHelpers.Select(th => th.GetHashCode()).ToList();
            }

            // Set cache size to 400 so anything more then that will force compacts
            Cache = new MemoryCache<int, TagHelperDescriptor>(400);
        }

        private IReadOnlyList<int> TagHelperHashes { get; }

        private IReadOnlyList<TagHelperDescriptor> TagHelpers { get; }

        private MemoryCache<int, TagHelperDescriptor> Cache { get; }

        [Benchmark(Description = "MemoryCache Set performance with limited size")]
        public void Set_Performance()
        {
            for (var i = 0; i < TagHelpers.Count; i++)
            {
                Cache.Set(TagHelperHashes[i], TagHelpers[i]);
            }
        }
    }
}
