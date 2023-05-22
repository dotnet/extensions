// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.ComplianceReports;

internal sealed class Emitter : EmitterBase
{
    private readonly Stack<int> _itemCounts = new();
    private int _itemCount;

    public Emitter()
        : base(false)
    {
    }

    [SuppressMessage("Performance", "R9A036:Use 'Microsoft.Extensions.Text.NumericExtensions.ToInvariantString' for improved performance", Justification = "Can't use that in a generator")]
    public string Emit(IReadOnlyCollection<ClassifiedType> classifiedTypes, string assemblyName)
    {
        OutObject(() =>
        {
            OutNameValue("Name", assemblyName);

            OutArray("Types", () =>
            {
                foreach (var classifiedType in classifiedTypes.OrderBy(ct => ct.TypeName))
                {
                    OutObject(() =>
                    {
                        OutNameValue("Name", classifiedType.TypeName);

                        if (classifiedType.Members != null)
                        {
                            OutArray("Members", () =>
                            {
                                foreach (var member in classifiedType.Members.OrderBy(m => m.Name))
                                {
                                    OutObject(() =>
                                    {
                                        OutNameValue("Name", member.Name);
                                        OutNameValue("Type", member.TypeName);
                                        OutNameValue("File", member.SourceFilePath);
                                        OutNameValue("Line", member.SourceLine.ToString(CultureInfo.InvariantCulture));

                                        if (member.Classifications.Count > 0)
                                        {
                                            OutArray("Classifications", () =>
                                            {
                                                foreach (var c in member.Classifications.OrderBy(c => c.Name))
                                                {
                                                    OutObject(() =>
                                                    {
                                                        OutNameValue("Name", c.Name);

                                                        if (!string.IsNullOrEmpty(c.Notes))
                                                        {
                                                            OutNameValue("Notes", c.Notes!);
                                                        }
                                                    });
                                                }
                                            });
                                        }
                                    });
                                }
                            });
                        }

                        if (classifiedType.LogMethods != null)
                        {
                            OutArray("Logging Methods", () =>
                            {
                                foreach (var method in classifiedType.LogMethods.OrderBy(m => m.MethodName))
                                {
                                    OutObject(() =>
                                    {
                                        OutNameValue("Name", method.MethodName);

                                        OutArray("Parameters", () =>
                                        {
                                            foreach (var p in method.Parameters)
                                            {
                                                OutObject(() =>
                                                {
                                                    OutNameValue("Name", p.Name);
                                                    OutNameValue("Type", p.TypeName);
                                                    OutNameValue("File", p.SourceFilePath);
                                                    OutNameValue("Line", p.SourceLine.ToString(CultureInfo.InvariantCulture));

                                                    if (p.Classifications.Count > 0)
                                                    {
                                                        OutArray("Classifications", () =>
                                                        {
                                                            foreach (var c in p.Classifications.OrderBy(c => c.Name))
                                                            {
                                                                OutObject(() =>
                                                                {
                                                                    OutNameValue("Name", c.Name);

                                                                    if (!string.IsNullOrEmpty(c.Notes))
                                                                    {
                                                                        OutNameValue("Notes", c.Notes!);
                                                                    }
                                                                });
                                                            }
                                                        });
                                                    }
                                                });
                                            }
                                        });
                                    });
                                }
                            });
                        }
                    });
                }
            });
        });

        return Capture();
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

    private void OutObject(Action action)
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

    private void OutArray(string name, Action action)
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

    private void OutNameValue(string name, string value)
    {
        NewItem();
        OutIndent();
        Out($"\"{name}\": \"{value}\"");
    }
}
