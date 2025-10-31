﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.ComplianceReports;

internal sealed class ComplianceReportEmitter : JsonEmitterBase
{
    public ComplianceReportEmitter()
        : base(false)
    {
    }

    /// <summary>
    /// Generates JSON object containing the <see cref="ClassifiedTypes"/> for compliance report.
    /// </summary>
    /// <param name="classifiedTypes">The classified types.</param>
    /// <param name="assemblyName">The assembly name.</param>
    /// <param name="includeName">Whether to include the assembly name in the report. Defaulted to true.</param>
    /// <param name="indentationLevel">The number of indentations in case its nested in other reports like <see cref="MetadataReportsGenerator"/>.Defaulted to zero.</param>
    /// <returns>string report as json or String.Empty.</returns>
    [SuppressMessage("Performance", "LA0002:Use 'Microsoft.Extensions.Text.NumericExtensions.ToInvariantString' for improved performance", Justification = "Can't use that in a generator")]
    public string Emit(IReadOnlyCollection<ClassifiedType> classifiedTypes, string assemblyName,
        bool includeName = true, int indentationLevel = 0) // show or hide assemblyName in the report,defaulted to true.
    {
        Indent(indentationLevel);
        OutObject(() =>
        {
            // this is only for not displaying a name as part of ComplianceReport properties,it should be at the root of the report, defaulted to true for backward compatibility
            if (includeName)
            {
                OutNameValue("Name", assemblyName);
            }

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
        Unindent(indentationLevel);

        return Capture();
    }

}
