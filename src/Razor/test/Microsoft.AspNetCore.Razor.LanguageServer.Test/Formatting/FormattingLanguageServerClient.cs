// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    public class FormattingLanguageServerClient : ILanguageServerClient
    {
        private readonly FilePathNormalizer _filePathNormalizer = new FilePathNormalizer();
        private readonly Dictionary<string, RazorCodeDocument> _documents = new Dictionary<string, RazorCodeDocument>();

        public void SendNotification(string method) => throw new NotImplementedException();

        public void SendNotification<T>(string method, T @params) => throw new NotImplementedException();

        public Task<TResponse> SendRequest<TResponse>(string method) => throw new NotImplementedException();

        public Task SendRequest<T>(string method, T @params) => throw new NotImplementedException();

        public TaskCompletionSource<JToken> GetRequest(long id) => throw new NotImplementedException();

        public void AddCodeDocument(RazorCodeDocument codeDocument)
        {
            var path = _filePathNormalizer.Normalize(codeDocument.Source.FilePath);
            _documents.TryAdd(path, codeDocument);
        }

        async Task<TResponse> IResponseRouter.SendRequest<T, TResponse>(string method, T @params)
        {
            if (!(@params is RazorDocumentRangeFormattingParams formattingParams) ||
                !string.Equals(method, "razor/rangeFormatting", StringComparison.Ordinal))
            {
                throw new NotImplementedException();
            }

            var response = await FormatAsync(formattingParams);

            return Convert<RazorDocumentRangeFormattingResponse, TResponse>(response);
        }

        private async Task<RazorDocumentRangeFormattingResponse> FormatAsync(RazorDocumentRangeFormattingParams @params)
        {
            if (@params.Kind != RazorLanguageKind.CSharp)
            {
                throw new NotImplementedException($"{@params.Kind} formatting is not yet supported.");
            }

            var options = @params.Options;
            var workspace = new AdhocWorkspace();
            var cSharpOptions = workspace.Options
                .WithChangedOption(FormattingOptions.TabSize, LanguageNames.CSharp, (int)options.TabSize)
                .WithChangedOption(FormattingOptions.UseTabs, LanguageNames.CSharp, !options.InsertSpaces);

            var codeDocument = _documents[@params.HostDocumentFilePath];
            var csharpDocument = codeDocument.GetCSharpDocument();
            var syntaxTree = CSharpSyntaxTree.ParseText(csharpDocument.GeneratedCode);
            var sourceText = SourceText.From(csharpDocument.GeneratedCode);
            var root = await syntaxTree.GetRootAsync();
            var spanToFormat = @params.ProjectedRange.AsTextSpan(sourceText);

            var changes = Formatter.GetFormattedTextChanges(root, spanToFormat, workspace, options: cSharpOptions);

            var response = new RazorDocumentRangeFormattingResponse()
            {
                Edits = changes.Select(c => c.AsTextEdit(sourceText)).ToArray()
            };

            return response;
        }

        private static TResponse Convert<T, TResponse>(T instance)
        {
            var parameter = Expression.Parameter(typeof(T));
            var convert = Expression.Convert(parameter, typeof(TResponse));
            var lambda = Expression.Lambda<Func<T, TResponse>>(convert, parameter).Compile();

            return lambda(instance);
        }
    }
}
