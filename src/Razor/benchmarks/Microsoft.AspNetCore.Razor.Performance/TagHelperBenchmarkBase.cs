// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Serialization;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Razor.Performance
{
    public abstract class TagHelperBenchmarkBase
    {
        protected readonly byte[] _tagHelperBuffer;

        public TagHelperBenchmarkBase()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null && !File.Exists(Path.Combine(current.FullName, "taghelpers.json")))
            {
                current = current.Parent;
            }

            var tagHelperFilePath = Path.Combine(current.FullName, "taghelpers.json");
            _tagHelperBuffer = File.ReadAllBytes(tagHelperFilePath);

            // Deserialize from json file.
            TagHelperDescriptorJsonConverter.DisableCachingForTesting = true;
            DefaultSerializer = new JsonSerializer();
            DefaultSerializer.Converters.Add(TagHelperDescriptorJsonConverter.Instance);

            using var stream = new MemoryStream(_tagHelperBuffer);
            using var reader = new JsonTextReader(new StreamReader(stream));
            DefaultTagHelpers = DefaultSerializer.Deserialize<IReadOnlyList<TagHelperDescriptor>>(reader);
            TagHelperDescriptorJsonConverter.DisableCachingForTesting = false;
        }

        protected IReadOnlyList<TagHelperDescriptor> DefaultTagHelpers { get; set; }

        protected JsonSerializer DefaultSerializer { get; set; }
    }
}
