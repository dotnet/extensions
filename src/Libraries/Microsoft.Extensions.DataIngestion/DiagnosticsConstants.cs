// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.DataIngestion;

internal static class DiagnosticsConstants
{
    internal const string ActivitySourceName = "Experimental.Microsoft.Extensions.DataIngestion";
    internal const string ErrorTypeTagName = "error.type";

    internal static class ProcessDocument
    {
        internal const string ActivityName = "ProcessDocument";
    }

    internal static class ProcessSource
    {
        internal const string DocumentIdTagName = "rag.document.id";
    }
}
