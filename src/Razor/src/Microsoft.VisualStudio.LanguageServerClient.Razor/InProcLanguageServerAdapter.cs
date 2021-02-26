// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.AspNetCore.Razor.LanguageServer;
using Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp;
using ClientFormattingOptions = Microsoft.VisualStudio.LanguageServer.Protocol.FormattingOptions;
using ClientPosition = Microsoft.VisualStudio.LanguageServer.Protocol.Position;
using ClientRange = Microsoft.VisualStudio.LanguageServer.Protocol.Range;
using ClientRazorLanguageKind = Microsoft.VisualStudio.LanguageServerClient.Razor.RazorLanguageKind;
using ClientRazorLanguageQueryParams = Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp.RazorLanguageQueryParams;
using ClientRazorLanguageQueryResponse = Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp.RazorLanguageQueryResponse;
using ClientRazorMapToDocumentEditsParams = Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp.RazorMapToDocumentEditsParams;
using ClientRazorMapToDocumentEditsResponse = Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp.RazorMapToDocumentEditsResponse;
using ClientRazorMapToDocumentRangesParams = Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp.RazorMapToDocumentRangesParams;
using ClientRazorMapToDocumentRangesResponse = Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp.RazorMapToDocumentRangesResponse;
using ClientTextEdit = Microsoft.VisualStudio.LanguageServer.Protocol.TextEdit;
using ServerRazorLanguageQueryResponse = Microsoft.AspNetCore.Razor.LanguageServer.RazorLanguageQueryResponse;
using ServerFormattingOptions = OmniSharp.Extensions.LanguageServer.Protocol.Models.FormattingOptions;
using ServerMappingBehavior = Microsoft.AspNetCore.Razor.LanguageServer.MappingBehavior;
using ServerPosition = OmniSharp.Extensions.LanguageServer.Protocol.Models.Position;
using ServerRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using ServerRazorLanguageKind = Microsoft.AspNetCore.Razor.LanguageServer.RazorLanguageKind;
using ServerRazorLanguageQueryParams = Microsoft.AspNetCore.Razor.LanguageServer.RazorLanguageQueryParams;
using ServerRazorMapToDocumentEditsParams = Microsoft.AspNetCore.Razor.LanguageServer.RazorMapToDocumentEditsParams;
using ServerRazorMapToDocumentEditsResponse = Microsoft.AspNetCore.Razor.LanguageServer.RazorMapToDocumentEditsResponse;
using ServerRazorMapToDocumentRangesParams = Microsoft.AspNetCore.Razor.LanguageServer.RazorMapToDocumentRangesParams;
using ServerRazorMapToDocumentRangesResponse = Microsoft.AspNetCore.Razor.LanguageServer.RazorMapToDocumentRangesResponse;
using ServerTextEdit = OmniSharp.Extensions.LanguageServer.Protocol.Models.TextEdit;
using ServerTextEditKind = Microsoft.AspNetCore.Razor.LanguageServer.TextEditKind;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    // This class is more or less a "hack". Its intent is to bridge the end-user experience gap until we're able to re-design some of the Razor language server components.
    // Ultimately this "inproc" adapter enables us to perform common Razor language interactions without going over StreamJsonRpc. This is important because StreamJsonRpc
    // has its own thread/synchornization model (that we don't care about here) that frequently gets blocked and requires constant serialization/deserialization which is
    // expensive. After some spot-testing StreamJsonRpc requests in real scenarios they were taking anywhere from 0-100ms to complete typically averaging out at round 40ms.
    //
    // The "re-design" i'm referring to here would involve us taking our virtual document model that currently resides in-proc in Visual Studio and shifting that understanding
    // into the Razor Language Server where it has all the remapping logic in-proc. This means the Razor language server would ultimately own the remapping behavior of C#/HTML
    // by doing server -> client requests for re-invocations. We do this already for things like light bulbs, semantic tokens and OnTypeFormatting; the intent here would be to
    // expand that surface area to all requests and build a document synchronization model for virtual documents that could exist on any platform.
    [Export(typeof(InProcLanguageServerAdapter))]
    internal class InProcLanguageServerAdapter
    {
        private readonly DefaultLSPProjectionProvider _projectionProvider;
        private readonly DefaultLSPDocumentMappingProvider _mappingProvider;

        [ImportingConstructor]
        public InProcLanguageServerAdapter(
            [Import(typeof(LSPProjectionProvider))] DefaultLSPProjectionProvider projectionProvider,
            [Import(typeof(LSPDocumentMappingProvider))] DefaultLSPDocumentMappingProvider mappingProvider)
        {
            if (projectionProvider is null)
            {
                throw new ArgumentNullException(nameof(projectionProvider));
            }

            if (mappingProvider is null)
            {
                throw new ArgumentNullException(nameof(mappingProvider));
            }

            _projectionProvider = projectionProvider;
            _mappingProvider = mappingProvider;
        }

        public void Bind(RazorLanguageServer server)
        {
            if (server is null)
            {
                throw new ArgumentNullException(nameof(server));
            }

#pragma warning disable CS0618 // Type or member is obsolete
            var languageEndpoint = server.GetLanguageEndpoint();
#pragma warning restore CS0618 // Type or member is obsolete

            _projectionProvider.UseInProcLanguageQueries(async (request, cancellationToken) =>
            {
                var serverRequest = ConvertToServer(request);
                var serverResponse = await languageEndpoint.Handle(serverRequest, cancellationToken).ConfigureAwait(false);
                var clientResponse = ConvertToClient(serverResponse);

                return clientResponse;
            });

            _mappingProvider.UseInProcRangeMapping(async (request, cancellationToken) =>
            {
                var serverRequest = ConvertToServer(request);
                var serverResponse = await languageEndpoint.Handle(serverRequest, cancellationToken).ConfigureAwait(false);
                var clientResponse = ConvertToClient(serverResponse);

                return clientResponse;
            });

            _mappingProvider.UseInProcEditMapping(async (request, cancellationToken) =>
            {
                var serverRequest = ConvertToServer(request);
                var serverResponse = await languageEndpoint.Handle(serverRequest, cancellationToken).ConfigureAwait(false);
                var clientResponse = ConvertToClient(serverResponse);

                return clientResponse;
            });
        }

        // Internal for testing
        internal static ClientRazorMapToDocumentEditsResponse ConvertToClient(ServerRazorMapToDocumentEditsResponse serverResponse)
        {
            return new ClientRazorMapToDocumentEditsResponse()
            {
                TextEdits = ConvertToClient(serverResponse.TextEdits),
                HostDocumentVersion = (long)serverResponse.HostDocumentVersion,
            };
        }

        // Internal for testing
        internal static ServerRazorMapToDocumentEditsParams ConvertToServer(ClientRazorMapToDocumentEditsParams request)
        {
            return new ServerRazorMapToDocumentEditsParams()
            {
                Kind = (ServerRazorLanguageKind)request.Kind,
                TextEditKind = (ServerTextEditKind)request.TextEditKind,
                RazorDocumentUri = request.RazorDocumentUri,
                FormattingOptions = ConvertToServer(request.FormattingOptions),
                ProjectedTextEdits = ConvertToServer(request.ProjectedTextEdits),
            };
        }

        // Internal for testing
        internal static ClientRazorMapToDocumentRangesResponse ConvertToClient(ServerRazorMapToDocumentRangesResponse serverResponse)
        {
            return new ClientRazorMapToDocumentRangesResponse()
            {
                Ranges = ConvertToClient(serverResponse.Ranges),
                HostDocumentVersion = serverResponse.HostDocumentVersion,
            };
        }

        // Internal for testing
        internal static ServerRazorMapToDocumentRangesParams ConvertToServer(ClientRazorMapToDocumentRangesParams request)
        {
            return new ServerRazorMapToDocumentRangesParams()
            {
                Kind = (ServerRazorLanguageKind)request.Kind,
                MappingBehavior = (ServerMappingBehavior)request.MappingBehavior,
                ProjectedRanges = ConvertToServer(request.ProjectedRanges),
                RazorDocumentUri = request.RazorDocumentUri,
            };
        }

        // Internal for testing
        internal static ClientRazorLanguageQueryResponse ConvertToClient(ServerRazorLanguageQueryResponse serverResponse)
        {
            return new ClientRazorLanguageQueryResponse()
            {
                HostDocumentVersion = serverResponse.HostDocumentVersion,
                Kind = (ClientRazorLanguageKind)serverResponse.Kind,
                Position = ConvertToClient(serverResponse.Position),
                PositionIndex = serverResponse.PositionIndex,
            };
        }

        // Internal for testing
        internal static ServerRazorLanguageQueryParams ConvertToServer(ClientRazorLanguageQueryParams request)
        {
            return new ServerRazorLanguageQueryParams()
            {
                Position = ConvertToServer(request.Position),
                Uri = request.Uri,
            };
        }

        private static ServerPosition ConvertToServer(ClientPosition position)
        {
            if (position == null)
            {
                return null;
            }

            return new ServerPosition(position.Line, position.Character);
        }

        private static ClientPosition ConvertToClient(ServerPosition position)
        {
            if (position == null)
            {
                return null;
            }

            return new ClientPosition
            {
                Character = position.Character,
                Line = position.Line,
            };
        }

        private static ServerRange[] ConvertToServer(ClientRange[] ranges)
        {
            if (ranges == null)
            {
                return null;
            }
            var serverRanges = new ServerRange[ranges.Length];
            for (var i = 0; i < ranges.Length; i++)
            {
                var serverRange = ConvertToServer(ranges[i]);
                serverRanges[i] = serverRange;
            }

            return serverRanges;
        }

        private static ClientRange[] ConvertToClient(ServerRange[] ranges)
        {
            if (ranges == null)
            {
                return null;
            }

            var clientRanges = new ClientRange[ranges.Length];
            for (var i = 0; i < ranges.Length; i++)
            {
                var clientRange = ConvertToClient(ranges[i]);
                clientRanges[i] = clientRange;
            }

            return clientRanges;
        }

        private static ServerRange ConvertToServer(ClientRange range)
        {
            if (range == null)
            {
                return null;
            }

            var serverStart = ConvertToServer(range.Start);
            var serverEnd = ConvertToServer(range.End);
            return new ServerRange(serverStart, serverEnd);
        }

        private static ClientRange ConvertToClient(ServerRange range)
        {
            if (range == null)
            {
                return null;
            }

            var clientStart = ConvertToClient(range.Start);
            var clientEnd = ConvertToClient(range.End);
            return new ClientRange
            {
                Start = clientStart,
                End = clientEnd,
            };
        }

        private static ServerTextEdit[] ConvertToServer(ClientTextEdit[] textEdits)
        {
            if (textEdits == null)
            {
                return null;
            }

            var serverTextEdits = new ServerTextEdit[textEdits.Length];
            for (var i = 0; i < textEdits.Length; i++)
            {
                var clientRange = ConvertToServer(textEdits[i]);
                serverTextEdits[i] = clientRange;
            }

            return serverTextEdits;
        }

        private static ClientTextEdit[] ConvertToClient(ServerTextEdit[] textEdits)
        {
            if (textEdits == null)
            {
                return null;
            }

            var clientRanges = new ClientTextEdit[textEdits.Length];
            for (var i = 0; i < textEdits.Length; i++)
            {
                var clientRange = ConvertToClient(textEdits[i]);
                clientRanges[i] = clientRange;
            }

            return clientRanges;
        }

        private static ServerTextEdit ConvertToServer(ClientTextEdit textEdit)
        {
            if (textEdit == null)
            {
                return null;
            }

            var serverRange = ConvertToServer(textEdit.Range);
            return new ServerTextEdit
            {
                Range = serverRange,
                NewText = textEdit.NewText,
            };
        }

        private static ClientTextEdit ConvertToClient(ServerTextEdit textEdit)
        {
            if (textEdit == null)
            {
                return null;
            }

            var clientRange = ConvertToClient(textEdit.Range);
            return new ClientTextEdit
            {
                Range = clientRange,
                NewText = textEdit.NewText,
            };
        }

        private static ServerFormattingOptions ConvertToServer(ClientFormattingOptions clientOptions)
        {
            if (clientOptions == null)
            {
                return null;
            }

            var serverOptions = new ServerFormattingOptions()
            {
                TabSize = clientOptions.TabSize,
                InsertSpaces = clientOptions.InsertSpaces,
            };

            if (clientOptions.OtherOptions != null)
            {
                foreach (var clientOption in clientOptions.OtherOptions)
                {
                    switch (clientOption.Value)
                    {
                        case string stringValue:
                            serverOptions[clientOption.Key] = stringValue;
                            break;
                        case long numberValue:
                            serverOptions[clientOption.Key] = numberValue;
                            break;
                        case bool boolValue:
                            serverOptions[clientOption.Key] = boolValue;
                            break;
                    }
                }
            }


            return serverOptions;
        }
    }
}
