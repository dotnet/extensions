// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
using OmniSharp.Extensions.LanguageServer.Protocol.Progress;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.WorkDone;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    public class FormattingLanguageServerClient : IClientLanguageServer
    {
        private readonly FilePathNormalizer _filePathNormalizer = new FilePathNormalizer();
        private readonly Dictionary<string, RazorCodeDocument> _documents = new Dictionary<string, RazorCodeDocument>();

        public IProgressManager ProgressManager => throw new NotImplementedException();

        public IServerWorkDoneManager WorkDoneManager => throw new NotImplementedException();

        public ILanguageServerConfiguration Configuration => throw new NotImplementedException();

        public OmniSharp.Extensions.LanguageServer.Protocol.Models.InitializeParams ClientSettings => throw new NotImplementedException();

        public OmniSharp.Extensions.LanguageServer.Protocol.Models.InitializeResult ServerSettings => throw new NotImplementedException();

        public void SendNotification(string method) => throw new NotImplementedException();

        public void SendNotification<T>(string method, T @params) => throw new NotImplementedException();

        public void AddCodeDocument(RazorCodeDocument codeDocument)
        {
            var path = _filePathNormalizer.Normalize(codeDocument.Source.FilePath);
            _documents.TryAdd(path, codeDocument);
        }

        private RazorDocumentRangeFormattingResponse Format(RazorDocumentRangeFormattingParams @params)
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
            var root = syntaxTree.GetRoot();
            var spanToFormat = @params.ProjectedRange.AsTextSpan(sourceText);

            var changes = Formatter.GetFormattedTextChanges(root, spanToFormat, workspace, options: cSharpOptions);

            var response = new RazorDocumentRangeFormattingResponse()
            {
                Edits = changes.Select(c => c.AsTextEdit(sourceText)).ToArray()
            };

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

        private static TResponse Convert<T, TResponse>(T instance)
        {
            var parameter = Expression.Parameter(typeof(T));
            var convert = Expression.Convert(parameter, typeof(TResponse));
            var lambda = Expression.Lambda<Func<T, TResponse>>(convert, parameter).Compile();

            return lambda(instance);
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

        (string method, TaskCompletionSource<JToken> pendingTask) IResponseRouter.GetRequest(long id)
        {
            throw new NotImplementedException();
        }

        public object GetService(Type serviceType)
        {
            throw new NotImplementedException();
        }
    }
}
