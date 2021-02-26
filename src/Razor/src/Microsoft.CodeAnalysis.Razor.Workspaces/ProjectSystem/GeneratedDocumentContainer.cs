// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class GeneratedDocumentContainer
    {
        public event EventHandler<TextChangeEventArgs> GeneratedCSharpChanged;
        public event EventHandler<TextChangeEventArgs> GeneratedHtmlChanged;

        private SourceText _source;
        private VersionStamp? _inputVersion;
        private VersionStamp? _outputCSharpVersion;
        private VersionStamp? _outputHtmlVersion;
        private RazorCSharpDocument _outputCSharp;
        private RazorHtmlDocument _outputHtml;
        private DocumentSnapshot _latestDocument;

        private readonly object _setOutputLock = new object();
        private readonly TextContainer _csharpTextContainer;
        private readonly TextContainer _htmlTextContainer;

        public GeneratedDocumentContainer()
        {
            _csharpTextContainer = new TextContainer(_setOutputLock);
            _csharpTextContainer.TextChanged += CSharpTextContainer_TextChanged;

            _htmlTextContainer = new TextContainer(_setOutputLock);
            _htmlTextContainer.TextChanged += HtmlTextContainer_TextChanged;
        }

        public SourceText Source
        {
            get
            {
                lock (_setOutputLock)
                {
                    return _source;
                }
            }
        }

        public VersionStamp InputVersion
        {
            get
            {
                lock (_setOutputLock)
                {
                    return _inputVersion.Value;
                }
            }
        }

        public VersionStamp OutputCSharpVersion
        {
            get
            {
                lock (_setOutputLock)
                {
                    return _outputCSharpVersion.Value;
                }
            }
        }

        public VersionStamp OutputHtmlVersion
        {
            get
            {
                lock (_setOutputLock)
                {
                    return _outputHtmlVersion.Value;
                }
            }
        }

        public RazorCSharpDocument OutputCSharp
        {
            get
            {
                lock (_setOutputLock)
                {
                    return _outputCSharp;
                }
            }
        }

        public RazorHtmlDocument OutputHtml
        {
            get
            {
                lock (_setOutputLock)
                {
                    return _outputHtml;
                }
            }
        }

        public DocumentSnapshot LatestDocument
        {
            get
            {
                lock (_setOutputLock)
                {
                    return _latestDocument;
                }
            }
        }

        public SourceTextContainer CSharpSourceTextContainer
        {
            get
            {
                lock (_setOutputLock)
                {
                    return _csharpTextContainer;
                }
            }
        }

        public SourceTextContainer HtmlSourceTextContainer
        {
            get
            {
                lock (_setOutputLock)
                {
                    return _htmlTextContainer;
                }
            }
        }

        public void SetOutput(
            DefaultDocumentSnapshot document, 
            RazorCSharpDocument outputCSharp,
            RazorHtmlDocument outputHtml,
            VersionStamp inputVersion,
            VersionStamp outputCSharpVersion,
            VersionStamp outputHtmlVersion)
        {
            lock (_setOutputLock)
            {
                if (_inputVersion.HasValue &&
                    _inputVersion != inputVersion &&
                    _inputVersion == _inputVersion.Value.GetNewerVersion(inputVersion))
                {
                    // Latest document is newer than the provided document.
                    return;
                }

                if (!document.TryGetText(out var source))
                {
                    Debug.Fail("The text should have already been evaluated.");
                    return;
                }

                _source = source;
                _inputVersion = inputVersion;
                _outputCSharpVersion = outputCSharpVersion;
                _outputHtmlVersion = outputHtmlVersion;
                _outputCSharp = outputCSharp;
                _outputHtml = outputHtml;
                _latestDocument = document;
                _csharpTextContainer.SetText(SourceText.From(_outputCSharp.GeneratedCode));
                _htmlTextContainer.SetText(SourceText.From(_outputHtml.GeneratedHtml));
            }
        }

        private void CSharpTextContainer_TextChanged(object sender, TextChangeEventArgs args)
        {
            GeneratedCSharpChanged?.Invoke(this, args);
        }

        private void HtmlTextContainer_TextChanged(object sender, TextChangeEventArgs args)
        {
            GeneratedHtmlChanged?.Invoke(this, args);
        }

        private class TextContainer : SourceTextContainer
        {
            public override event EventHandler<TextChangeEventArgs> TextChanged;

            private readonly object _outerLock;
            private SourceText _currentText;

            public TextContainer(object outerLock)
                : this(SourceText.From(string.Empty))
            {
                _outerLock = outerLock;
            }

            public TextContainer(SourceText sourceText)
            {
                if (sourceText == null)
                {
                    throw new ArgumentNullException(nameof(sourceText));
                }

                _currentText = sourceText;
            }

            public override SourceText CurrentText
            {
                get
                {
                    lock (_outerLock)
                    {
                        return _currentText;
                    }
                }
            }

            public void SetText(SourceText sourceText)
            {
                if (sourceText == null)
                {
                    throw new ArgumentNullException(nameof(sourceText));
                }

                lock (_outerLock)
                {

                    var e = new TextChangeEventArgs(_currentText, sourceText);
                    _currentText = sourceText;

                    TextChanged?.Invoke(this, e);
                }
            }
        }
    }
}
