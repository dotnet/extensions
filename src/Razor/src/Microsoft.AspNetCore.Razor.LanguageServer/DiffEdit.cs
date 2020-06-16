// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal readonly struct DiffEdit
    {
        public DiffEdit(Type operation, int pos, int? newTextPosition)
        {
            Operation = operation;
            Position = pos;
            NewTextPosition = newTextPosition;
        }

        public Type Operation { get; }

        public int Position { get; }

        public int? NewTextPosition { get; }

        public static DiffEdit Insert(int pos, int newTextPos)
        {
            return new DiffEdit(Type.Insert, pos, newTextPos);
        }

        public static DiffEdit Delete(int pos)
        {
            return new DiffEdit(Type.Delete, pos, newTextPosition: null);
        }

        internal enum Type
        {
            Insert,
            Delete,
        }
    }
}
