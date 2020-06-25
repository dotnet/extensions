// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Serialization;
using Microsoft.VisualStudio.LanguageServices.Razor.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.Razor.Performance
{
    public class TagHelperSerializationBenchmark
    {
        private readonly byte[] _tagHelperBuffer;

        public TagHelperSerializationBenchmark()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null && !File.Exists(Path.Combine(current.FullName, "taghelpers.json")))
            {
                current = current.Parent;
            }

            var tagHelperFilePath = Path.Combine(current.FullName, "taghelpers.json");
            _tagHelperBuffer = File.ReadAllBytes(tagHelperFilePath);

            // Deserialize from json file.
            DefaultSerializer = new JsonSerializer();
            DefaultSerializer.Converters.Add(new TagHelperDescriptorJsonConverter());
            using (var stream = new MemoryStream(_tagHelperBuffer))
            using (var reader = new JsonTextReader(new StreamReader(stream)))
            {
                DefaultTagHelpers = DefaultSerializer.Deserialize<IReadOnlyList<TagHelperDescriptor>>(reader);
            }
        }

        public IReadOnlyList<TagHelperDescriptor> DefaultTagHelpers { get; set; }

        public JsonSerializer DefaultSerializer { get; set; }

        [Benchmark(Description = "Razor TagHelper Roundtrip Serialization")]
        public void TagHelper_Serialization_RoundTrip()
        {
            // Serialize back to json.
            MemoryStream originalStream;
            using (originalStream = new MemoryStream())
            using (var writer = new StreamWriter(originalStream, Encoding.UTF8, bufferSize: 4096))
            {
                DefaultSerializer.Serialize(writer, DefaultTagHelpers);
            }

            IReadOnlyList<TagHelperDescriptor> reDeserializedTagHelpers;
            var stream = new MemoryStream(originalStream.GetBuffer());
            using (stream)
            using (var reader = new JsonTextReader(new StreamReader(stream)))
            {
                reDeserializedTagHelpers = DefaultSerializer.Deserialize<IReadOnlyList<TagHelperDescriptor>>(reader);
            }
        }

        [Benchmark(Description = "Razor TagHelper Serialization")]
        public void TagHelper_Serialization()
        {
            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 4096))
            {
                DefaultSerializer.Serialize(writer, DefaultTagHelpers);
            }
        }

        [Benchmark(Description = "Razor TagHelper Deserialization")]
        public void TagHelper_Deserialization()
        {
            // Deserialize from json file.
            IReadOnlyList<TagHelperDescriptor> tagHelpers;
            using (var stream = new MemoryStream(_tagHelperBuffer))
            using (var reader = new JsonTextReader(new StreamReader(stream)))
            {
                tagHelpers = DefaultSerializer.Deserialize<IReadOnlyList<TagHelperDescriptor>>(reader);
            }
        }
    }
}
