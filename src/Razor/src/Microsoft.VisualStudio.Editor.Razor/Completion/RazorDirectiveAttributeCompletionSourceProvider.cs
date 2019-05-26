// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Editor.Razor.Completion
{
    [System.Composition.Shared]
    [Export(typeof(IAsyncCompletionSourceProvider))]
    [Name("Razor directive attribute completion provider.")]
    [ContentType(RazorLanguage.CoreContentType)]
    internal class RazorDirectiveAttributeCompletionSourceProvider : IAsyncCompletionSourceProvider
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly RazorCompletionFactsService _completionFactsService;
        private readonly ICompletionBroker _completionBroker;

        [ImportingConstructor]
        public RazorDirectiveAttributeCompletionSourceProvider(
            ForegroundDispatcher foregroundDispatcher,
            RazorCompletionFactsService completionFactsService,
            IAsyncCompletionBroker asyncCoompletionBroker,
            ICompletionBroker completionBroker)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (completionFactsService == null)
            {
                throw new ArgumentNullException(nameof(completionFactsService));
            }

            if (asyncCoompletionBroker is null)
            {
                throw new ArgumentNullException(nameof(asyncCoompletionBroker));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _completionFactsService = completionFactsService;
            _completionBroker = completionBroker;
        }

        public IAsyncCompletionSource GetOrCreate(ITextView textView)
        {
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            var razorBuffer = textView.BufferGraph.GetRazorBuffers().FirstOrDefault();
            if (!razorBuffer.Properties.TryGetProperty(typeof(RazorDirectiveAttributeCompletionSource), out IAsyncCompletionSource completionSource) ||
                completionSource == null)
            {
                completionSource = CreateCompletionSource(razorBuffer);
                razorBuffer.Properties.AddProperty(typeof(RazorDirectiveAttributeCompletionSource), completionSource);
            }

            return completionSource;
        }

        // Internal for testing
        internal IAsyncCompletionSource CreateCompletionSource(ITextBuffer razorBuffer)
        {
            if (!razorBuffer.Properties.TryGetProperty(typeof(VisualStudioRazorParser), out VisualStudioRazorParser parser))
            {
                // Parser hasn't been associated with the text buffer yet.
                return null;
            }

            var completionSource = new RazorDirectiveAttributeCompletionSource(_foregroundDispatcher, parser, _completionFactsService, _completionBroker);
            return completionSource;
        }
    }
}