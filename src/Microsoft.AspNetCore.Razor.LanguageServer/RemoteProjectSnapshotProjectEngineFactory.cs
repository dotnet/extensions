// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RemoteProjectSnapshotProjectEngineFactory : DefaultProjectSnapshotProjectEngineFactory
    {
        private readonly FilePathNormalizer _filePathNormalizer;

        public RemoteProjectSnapshotProjectEngineFactory(
            IFallbackProjectEngineFactory fallback,
            Lazy<IProjectEngineFactory, ICustomProjectEngineFactoryMetadata>[] factories,
            FilePathNormalizer filePathNormalizer) : base(fallback, factories)
        {
            if (fallback == null)
            {
                throw new ArgumentNullException(nameof(fallback));
            }

            if (factories == null)
            {
                throw new ArgumentNullException(nameof(factories));
            }

            if (filePathNormalizer == null)
            {
                throw new ArgumentNullException(nameof(filePathNormalizer));
            }

            _filePathNormalizer = filePathNormalizer;
        }

        public override RazorProjectEngine Create(
            ProjectSnapshot project,
            RazorProjectFileSystem fileSystem,
            Action<RazorProjectEngineBuilder> configure)
        {
            var remoteFileSystem = new RemoteRazorProjectFileSystem(_filePathNormalizer);

            return base.Create(project, remoteFileSystem, configure);
        }
    }
}
