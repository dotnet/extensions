// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RemoteProjectSnapshotProjectEngineFactory : DefaultProjectSnapshotProjectEngineFactory
    {
        public static readonly IFallbackProjectEngineFactory FallbackProjectEngineFactory = new FallbackProjectEngineFactory();

        private readonly FilePathNormalizer _filePathNormalizer;

        public RemoteProjectSnapshotProjectEngineFactory(FilePathNormalizer filePathNormalizer) : 
            base(FallbackProjectEngineFactory, ProjectEngineFactories.Factories)
        {
            if (filePathNormalizer == null)
            {
                throw new ArgumentNullException(nameof(filePathNormalizer));
            }

            _filePathNormalizer = filePathNormalizer;
        }

        public override RazorProjectEngine Create(
            RazorConfiguration configuration,
            RazorProjectFileSystem fileSystem,
            Action<RazorProjectEngineBuilder> configure)
        {
            if (!(fileSystem is DefaultRazorProjectFileSystem defaultFileSystem))
            {
                Debug.Fail("Unexpected file system.");
                return null;
            }

            var remoteFileSystem = new RemoteRazorProjectFileSystem(defaultFileSystem.Root, _filePathNormalizer);
            return base.Create(configuration, remoteFileSystem, configure);
        }
    }
}
