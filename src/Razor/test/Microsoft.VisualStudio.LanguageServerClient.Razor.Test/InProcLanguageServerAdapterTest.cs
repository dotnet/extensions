// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Xunit;
using ClientFormattingOptions = Microsoft.VisualStudio.LanguageServer.Protocol.FormattingOptions;
using ClientPosition = Microsoft.VisualStudio.LanguageServer.Protocol.Position;
using ClientRange = Microsoft.VisualStudio.LanguageServer.Protocol.Range;
using ClientRazorLanguageKind = Microsoft.VisualStudio.LanguageServerClient.Razor.RazorLanguageKind;
using ClientRazorLanguageQueryParams = Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp.RazorLanguageQueryParams;
using ClientRazorMapToDocumentEditsParams = Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp.RazorMapToDocumentEditsParams;
using ClientRazorMapToDocumentRangesParams = Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp.RazorMapToDocumentRangesParams;
using ClientTextEdit = Microsoft.VisualStudio.LanguageServer.Protocol.TextEdit;
using ServerPosition = OmniSharp.Extensions.LanguageServer.Protocol.Models.Position;
using ServerRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using ServerRazorLanguageKind = Microsoft.AspNetCore.Razor.LanguageServer.RazorLanguageKind;
using ServerRazorLanguageQueryResponse = Microsoft.AspNetCore.Razor.LanguageServer.RazorLanguageQueryResponse;
using ServerRazorMapToDocumentEditsResponse = Microsoft.AspNetCore.Razor.LanguageServer.RazorMapToDocumentEditsResponse;
using ServerRazorMapToDocumentRangesResponse = Microsoft.AspNetCore.Razor.LanguageServer.RazorMapToDocumentRangesResponse;
using ServerTextEdit = OmniSharp.Extensions.LanguageServer.Protocol.Models.TextEdit;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    public class InProcLanguageServerAdapterTest
    {
        private static ServerPosition TestServerPosition1 { get; } = new ServerPosition(1, 23);

        private static ServerPosition TestServerPosition2 { get; } = new ServerPosition(4, 56);

        private static ServerRange TestServerRange1 { get; } = new ServerRange()
        {
            Start = TestServerPosition1,
            End = TestServerPosition1,
        };

        private static ServerRange TestServerRange2 { get; } = new ServerRange()
        {
            Start = TestServerPosition2,
            End = TestServerPosition2,
        };

        private static ServerTextEdit TestServerTextEdit1 { get; } = new ServerTextEdit()
        {
            NewText = "test1",
            Range = TestServerRange1,
        };

        private static ServerTextEdit TestServerTextEdit2 { get; } = new ServerTextEdit()
        {
            NewText = "test2",
            Range = TestServerRange2,
        };

        private static ClientPosition TestClientPosition1 { get; } = new ClientPosition(7, 89);

        private static ClientPosition TestClientPosition2 { get; } = new ClientPosition(10, 1112);

        private static ClientRange TestClientRange1 { get; } = new ClientRange()
        {
            Start = TestClientPosition1,
            End = TestClientPosition1,
        };

        private static ClientRange TestClientRange2 { get; } = new ClientRange()
        {
            Start = TestClientPosition2,
            End = TestClientPosition2,
        };

        private static ClientTextEdit TestClientTextEdit1 { get; } = new ClientTextEdit()
        {
            NewText = "test1",
            Range = TestClientRange1,
        };

        private static ClientTextEdit TestClientTextEdit2 { get; } = new ClientTextEdit()
        {
            NewText = "test2",
            Range = TestClientRange2,
        };

        private static Uri TestUri { get; } = new Uri("C:/path/to/file.razor");

        [Fact]
        public void ConvertToClient_EditResponse()
        {
            // Arrange
            var originalobject = new ServerRazorMapToDocumentEditsResponse()
            {
                HostDocumentVersion = 123,
                TextEdits = new[] { TestServerTextEdit1, TestServerTextEdit2 }
            };

            // Act
            var convertedObject = InProcLanguageServerAdapter.ConvertToClient(originalobject);

            // Assert
            AssertConversionIsEqual(originalobject, convertedObject);
        }

        [Fact]
        public void ConvertToServer_EditParams()
        {
            // Arrange
            var originalobject = new ClientRazorMapToDocumentEditsParams()
            {
                Kind = ClientRazorLanguageKind.Html,
                TextEditKind = HtmlCSharp.TextEditKind.Snippet,
                RazorDocumentUri = TestUri,
                FormattingOptions = new ClientFormattingOptions()
                {
                    InsertSpaces = true,
                    TabSize = 7,
                    OtherOptions = new Dictionary<string, object>()
                    {
                        ["key"] = "value",
                    },
                },
                ProjectedTextEdits = new[] { TestClientTextEdit1, TestClientTextEdit2 },
            };

            // Act
            var convertedObject = InProcLanguageServerAdapter.ConvertToServer(originalobject);

            // Assert
            AssertConversionIsEqual(originalobject, convertedObject);
        }

        [Fact]
        public void ConvertToClient_MapRangesResponse()
        {
            // Arrange
            var originalobject = new ServerRazorMapToDocumentRangesResponse()
            {
                Ranges = new[] { TestServerRange1, TestServerRange2 },
                HostDocumentVersion = 1234567,
            };

            // Act
            var convertedObject = InProcLanguageServerAdapter.ConvertToClient(originalobject);

            // Assert
            AssertConversionIsEqual(originalobject, convertedObject);
        }

        [Fact]
        public void ConvertToServer_MapRangesParams()
        {
            // Arrange
            var originalobject = new ClientRazorMapToDocumentRangesParams()
            {
                Kind = ClientRazorLanguageKind.CSharp,
                MappingBehavior = LanguageServerMappingBehavior.Inclusive,
                ProjectedRanges = new[] { TestClientRange1, TestClientRange2 },
                RazorDocumentUri = TestUri,
            };

            // Act
            var convertedObject = InProcLanguageServerAdapter.ConvertToServer(originalobject);

            // Assert
            AssertConversionIsEqual(originalobject, convertedObject);
        }

        [Fact]
        public void ConvertToClient_LanguageQueryResponse()
        {
            // Arrange
            var originalobject = new ServerRazorLanguageQueryResponse()
            {
                HostDocumentVersion = 1337,
                Kind = ServerRazorLanguageKind.Html,
                Position = TestServerPosition2,
                PositionIndex = 7890,
            };

            // Act
            var convertedObject = InProcLanguageServerAdapter.ConvertToClient(originalobject);

            // Assert
            AssertConversionIsEqual(originalobject, convertedObject);
        }

        [Fact]
        public void ConvertToServer_LanguageQueryParams()
        {
            // Arrange
            var originalobject = new ClientRazorLanguageQueryParams()
            {
                Position = TestClientPosition1,
                Uri = TestUri,
            };

            // Act
            var convertedObject = InProcLanguageServerAdapter.ConvertToServer(originalobject);

            // Assert
            AssertConversionIsEqual(originalobject, convertedObject);
        }

        private void AssertConversionIsEqual<TOriginalType>(TOriginalType original, object converted)
        {
            var serializedOriginal = JsonConvert.SerializeObject(original);
            var serializedConversion = JsonConvert.SerializeObject(converted);

            // At this point we can't just compare serializedOriginal to serializedConversion because the data types may serialize slightly differently but may still in fact be equivalent.
            // For instance, some may render `null` some may not, some may render in camelCase some may not. To ignore these differences we re-serialize the converted type into the original
            // type so we can do a string comparison.
            var deserializedConversionToOriginal = JsonConvert.DeserializeObject<TOriginalType>(serializedConversion);
            var reserializedOriginalAfterConvert = JsonConvert.SerializeObject(deserializedConversionToOriginal);

            Assert.Equal(serializedOriginal, reserializedOriginalAfterConvert);
        }
    }
}
