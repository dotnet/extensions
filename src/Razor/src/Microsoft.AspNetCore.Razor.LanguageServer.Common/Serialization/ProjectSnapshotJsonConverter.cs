// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Common.Serialization
{
    internal class ProjectSnapshotJsonConverter : JsonConverter
    {
        public static readonly ProjectSnapshotJsonConverter Instance = new ProjectSnapshotJsonConverter();

        public override bool CanRead => false;

        public override bool CanWrite => true;

        public override bool CanConvert(Type objectType)
        {
            return typeof(ProjectSnapshot).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var project = (ProjectSnapshot)value;

            var documents = new List<DocumentSnapshotHandle>();
            foreach (var documentFilePath in project.DocumentFilePaths)
            {
                var document = project.GetDocument(documentFilePath);
                var documentHandle = new DocumentSnapshotHandle(document.FilePath, document.TargetPath, document.FileKind);
                documents.Add(documentHandle);
            }

            var handle = new FullProjectSnapshotHandle(project.FilePath, project.Configuration, project.RootNamespace, project.ProjectWorkspaceState, documents);

            FullProjectSnapshotHandleJsonConverter.Instance.WriteJson(writer, handle, serializer);
        }
    }
}