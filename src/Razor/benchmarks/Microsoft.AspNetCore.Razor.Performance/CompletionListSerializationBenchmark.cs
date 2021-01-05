// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Completion;
using Microsoft.CodeAnalysis.Razor.Serialization;
using Microsoft.VisualStudio.Editor.Razor;
using Newtonsoft.Json;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Serialization;
using LSPTagHelperCompletionService = Microsoft.AspNetCore.Razor.LanguageServer.Completion.DefaultTagHelperCompletionService;
using RazorTagHelperCompletionService = Microsoft.VisualStudio.Editor.Razor.DefaultTagHelperCompletionService;

namespace Microsoft.AspNetCore.Razor.Performance
{
    public class CompletionListSerializationBenchmark : TagHelperBenchmarkBase
    {
        private readonly byte[] _completionListBuffer;

        public CompletionListSerializationBenchmark()
        {
            var tagHelperFactsService = new DefaultTagHelperFactsService();
            var completionService = new RazorTagHelperCompletionService(tagHelperFactsService);
            var htmlFactsService = new DefaultHtmlFactsService();
            var componentCompletionService = new LSPTagHelperCompletionService(completionService, htmlFactsService, tagHelperFactsService);

            var documentContent = "<";
            var queryIndex = 1;
            CompletionList = GenerateCompletionList(documentContent, queryIndex, componentCompletionService);
            _completionListBuffer = GenerateBuffer(CompletionList);

            Serializer.Instance.JsonSerializer.Converters.Add(TagHelperDescriptorJsonConverter.Instance);
        }

        private CompletionList CompletionList { get; }

        [Benchmark(Description = "Component Completion List Roundtrip Serialization")]
        public void ComponentElement_CompletionList_Serialization_RoundTrip()
        {
            // Serialize back to json.
            MemoryStream originalStream;
            using (originalStream = new MemoryStream())
            using (var writer = new StreamWriter(originalStream, Encoding.UTF8, bufferSize: 4096))
            {
                Serializer.Instance.JsonSerializer.Serialize(writer, CompletionList);
            }

            CompletionList deserializedCompletions;
            var stream = new MemoryStream(originalStream.GetBuffer());
            using (stream)
            using (var reader = new JsonTextReader(new StreamReader(stream)))
            {
                deserializedCompletions = Serializer.Instance.JsonSerializer.Deserialize<CompletionList>(reader);
            }
        }

        [Benchmark(Description = "Component Completion List Serialization")]
        public void ComponentElement_CompletionList_Serialization()
        {
            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 4096);
            Serializer.Instance.JsonSerializer.Serialize(writer, CompletionList);
        }

        [Benchmark(Description = "Component Completion List Deserialization")]
        public void ComponentElement_CompletionList_Deserialization()
        {
            // Deserialize from json file.
            using var stream = new MemoryStream(_completionListBuffer);
            using var reader = new JsonTextReader(new StreamReader(stream));
            CompletionList deserializedCompletions;
            deserializedCompletions = Serializer.Instance.JsonSerializer.Deserialize<CompletionList>(reader);
        }

        private CompletionList GenerateCompletionList(string documentContent, int queryIndex, LSPTagHelperCompletionService componentCompletionService)
        {
            var sourceDocument = RazorSourceDocument.Create(documentContent, RazorSourceDocumentProperties.Default);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);

            var syntaxTree = RazorSyntaxTree.Parse(sourceDocument);
            codeDocument.SetSyntaxTree(syntaxTree);

            var tagHelperDocumentContext = TagHelperDocumentContext.Create(prefix: string.Empty, DefaultTagHelpers);
            codeDocument.SetTagHelperContext(tagHelperDocumentContext);

            var completionQueryLocation = new SourceSpan(queryIndex, length: 0);
            var completionItems = componentCompletionService.GetCompletionsAt(completionQueryLocation, codeDocument);
            var completionList = RazorCompletionEndpoint.CreateLSPCompletionList(completionItems);
            return completionList;
        }

        private static byte[] GenerateBuffer(CompletionList completionList)
        {
            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 4096);
            Serializer.Instance.JsonSerializer.Serialize(writer, completionList);
            var buffer = stream.GetBuffer();

            return buffer;
        }
    }
}
