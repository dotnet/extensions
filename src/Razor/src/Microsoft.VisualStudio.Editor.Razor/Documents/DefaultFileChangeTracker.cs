// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Editor.Razor.Documents
{
    // A noop implementation for non-ide cases
    internal class DefaultFileChangeTracker : FileChangeTracker
    {
        public override event EventHandler<FileChangeEventArgs> Changed
        {
            // Do nothing (the handlers would never be used anyway)
            add { }
            remove { }
        }

        public DefaultFileChangeTracker(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            FilePath = filePath;
        }

        public override string FilePath { get; }

        public override void StartListening()
        {
            // Do nothing
        }

        public override void StopListening()
        {
            // Do nothing
        }
    }
}
