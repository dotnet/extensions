// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Serialization;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Razor.Performance
{
    public class TagHelperResolutionResultSerializationBenchmark
    {
        public TagHelperResolutionResultSerializationBenchmark()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null && !File.Exists(Path.Combine(current.FullName, "taghelpers.json")))
            {
                current = current.Parent;
            }

            var tagHelperFilePath = Path.Combine(current.FullName, "taghelpers.json");
            var tagHelperBuffer = File.ReadAllBytes(tagHelperFilePath);

            // Deserialize from json file.
            Serializer = new JsonSerializer();
            Serializer.Converters.Add(new TagHelperDescriptorJsonConverter());
            Serializer.Converters.Add(new TagHelperResolutionResultJsonConverter());
            using (var stream = new MemoryStream(tagHelperBuffer))
            using (var reader = new JsonTextReader(new StreamReader(stream)))
            {
                var tagHelpers = Serializer.Deserialize<IReadOnlyList<TagHelperDescriptor>>(reader);
                TagHelperResolutionResult = new TagHelperResolutionResult(tagHelpers, Array.Empty<RazorDiagnostic>());
            }
        }

        public JsonSerializer Serializer { get; set; }
        private TagHelperResolutionResult TagHelperResolutionResult { get; }

        [Benchmark(Description = "Razor TagHelperResolutionResult Roundtrip JsonConverter Serialization")]
        public void TagHelper_JsonConvert_Serialization_RoundTrip()
        {
            MemoryStream originalStream;
            using (originalStream = new MemoryStream())
            using (var writer = new StreamWriter(originalStream, Encoding.UTF8, bufferSize: 4096))
            {
                Serializer.Serialize(writer, TagHelperResolutionResult);
            }

            TagHelperResolutionResult deserializedResult;
            var stream = new MemoryStream(originalStream.GetBuffer());
            using (stream)
            using (var reader = new JsonTextReader(new StreamReader(stream)))
            {
                deserializedResult = Serializer.Deserialize<TagHelperResolutionResult>(reader);
            }
        }
    }
}
