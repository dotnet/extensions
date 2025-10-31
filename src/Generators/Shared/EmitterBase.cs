// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;

#pragma warning disable CA1716
namespace Microsoft.Gen.Shared;
#pragma warning restore CA1716

#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
internal class EmitterBase
{
    protected readonly Stack<int> ItemCounts = new();
    protected int ItemCount;

    private const int DefaultStringBuilderCapacity = 1024;
    private const int IndentChars = 4;

    private readonly StringBuilder _sb = new(DefaultStringBuilderCapacity);
    private readonly string[] _padding = new string[16];
    private int _indent;

    public EmitterBase(bool emitPreamble = true)
    {
        var padding = _padding;
        for (int i = 0; i < padding.Length; i++)
        {
            padding[i] = new string(' ', i * IndentChars);
        }

        if (emitPreamble)
        {
            Out(GeneratorUtilities.FilePreamble);
        }
    }

    protected void OutOpenBrace(bool isRoot = false) // isRoot is used to neglect any extra indentation before the brace, root has no indentation, defaulted to false for backward compatibility.
    {
        if (isRoot)
        {
            Out("{");
        }
        else
        {
            OutLn("{");
        }

        Indent();
    }

    protected void OutCloseBrace(bool isRoot = false)// isRoot is used to neglect any extra indentation before the brace, root has no indentation, defaulted to false for backward compatibility.
    {
        Unindent();

        if (isRoot)
        {
            Out("}");
        }
        else
        {
            OutLn("}");
        }

    }

    protected void OutCloseBraceWithExtra(string extra)
    {
        Unindent();
        OutIndent();
        Out("}");
        Out(extra);
        OutLn();
    }

    protected void OutIndent()
    {
        _ = _sb.Append(_padding[_indent]);
    }

    protected string GetPaddingString(byte indent)
    {
        return _padding[indent];
    }

    protected void OutLn()
    {
        _ = _sb.AppendLine();
    }

    protected void OutLn(string line)
    {
        OutIndent();
        _ = _sb.AppendLine(line);
    }

    protected void OutPP(string line)
    {
        _ = _sb.AppendLine(line);
    }

    protected void OutEnumeration(IEnumerable<string> e)
    {
        bool first = true;
        foreach (var item in e)
        {
            if (!first)
            {
                Out(", ");
            }

            Out(item);
            first = false;
        }
    }

    protected void Out(string text) => _ = _sb.Append(text);
    protected void Out(char ch) => _ = _sb.Append(ch);
    protected void Indent(int times = 1) => _indent += times;
    protected void Unindent(int times = 1) => _indent -= times;
    protected void OutGeneratedCodeAttribute() => OutLn($"[{GeneratorUtilities.GeneratedCodeAttribute}]");
    protected string Capture() => _sb.ToString();
}
