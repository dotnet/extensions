// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Progress;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.WorkDone;
using Xunit.Sdk;
using FormattingOptions = Microsoft.CodeAnalysis.Formatting.FormattingOptions;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    public class FormattingLanguageServerClient : IClientLanguageServer
    {
        private readonly FilePathNormalizer _filePathNormalizer = new FilePathNormalizer();
        private readonly Dictionary<string, RazorCodeDocument> _documents = new Dictionary<string, RazorCodeDocument>();
        private readonly string _projectPath;
        private readonly string _baselineFileName;

        public FormattingLanguageServerClient(string projectPath, string fileName)
        {
            _projectPath = projectPath;
            _baselineFileName = fileName;
        }

#if GENERATE_BASELINES
        protected bool GenerateBaselines { get; } = true;
#else
        protected bool GenerateBaselines { get; } = false;
#endif

        public IProgressManager ProgressManager => throw new NotImplementedException();

        public IServerWorkDoneManager WorkDoneManager => throw new NotImplementedException();

        public ILanguageServerConfiguration Configuration => throw new NotImplementedException();

        public InitializeParams ClientSettings => throw new NotImplementedException();

        public InitializeResult ServerSettings => throw new NotImplementedException();

        public void SendNotification(string method) => throw new NotImplementedException();

        public void SendNotification<T>(string method, T @params) => throw new NotImplementedException();

        public void AddCodeDocument(RazorCodeDocument codeDocument)
        {
            var path = _filePathNormalizer.Normalize(codeDocument.Source.FilePath);
            _documents.TryAdd(path, codeDocument);
        }

        private RazorDocumentRangeFormattingResponse Format(RazorDocumentRangeFormattingParams @params)
        {
            if (@params.Kind == RazorLanguageKind.Razor)
            {
                throw new InvalidOperationException("We shouldn't be asked to format Razor language kind.");
            }

            var options = @params.Options;
            var response = new RazorDocumentRangeFormattingResponse();

            if (@params.Kind == RazorLanguageKind.CSharp)
            {
                var workspace = new AdhocWorkspace();
                var cSharpOptions = workspace.Options
                    .WithChangedOption(FormattingOptions.TabSize, LanguageNames.CSharp, (int)options.TabSize)
                    .WithChangedOption(FormattingOptions.IndentationSize, LanguageNames.CSharp, (int)options.TabSize)
                    .WithChangedOption(FormattingOptions.UseTabs, LanguageNames.CSharp, !options.InsertSpaces);

                var codeDocument = _documents[@params.HostDocumentFilePath];
                var csharpDocument = codeDocument.GetCSharpDocument();
                var syntaxTree = CSharpSyntaxTree.ParseText(csharpDocument.GeneratedCode);
                var sourceText = SourceText.From(csharpDocument.GeneratedCode);
                var root = syntaxTree.GetRoot();
                var spanToFormat = @params.ProjectedRange.AsTextSpan(sourceText);

                var changes = Formatter.GetFormattedTextChanges(root, spanToFormat, workspace, options: cSharpOptions);

                response.Edits = changes.Select(c => c.AsTextEdit(sourceText)).ToArray();
            }
            else if (@params.Kind == RazorLanguageKind.Html)
            {
                response.Edits = Array.Empty<TextEdit>();

                var codeDocument = _documents[@params.HostDocumentFilePath];
                var generatedHtml = codeDocument.GetHtmlDocument().GeneratedHtml;
                generatedHtml = generatedHtml.Replace("\r", "", StringComparison.Ordinal).Replace("\n", "\r\n", StringComparison.Ordinal);

                // Get formatted baseline file
                var baselineInputFileName = Path.ChangeExtension(_baselineFileName, ".input.html");
                var baselineOutputFileName = Path.ChangeExtension(_baselineFileName, ".output.html");

                var baselineInputFile = TestFile.Create(baselineInputFileName, GetType().GetTypeInfo().Assembly);
                var baselineOutputFile = TestFile.Create(baselineOutputFileName, GetType().GetTypeInfo().Assembly);

                if (GenerateBaselines)
                {
                    if (baselineInputFile.Exists())
                    {
                        // If it already exists, we only want to update if the input is different.
                        var inputContent = baselineInputFile.ReadAllText();
                        if (string.Equals(inputContent, generatedHtml, StringComparison.Ordinal))
                        {
                            return response;
                        }
                    }

                    var baselineInputFilePath = Path.Combine(_projectPath, baselineInputFileName);
                    File.WriteAllText(baselineInputFilePath, generatedHtml);

                    var baselineOutputFilePath = Path.Combine(_projectPath, baselineOutputFileName);
                    File.WriteAllText(baselineOutputFilePath, generatedHtml);

                    return response;
                }

                if (!baselineInputFile.Exists())
                {
                    throw new XunitException($"The resource {baselineInputFileName} was not found.");
                }

                if (!baselineOutputFile.Exists())
                {
                    throw new XunitException($"The resource {baselineOutputFileName} was not found.");
                }

                var baselineInputHtml = baselineInputFile.ReadAllText();
                if (!string.Equals(baselineInputHtml, generatedHtml, StringComparison.Ordinal))
                {
                    throw new XunitException($"The baseline for {_baselineFileName} is out of date.");
                }

                var baselineOutputHtml = baselineOutputFile.ReadAllText();
                var baselineInputText = SourceText.From(baselineInputHtml);
                var baselineOutputText = SourceText.From(baselineOutputHtml);
                var changes = SourceTextDiffer.GetMinimalTextChanges(baselineInputText, baselineOutputText, lineDiffOnly: false);
                var edits = changes.Select(c => c.AsTextEdit(baselineInputText)).ToArray();
                response.Edits = edits;
            }

            return response;
        }

        private class ResponseRouterReturns : IResponseRouterReturns
        {
            private object _response;

            public ResponseRouterReturns(object response)
            {
                _response = response;
            }

            public Task<TResponse> Returning<TResponse>(CancellationToken cancellationToken)
            {
                return Task.FromResult((TResponse)_response);
            }

            public Task ReturningVoid(CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }

        private static IResponseRouterReturns Convert<T>(T instance)
        {
            return new ResponseRouterReturns(instance);
        }

        public void SendNotification(IRequest request)
        {
            throw new NotImplementedException();
        }

        public IResponseRouterReturns SendRequest<T>(string method, T @params)
        {
            if (!(@params is RazorDocumentRangeFormattingParams formattingParams) ||
                !string.Equals(method, "razor/rangeFormatting", StringComparison.Ordinal))
            {
                throw new NotImplementedException();
            }

            var response = Format(formattingParams);

            return Convert<RazorDocumentRangeFormattingResponse>(response);
        }

        public IResponseRouterReturns SendRequest(string method)
        {
            throw new NotImplementedException();
        }

        public Task<TResponse> SendRequest<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public object GetService(Type serviceType)
        {
            throw new NotImplementedException();
        }

        public bool TryGetRequest(long id, [NotNullWhen(true)] out string method, [NotNullWhen(true)] out TaskCompletionSource<JToken> pendingTask)
        {
            throw new NotImplementedException();
        }
    }
}
