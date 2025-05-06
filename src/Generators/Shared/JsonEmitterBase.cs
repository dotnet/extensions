// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

#pragma warning disable CA1716
namespace Microsoft.Gen.Shared;
#pragma warning restore CA1716

#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif

internal class JsonEmitterBase : EmitterBase
{
    public JsonEmitterBase(bool emitPreamble = true)
        : base(emitPreamble)
    {
    }

    protected void OutObject(Action action, bool isProprietyDependent = false)
    {
        NewItem(!isProprietyDependent);
        ItemCounts.Push(ItemCount);
        ItemCount = 0;

        OutIndent();
        Out("{");
        Indent();
        action();
        OutLn();
        Unindent();
        OutIndent();
        Out("}");

        ItemCount = ItemCounts.Pop();
    }

    protected void OutArray(string name, Action action, bool isProprietyDependent = false)
    {
        NewItem(!isProprietyDependent);
        ItemCounts.Push(ItemCount);
        ItemCount = 0;

        OutIndent();

        if (string.IsNullOrEmpty(name))
        {
            Out("[");
        }
        else
        {
            Out($"\"{name}\": [");
        }

        Indent();
        action();
        OutLn();
        Unindent();
        OutIndent();
        Out("]");

        ItemCount = ItemCounts.Pop();
    }

    protected void OutNameValue(string name, string value, bool isSingle = false)
    {
        value = value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");

        NewItem(preAppendComma: !isSingle);
        OutIndent();
        Out($"\"{name}\": \"{value}\"{(isSingle ? "," : string.Empty)}");

        if (isSingle)
        {
            OutLn();
        }
    }

    private void NewItem(bool preAppendComma = true)
    {
        if (preAppendComma && ItemCount > 0)
        {
            Out(",");
        }

        OutLn();
        ItemCount++;
    }

}
