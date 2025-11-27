// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.DataIngestion;

internal static class DiagnosticsConstants
{
    internal const string ActivitySourceName = "Experimental.Microsoft.Extensions.DataIngestion";
    internal const string ErrorTypeTagName = "error.type";

    internal static class ProcessDirectory
    {
        internal const string ActivityName = "ProcessDirectory";
        internal const string DirectoryPathTagName = "rag.directory.path";
        internal const string SearchPatternTagName = "rag.directory.search.pattern";
        internal const string SearchOptionTagName = "rag.directory.search.option";
    }

    internal static class ProcessSource
    {
        internal const string ActivityName = "ProcessSource";
        internal const string DocumentIdTagName = "rag.document.id";
    }
}
