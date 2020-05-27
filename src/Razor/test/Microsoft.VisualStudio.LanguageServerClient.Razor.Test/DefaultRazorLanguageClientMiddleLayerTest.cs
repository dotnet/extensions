// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    public class DefaultRazorLanguageClientMiddleLayerTest
    {
        public DefaultRazorLanguageClientMiddleLayerTest()
        {
            Uri = new Uri("C:/path/to/file.razor");
        }

        private Uri Uri { get; }


        [Fact]
        public async Task OnTypeFormattingRequest_InterceptsRequestAndResponseCorrectly()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>());

            var appliedTextEdits = false;
            var editorService = new Mock<LSPEditorService>(MockBehavior.Strict);
            editorService
                .Setup(e => e.ApplyTextEditsAsync(Uri, It.IsAny<ITextSnapshot>(), It.IsAny<IEnumerable<TextEdit>>()))
                .Callback<Uri, ITextSnapshot, IEnumerable<TextEdit>>((uri, snapshot, edits) =>
                {
                    var edit = Assert.Single(edits);
                    Assert.Equal("SampleEdit", edit.NewText);
                    appliedTextEdits = true;
                })
                .Returns(Task.CompletedTask);

            var inputParams = new DocumentOnTypeFormattingParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Options = new FormattingOptions(),
            };
            var actualTextEdits = JToken.FromObject(new[] { new TextEdit() { NewText = "SampleEdit" } });
            Func<JToken, Task<JToken>> actualRequest = token =>
            {
                var request = token.ToObject<DocumentOnTypeFormattingParams>();
                Assert.True(request.Options.OtherOptions.ContainsKey(LanguageServerConstants.ExpectsCursorPlaceholderKey));

                return Task.FromResult(actualTextEdits);
            };

            var middleLayer = new DefaultRazorLanguageClientMiddleLayer(documentManager, editorService.Object);

            // Act
            await middleLayer.HandleRequestAsync(Methods.TextDocumentOnTypeFormattingName, JToken.FromObject(inputParams), actualRequest).ConfigureAwait(false);

            // Assert
            Assert.True(appliedTextEdits);
        }

        private class TestDocumentManager : LSPDocumentManager
        {
            private readonly Dictionary<Uri, LSPDocumentSnapshot> _documents = new Dictionary<Uri, LSPDocumentSnapshot>();

            public override event EventHandler<LSPDocumentChangeEventArgs> Changed;

            public override bool TryGetDocument(Uri uri, out LSPDocumentSnapshot lspDocumentSnapshot)
            {
                return _documents.TryGetValue(uri, out lspDocumentSnapshot);
            }

            public void AddDocument(Uri uri, LSPDocumentSnapshot documentSnapshot)
            {
                _documents.Add(uri, documentSnapshot);

                Changed?.Invoke(this, null);
            }
        }
    }
}
