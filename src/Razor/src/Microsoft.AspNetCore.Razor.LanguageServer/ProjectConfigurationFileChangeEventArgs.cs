// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.Serialization;
using Microsoft.CodeAnalysis.Razor.Workspaces.Serialization;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class ProjectConfigurationFileChangeEventArgs
    {
        private readonly JsonFileDeserializer _jsonFileDeserializer;
        private FullProjectSnapshotHandle _projectSnapshotHandle;
        private readonly object _projectSnapshotHandleLock;
        private bool _deserialized;

        public ProjectConfigurationFileChangeEventArgs(
            string configurationFilePath,
            RazorFileChangeKind kind) : this(configurationFilePath, kind, JsonFileDeserializer.Instance)
        {
        }

        // Internal for testing
        internal ProjectConfigurationFileChangeEventArgs(
            string configurationFilePath,
            RazorFileChangeKind kind,
            JsonFileDeserializer jsonFileDeserializer)
        {
            if (configurationFilePath is null)
            {
                throw new ArgumentNullException(nameof(configurationFilePath));
            }

            if (jsonFileDeserializer is null)
            {
                throw new ArgumentNullException(nameof(jsonFileDeserializer));
            }

            ConfigurationFilePath = configurationFilePath;
            Kind = kind;
            _jsonFileDeserializer = jsonFileDeserializer;
            _projectSnapshotHandleLock = new object();
        }

        public string ConfigurationFilePath { get; }

        public RazorFileChangeKind Kind { get; }

        public bool TryDeserialize(out FullProjectSnapshotHandle handle)
        {
            if (Kind == RazorFileChangeKind.Removed)
            {
                // There's no file to represent the snapshot handle.
                handle = null;
                return false;
            }

            lock (_projectSnapshotHandleLock)
            {
                if (!_deserialized)
                {
                    // We use a deserialized flag instead of checking if _projectSnapshotHandle is null because if we're reading an old snapshot
                    // handle that doesn't deserialize properly it could be expected that it would be null.
                    _deserialized = true;
                    _projectSnapshotHandle = _jsonFileDeserializer.Deserialize<FullProjectSnapshotHandle>(ConfigurationFilePath);
                }
            }

            handle = _projectSnapshotHandle;
            if (handle == null)
            {
                // Deserialization failed
                return false;
            }

            return true;
        }
    }
}
