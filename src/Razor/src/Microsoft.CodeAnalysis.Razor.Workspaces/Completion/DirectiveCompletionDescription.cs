// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Razor.Completion
{
    internal class DirectiveCompletionDescription
    {
        public DirectiveCompletionDescription(string description)
        {
            if (description == null)
            {
                throw new ArgumentNullException(nameof(description));
            }

            Description = description;
        }

        public string Description { get; }
    }
}
