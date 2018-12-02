// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    // Well-Known Schema and property names defined by the ManagedProjectSystem
    internal static class ManagedProjectSystemSchema
    {
        public static class ResolvedCompilationReference
        {
            public const string SchemaName = "ResolvedCompilationReference";

            public const string ItemName = "ResolvedCompilationReference";
        }

        public static class ContentItem
        {
            public const string SchemaName = "Content";

            public const string ItemName = "Content";
        }

        public static class NoneItem
        {
            public const string SchemaName = "None";

            public const string ItemName = "None";
        }

        public static class ItemReference
        {
            public const string FullPathPropertyName = "FullPath";

            public const string LinkPropertyName = "Link";
        }
    }
}
