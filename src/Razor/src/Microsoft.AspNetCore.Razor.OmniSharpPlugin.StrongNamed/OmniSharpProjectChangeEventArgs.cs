// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    public class OmniSharpProjectChangeEventArgs : EventArgs
    {
        internal OmniSharpProjectChangeEventArgs(ProjectChangeEventArgs args) : this(
            OmniSharpProjectSnapshot.Convert(args.Older),
            OmniSharpProjectSnapshot.Convert(args.Newer),
            (OmniSharpProjectChangeKind)args.Kind)
        {
            InternalProjectChangeEventArgs = args;
        }

        private OmniSharpProjectChangeEventArgs(OmniSharpProjectSnapshot older, OmniSharpProjectSnapshot newer, OmniSharpProjectChangeKind kind)
        {
            if (older == null && newer == null)
            {
                throw new ArgumentException("Both projects cannot be null.");
            }

            Older = older;
            Newer = newer;
            Kind = kind;

            ProjectFilePath = older?.FilePath ?? newer.FilePath;
        }

        internal ProjectChangeEventArgs InternalProjectChangeEventArgs { get; }

        public OmniSharpProjectSnapshot Older { get; }

        public OmniSharpProjectSnapshot Newer { get; }

        public string ProjectFilePath { get; }

        public string DocumentFilePath { get; }

        public OmniSharpProjectChangeKind Kind { get; }

        public static OmniSharpProjectChangeEventArgs CreateTestInstance(OmniSharpProjectSnapshot older, OmniSharpProjectSnapshot newer, OmniSharpProjectChangeKind kind) =>
            new OmniSharpProjectChangeEventArgs(older, newer, kind);
    }
}
