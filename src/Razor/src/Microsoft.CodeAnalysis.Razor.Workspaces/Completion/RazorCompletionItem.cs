// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Internal;

namespace Microsoft.CodeAnalysis.Razor.Completion
{
    internal sealed class RazorCompletionItem : IEquatable<RazorCompletionItem>
    {
        public RazorCompletionItem(
            string displayText, 
            string insertText, 
            string description, 
            RazorCompletionItemKind kind)
        {
            if (displayText == null)
            {
                throw new ArgumentNullException(nameof(displayText));
            }

            if (insertText == null)
            {
                throw new ArgumentNullException(nameof(insertText));
            }

            if (description == null)
            {
                throw new ArgumentNullException(nameof(description));
            }

            DisplayText = displayText;
            InsertText = insertText;
            Description = description;
            Kind = kind;
        }

        public string DisplayText { get; }

        public string InsertText { get; }

        public string Description { get; }

        public RazorCompletionItemKind Kind { get; }

        public override bool Equals(object obj)
        {
            return Equals(obj as RazorCompletionItem);
        }

        public bool Equals(RazorCompletionItem other)
        {
            if (other == null)
            {
                return false;
            }

            if (!string.Equals(DisplayText, other.DisplayText, StringComparison.Ordinal))
            {
                return false;
            }

            if (!string.Equals(InsertText, other.InsertText, StringComparison.Ordinal))
            {
                return false;
            }

            if (!string.Equals(Description, other.Description, StringComparison.Ordinal))
            {
                return false;
            }

            if (Kind != other.Kind)
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            var hashCodeCombiner = HashCodeCombiner.Start();
            hashCodeCombiner.Add(DisplayText);
            hashCodeCombiner.Add(InsertText);
            hashCodeCombiner.Add(Description);
            hashCodeCombiner.Add(Kind);

            return hashCodeCombiner.CombinedHash;
        }
    }
}
