// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.Internal;

namespace Microsoft.CodeAnalysis.Razor.Completion
{
    internal sealed class RazorCompletionItem : IEquatable<RazorCompletionItem>
    {
        private ItemCollection _items;

        public RazorCompletionItem(
            string displayText,
            string insertText,
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

            DisplayText = displayText;
            InsertText = insertText;
            Kind = kind;
        }

        public string DisplayText { get; }

        public string InsertText { get; }

        public RazorCompletionItemKind Kind { get; }

        public ItemCollection Items
        {
            get
            {
                if (_items == null)
                {
                    lock (this)
                    {
                        if (_items == null)
                        {
                            _items = new ItemCollection();
                        }
                    }
                }

                return _items;
            }
        }

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

            if (Kind != other.Kind)
            {
                return false;
            }

            if (!Enumerable.SequenceEqual(Items, other.Items))
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
            hashCodeCombiner.Add(Kind);

            return hashCodeCombiner.CombinedHash;
        }
    }
}
