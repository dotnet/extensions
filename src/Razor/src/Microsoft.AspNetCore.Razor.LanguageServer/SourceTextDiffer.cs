// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class SourceTextDiffer : TextDiffer
    {
        private readonly bool _lineDiffOnly;

        private SourceTextDiffer(SourceText oldText, SourceText newText, bool lineDiffOnly)
        {
            OldText = oldText;
            NewText = newText;
            _lineDiffOnly = lineDiffOnly;
        }

        private SourceText OldText { get; }

        private SourceText NewText { get; }

        protected override int OldTextLength => _lineDiffOnly ? OldText.Lines.Count : OldText.Length;

        protected override int NewTextLength => _lineDiffOnly ? NewText.Lines.Count : NewText.Length;

        public static IReadOnlyList<TextChange> GetMinimalTextChanges(SourceText oldText, SourceText newText, bool lineDiffOnly = true)
        {
            if (oldText is null)
            {
                throw new ArgumentNullException(nameof(oldText));
            }

            if (newText is null)
            {
                throw new ArgumentNullException(nameof(newText));
            }

            if (oldText.ContentEquals(newText))
            {
                return Array.Empty<TextChange>();
            }
            else if (oldText.Length == 0 || newText.Length == 0)
            {
                return newText.GetTextChanges(oldText);
            }

            var differ = new SourceTextDiffer(oldText, newText, lineDiffOnly);
            var edits = differ.ComputeDiff();

            var changes = differ.ProcessChanges(edits);

            Debug.Assert(oldText.WithChanges(changes).ContentEquals(newText), "Incorrect minimal changes");

            return changes;
        }

        protected override bool ContentEquals(int oldTextIndex, int newTextIndex)
        {
            if (_lineDiffOnly)
            {
                var oldLine = OldText.Lines[oldTextIndex];
                var oldLineText = oldLine.Text.GetSubText(oldLine.SpanIncludingLineBreak);

                var newLine = NewText.Lines[newTextIndex];
                var newLineText = newLine.Text.GetSubText(newLine.SpanIncludingLineBreak);

                return oldLineText.ContentEquals(newLineText);
            }

            return OldText[oldTextIndex] == NewText[newTextIndex];
        }

        private IReadOnlyList<TextChange> ProcessChanges(IReadOnlyList<DiffEdit> edits)
        {
            // Scan through the list of edits and collapse them into a minimal set of TextChanges.
            // This method assumes that there are no overlapping changes and the changes are sorted.

            var minimalChanges = new List<TextChange>();

            var start = 0;
            var end = 0;
            var builder = new StringBuilder();
            foreach (var edit in edits)
            {
                var startPosition = _lineDiffOnly ? OldText.Lines[edit.Position].Start : edit.Position;
                if (startPosition != end)
                {
                    // Previous edit's end doesn't match the new edit's start.
                    // Output the text change we were tracking.
                    if (start != end || builder.Length > 0)
                    {
                        minimalChanges.Add(new TextChange(TextSpan.FromBounds(start, end), builder.ToString()));
                        builder.Clear();
                    }

                    start = startPosition;
                }

                end = AppendEdit(edit);
            }

            if (start != end || builder.Length > 0)
            {
                minimalChanges.Add(new TextChange(TextSpan.FromBounds(start, end), builder.ToString()));
            }

            return minimalChanges;

            // Appends the new edit's content to the buffer and returns the end position.
            int AppendEdit(DiffEdit edit)
            {
                if (_lineDiffOnly)
                {
                    if (edit.Operation == DiffEdit.Type.Insert)
                    {
                        var newLine = NewText.Lines[edit.NewTextPosition.Value];
                        builder.Append(newLine.Text.ToString(newLine.SpanIncludingLineBreak));
                        return OldText.Lines[edit.Position].Start;
                    }
                    else
                    {
                        return OldText.Lines[edit.Position].EndIncludingLineBreak;
                    }
                }
                else
                {
                    if (edit.Operation == DiffEdit.Type.Insert)
                    {
                        builder.Append(NewText[edit.NewTextPosition.Value]);
                        return edit.Position;
                    }

                    return edit.Position + 1;
                }
            }
        }
    }
}
