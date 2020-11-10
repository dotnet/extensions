// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Workspaces.Serialization;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Razor.Performance
{
    public class FullProjectSnapshotHandleSerializationBenchmark
    {
        // Hardcoded expectations from `ProjectSystem\project.razor.json`
        private static readonly string ExpectedFilePath = "C:\\Users\\admin\\location\\blazorserver\\blazorserver.csproj";
        private static readonly int ExpectedTagHelperCount = 228;

        private JsonSerializer Serializer { get; set; }
        private JsonReader Reader { get; set; }
        private byte[] FullProjectSnapshotBuffer { get; set; }

        [IterationSetup]
        public void Setup()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null && !File.Exists(Path.Combine(current.FullName, "ProjectSystem", "project.razor.json")))
            {
                current = current.Parent;
            }

            var fullProjectSnapshotFilePath = Path.Combine(current.FullName, "ProjectSystem", "project.razor.json");
            FullProjectSnapshotBuffer = File.ReadAllBytes(fullProjectSnapshotFilePath);

            Serializer = new JsonSerializer();
            Serializer.Converters.RegisterRazorConverters();
            Serializer.Converters.Add(FullProjectSnapshotHandleJsonConverter.Instance);
        }

        [Benchmark(Description = "Razor FullProjectSnapshotHandle Roundtrip JsonConverter Serialization")]
        public void TagHelper_JsonConvert_Serialization_RoundTrip()
        {
            var stream = new MemoryStream(FullProjectSnapshotBuffer);
            Reader = new JsonTextReader(new StreamReader(stream));

            Reader.Read();

            var res = FullProjectSnapshotHandleJsonConverter.Instance.ReadJson(Reader, typeof(FullProjectSnapshotHandle), null, Serializer) as FullProjectSnapshotHandle;

            if (res.FilePath != ExpectedFilePath ||
                res.ProjectWorkspaceState.TagHelpers.Count != ExpectedTagHelperCount)
            {
                throw new InvalidDataException();
            }
        }
    }
}
