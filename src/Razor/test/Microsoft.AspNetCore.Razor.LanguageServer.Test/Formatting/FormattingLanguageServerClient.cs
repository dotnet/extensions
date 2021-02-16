// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.Test;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using Microsoft.WebTools.Languages.Shared.ContentTypes;
using Microsoft.WebTools.Languages.Shared.Editor.Text;
using Microsoft.WebTools.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Progress;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.WorkDone;
using Xunit;
using FormattingOptions = OmniSharp.Extensions.LanguageServer.Protocol.Models.FormattingOptions;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal class FormattingLanguageServerClient : ClientNotifierServiceBase
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

        public IProgressManager ProgressManager => throw new NotImplementedException();

        public IServerWorkDoneManager WorkDoneManager => throw new NotImplementedException();

        public ILanguageServerConfiguration Configuration => throw new NotImplementedException();

        public override InitializeParams ClientSettings
        {
            get
            {
                throw new NotImplementedException();
            }
        }

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
                var codeDocument = _documents[@params.HostDocumentFilePath];
                var csharpSourceText = codeDocument.GetCSharpSourceText();
                var csharpDocument = GetCSharpDocument(codeDocument, @params.Options);
                if (!csharpDocument.TryGetSyntaxRoot(out var root))
                {
                    throw new InvalidOperationException("Couldn't get syntax root.");
                }
                var spanToFormat = @params.ProjectedRange.AsTextSpan(csharpSourceText);

                var changes = Formatter.GetFormattedTextChanges(root, spanToFormat, csharpDocument.Project.Solution.Workspace);

                response.Edits = changes.Select(c => c.AsTextEdit(csharpSourceText)).ToArray();
            }
            else if (@params.Kind == RazorLanguageKind.Html)
            {
                response.Edits = Array.Empty<TextEdit>();

                var codeDocument = _documents[@params.HostDocumentFilePath];
                var generatedHtml = codeDocument.GetHtmlDocument().GeneratedHtml;
                generatedHtml = generatedHtml.Replace("\r", "", StringComparison.Ordinal).Replace("\n", "\r\n", StringComparison.Ordinal);
                var generatedHtmlSource = SourceText.From(generatedHtml, Encoding.UTF8);

                var editHandlerAssembly = Assembly.Load("Microsoft.WebTools.Languages.LanguageServer.Server, Version=16.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
                var editHandlerType = editHandlerAssembly.GetType("Microsoft.WebTools.Languages.LanguageServer.Server.Html.OperationHandlers.ApplyFormatEditsHandler", throwOnError: true);
                var bufferManagerType = editHandlerAssembly.GetType("Microsoft.WebTools.Languages.LanguageServer.Server.Shared.Buffer.BufferManager", throwOnError: true);

                var exportProvider = EditorTestCompositions.Editor.ExportProviderFactory.CreateExportProvider();
                var contentTypeService = exportProvider.GetExportedValue<IContentTypeRegistryService>();

                contentTypeService.AddContentType(HtmlContentTypeDefinition.HtmlContentType, new[] { StandardContentTypeNames.Text });

                var textBufferFactoryService = exportProvider.GetExportedValue<ITextBufferFactoryService>();
                var textBufferListeners = Array.Empty<Lazy<IWebTextBufferListener, IOrderedComponentContentTypes>>();
                var bufferManager = Activator.CreateInstance(bufferManagerType, new object[] { contentTypeService, textBufferFactoryService, textBufferListeners });
                var joinableTaskFactoryThreadSwitcher = typeof(IdAttribute).Assembly.GetType("Microsoft.WebTools.Shared.Threading.JoinableTaskFactoryThreadSwitcher", throwOnError: true);
                var threadSwitcher = (IThreadSwitcher)Activator.CreateInstance(joinableTaskFactoryThreadSwitcher, new object[] { new JoinableTaskContext().Factory });
                var applyFormatEditsHandler = Activator.CreateInstance(editHandlerType, new object[] { bufferManager, threadSwitcher, textBufferFactoryService });

                // Make sure the buffer manager knows about the source document
                var documentUri = DocumentUri.From($"file:///{@params.HostDocumentFilePath}");
                var contentTypeName = HtmlContentTypeDefinition.HtmlContentType;
                var initialContent = generatedHtml;
                var snapshotVersionFromLSP = 0;
                Assert.IsAssignableFrom<ITextSnapshot>(bufferManager.GetType().GetMethod("CreateBuffer").Invoke(bufferManager, new object[] { documentUri, contentTypeName, initialContent, snapshotVersionFromLSP }));

                var requestType = editHandlerAssembly.GetType("Microsoft.WebTools.Languages.LanguageServer.Server.ContainedLanguage.ApplyFormatEditsParamForOmniSharp", throwOnError: true);
                var serializedValue = $@"{{
    ""Options"": {{
        ""UseSpaces"": {(@params.Options.InsertSpaces ? "true" : "false")},
        ""TabSize"": {@params.Options.TabSize},
        ""IndentSize"": {@params.Options.TabSize}
    }},
    ""SpanToFormat"": {{
        ""start"": {@params.ProjectedRange.AsTextSpan(generatedHtmlSource).Start},
        ""length"": {@params.ProjectedRange.AsTextSpan(generatedHtmlSource).Length},
    }},
    ""Uri"": ""file:///{@params.HostDocumentFilePath}"",
    ""GeneratedChanges"": [
    ]
}}
";
                var request = JsonConvert.DeserializeObject(serializedValue, requestType);
                var resultTask = (Task)applyFormatEditsHandler.GetType().GetRuntimeMethod("Handle", new Type[] { requestType, typeof(CancellationToken) }).Invoke(applyFormatEditsHandler, new object[] { request, CancellationToken.None });
                var result = resultTask.GetType().GetProperty(nameof(Task<int>.Result)).GetValue(resultTask);
                var rawTextChanges = result.GetType().GetProperty("TextChanges").GetValue(result);
                var serializedTextChanges = JsonConvert.SerializeObject(rawTextChanges, Newtonsoft.Json.Formatting.Indented);
                var textChanges = JsonConvert.DeserializeObject<HtmlFormatterTextEdit[]>(serializedTextChanges);
                response.Edits = textChanges.Select(change => change.AsTextEdit(SourceText.From(generatedHtml))).ToArray();
            }

            return response;
        }

        private struct HtmlFormatterTextEdit
        {
#pragma warning disable CS0649 // Field 'name' is never assigned to, and will always have its default value
            public int Position;
            public int Length;
            public string NewText;
#pragma warning restore CS0649 // Field 'name' is never assigned to, and will always have its default value

            public TextEdit AsTextEdit(SourceText sourceText)
            {
                var startLinePosition = sourceText.Lines.GetLinePosition(Position);
                var endLinePosition = sourceText.Lines.GetLinePosition(Position + Length);

                return new TextEdit
                {
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range()
                    {
                        Start = new Position(startLinePosition.Line, startLinePosition.Character),
                        End = new Position(endLinePosition.Line, endLinePosition.Character),
                    },
                    NewText = NewText,
                };
            }
        }

        private static Document GetCSharpDocument(RazorCodeDocument codeDocument, FormattingOptions options)
        {
            var adhocWorkspace = new AdhocWorkspace();
            var csharpOptions = adhocWorkspace.Options
                .WithChangedOption(CodeAnalysis.Formatting.FormattingOptions.TabSize, LanguageNames.CSharp, (int)options.TabSize)
                .WithChangedOption(CodeAnalysis.Formatting.FormattingOptions.IndentationSize, LanguageNames.CSharp, (int)options.TabSize)
                .WithChangedOption(CodeAnalysis.Formatting.FormattingOptions.UseTabs, LanguageNames.CSharp, !options.InsertSpaces);
            adhocWorkspace.TryApplyChanges(adhocWorkspace.CurrentSolution.WithOptions(csharpOptions));

            var project = adhocWorkspace.AddProject("TestProject", LanguageNames.CSharp);
            var csharpSourceText = codeDocument.GetCSharpSourceText();
            var csharpDocument = adhocWorkspace.AddDocument(project.Id, "TestDocument", csharpSourceText);
            return csharpDocument;
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

        public override Task<IResponseRouterReturns> SendRequestAsync<T>(string method, T @params)
        {
            if (!(@params is RazorDocumentRangeFormattingParams formattingParams) ||
                !string.Equals(method, "razor/rangeFormatting", StringComparison.Ordinal))
            {
                throw new NotImplementedException();
            }

            var response = Format(formattingParams);

            return Task.FromResult(Convert<RazorDocumentRangeFormattingResponse>(response));
        }

        public override Task<IResponseRouterReturns> SendRequestAsync(string method)
        {
            throw new NotImplementedException();
        }

        public Task<TResponse> SendRequestAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
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

        public override Task OnStarted(ILanguageServer server, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
