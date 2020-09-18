// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
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

        /// <summary>
        /// Attempts to retrieve the Razor addressable <see cref="Uri"/> for the provided <paramref name="textBuffer"/>.
        /// </summary>
        /// <param name="textBuffer">A text buffer</param>
        /// <param name="uri">A <see cref="Uri"/> that can be used to locate the provided <paramref name="textBuffer"/>.</param>
        /// <returns><c>true</c> if a Razor based <see cref="Uri"/> existed on the buffer, other wise <c>false</c>.</returns>
        public abstract bool TryGet(ITextBuffer textBuffer, out Uri uri);

        /// <summary>
        /// Adds or updates the provided <paramref name="uri"/> for the given <paramref name="textBuffer"/> under a Razor property name.
        /// </summary>
        /// <param name="textBuffer">A text buffer</param>
        /// <param name="uri">A <see cref="Uri"/> that can be used to locate the provided <paramref name="textBuffer"/>.</param>
        public abstract void AddOrUpdate(ITextBuffer textBuffer, Uri uri);

        /// <summary>
        /// Clears all <see cref="Uri"/> related state on the provided <paramref name="buffer"/>.
        /// </summary>
        /// <param name="buffer">A text buffer</param>
        public abstract void Remove(ITextBuffer buffer);
    }
}
