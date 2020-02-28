// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    /// <summary>
    /// The purpose of the <see cref="FileUriProvider"/> is to take something that's not always addressable (an <see cref="ITextBuffer"/>) and make it addressable.
    /// This is required in LSP scenarios because everything operates off of the assumption that a document can be addressable via a <see cref="Uri"/>.
    /// </summary>
    internal abstract class FileUriProvider
    {
        /// <summary>
        /// Gets or creates an addressable <see cref="Uri"/> for the provided <paramref name="textBuffer"/>.
        ///
        /// In the case that the <paramref name="textBuffer"/> is not currently addressable via a <see cref="Uri"/>, we create one.
        /// </summary>
        /// <param name="textBuffer">A text buffer</param>
        /// <returns>A <see cref="Uri"/> that can be used to locate the provided <paramref name="textBuffer"/>.</returns>
        public abstract Uri GetOrCreate(ITextBuffer textBuffer);
    }
}
