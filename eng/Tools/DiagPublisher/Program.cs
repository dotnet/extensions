// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.Gen.AutoClient;
using Microsoft.Gen.ContextualOptions;
using Microsoft.Gen.EnumStrings;
using Microsoft.Gen.Logging;
using Microsoft.Gen.Metrics;
using Microsoft.Shared.DiagnosticIds;

namespace DiagPublisher;

public class Program
{
    private static void Main(string[] args)
    {
        // Pre-read this type name to avoid
        // error CS0433: The type 'DiagDescriptorsBase' exists in both 'Microsoft.Gen.AutoClient, ...' and 'Microsoft.Gen.ContextualOptions, ...'
        string diagDescriptorsBaseName = typeof(Microsoft.Gen.Shared.DiagDescriptorsBase).FullName!;

        string extraAnalyzersDiagDescriptorsName = typeof(Microsoft.Extensions.ExtraAnalyzers.DiagDescriptors).FullName!;

        _ = typeof(AutoClientGenerator).FullName;
        _ = typeof(ContextualOptionsGenerator).FullName;
        _ = typeof(EnumStringsGenerator).FullName;
        _ = typeof(LoggingGenerator).FullName;
        _ = typeof(MetricsGenerator).FullName;
        _ = typeof(Microsoft.Extensions.ExtraAnalyzers.DiagDescriptors).FullName;

        Dictionary<string, StringBuilder> diagnosticIds = [];

        List<Type> candidateTypes = [];
        foreach (Type type in GetCandidateTypes())
        {
            if (type.BaseType is null)
            {
                continue;
            }

            if (type.BaseType.FullName == diagDescriptorsBaseName)
            {
                // handle DiagDescriptorsBase implementations
                foreach (DiagnosticDescriptor diagnosticDescriptors in GetDiagnosticDescriptors(type))
                {
                    if (!diagnosticIds.ContainsKey(diagnosticDescriptors.Category))
                    {
                        StringBuilder sb = new();
                        sb.AppendLine($"# {diagnosticDescriptors.Category}");
                        sb.AppendLine();
                        sb.AppendLine("| Diagnostic ID     | Description |");
                        sb.AppendLine("| :---------------- | :---------- |");

                        diagnosticIds[diagnosticDescriptors.Category] = sb;
                    }

                    diagnosticIds[diagnosticDescriptors.Category].AppendLine($"| `{diagnosticDescriptors.Id}` | {diagnosticDescriptors.Title} |");
                }

                continue;
            }

            if (type.FullName == extraAnalyzersDiagDescriptorsName)
            {
                // handle ExtraAnalyzers.DiagDescriptors
                foreach (DiagnosticDescriptor diagnosticDescriptors in GetDiagnosticDescriptors(type))
                {
                    const string Category = "ExtraAnalyzers";

                    if (!diagnosticIds.ContainsKey(Category))
                    {
                        StringBuilder sb = new();
                        sb.AppendLine($"# {Category}");
                        sb.AppendLine();
                        sb.AppendLine("| Diagnostic ID     | Category | Description |");
                        sb.AppendLine("| :---------------- | :---------- | :---------- |");

                        diagnosticIds[Category] = sb;
                    }

                    diagnosticIds[Category].AppendLine($"| `{diagnosticDescriptors.Id}` | {diagnosticDescriptors.Category}  | {diagnosticDescriptors.Title} |");
                }

                continue;
            }
        }


        foreach (string category in diagnosticIds.Keys.OrderBy(k => k))
        {
            Console.WriteLine(diagnosticIds[category].ToString());
            Console.WriteLine();
            Console.WriteLine();
        }

        return;

        static IEnumerable<Type> GetCandidateTypes()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies().Where(p => !p.IsDynamic))
            {
                foreach (Type type in GetLoadableTypes(assembly))
                {
                    yield return type;
                }
            }
        }

        static IEnumerable<DiagnosticDescriptor> GetDiagnosticDescriptors(Type candidateType)
        {
            foreach (var member in candidateType.GetMembers())
            {
                if (member is PropertyInfo propertyInfo &&
                    propertyInfo.PropertyType.IsAssignableFrom(typeof(DiagnosticDescriptor)))
                {
                    if (propertyInfo.GetGetMethod() is MethodInfo getMethodInfo &&
                        getMethodInfo.IsStatic)
                    {
                        var descriptor = (DiagnosticDescriptor)propertyInfo.GetValue(null)!;

                        if (descriptor.HelpLinkUri != string.Format(DiagnosticIds.UrlFormat, descriptor.Id))
                        {
                            ReportError($"{candidateType.FullName}.{member.Name} {descriptor.Id}: {nameof(DiagnosticDescriptor.HelpLinkUri)} must start with {DiagnosticIds.UrlFormat}");
                        }

                        yield return descriptor;
                    }
                    else
                    {
                        ReportError($"{candidateType.FullName}.{member.Name} can't be queried");
                    }
                }
            }
        }

        static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t is not null)!;
            }
        }

        static void ReportError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("[ERROR]: ");
            Console.ResetColor();
            Console.WriteLine(message);
        }
    }
}
