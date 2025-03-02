// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

#pragma warning disable CA1716
namespace Microsoft.Gen.Shared;
#pragma warning restore CA1716

#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
internal class JsonEmitterBase : EmitterBase
{
    private readonly Stack<int> _itemCounts = new();
    private int _itemCount;
    public JsonEmitterBase(bool emitPreamble = true)
        : base(emitPreamble)
    {
    }

    protected void OutObject(Action action)
    {
        NewItem();
        _itemCounts.Push(_itemCount);
        _itemCount = 0;

        OutIndent();
        Out("{");
        Indent();
        action();
        OutLn();
        Unindent();
        OutIndent();
        Out("}");

        _itemCount = _itemCounts.Pop();
    }

    protected void OutArray(string name, Action action)
    {
        NewItem();
        _itemCounts.Push(_itemCount);
        _itemCount = 0;

        OutIndent();
        Out($"\"{name}\": [");
        Indent();
        action();
        OutLn();
        Unindent();
        OutIndent();
        Out("]");

        _itemCount = _itemCounts.Pop();
    }

    protected void OutNameValue(string name, string value)
    {
        value = value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");

        NewItem();
        OutIndent();
        Out($"\"{name}\": \"{value}\"");
    }

    protected void OutEmptyObject()
    {
        Out("{}");
    }

    protected void OutEmptyArray()
    {
        Out("[]");
    }

    private void NewItem()
    {
        if (_itemCount > 0)
        {
            Out(",");
        }

        OutLn();
        _itemCount++;
    }

}
