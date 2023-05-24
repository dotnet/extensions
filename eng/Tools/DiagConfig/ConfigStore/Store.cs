// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YamlDotNet.Serialization;

namespace DiagConfig.ConfigStore;

internal sealed class Store
{
    public IDictionary<string, Analyzer> Analyzers { get; set; } = new SortedDictionary<string, Analyzer>();

    public static Store Load(string directory)
    {
        var result = new Store();
        var deser = new Deserializer();

        foreach (var file in Directory.GetFiles(directory, "*.yml"))
        {
            var data = File.ReadAllText(file);

            Analyzer a;
            try
            {
                a = deser.Deserialize<Analyzer>(data);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Unable to parse diagnostic config file {file} due to {ex.Message}", ex);
            }

            result.Analyzers.Add(Path.GetFileNameWithoutExtension(file), a);
        }

        return result;
    }

    public void Save(string directory)
    {
        foreach (var a in Analyzers.Values)
        {
            foreach (var d in a.Diagnostics.Values)
            {
                foreach (var s in d.Attributes.Values)
                {
                    if (s == null)
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(s.Comment))
                    {
                        s.Comment = null;
                    }

                    if (s.Options != null && s.Options.Count == 0)
                    {
                        s.Options = null;
                    }
                }

                if (d.Metadata.CustomTags != null && d.Metadata.CustomTags.Count == 0)
                {
                    d.Metadata.CustomTags = null;
                }

                if (string.IsNullOrWhiteSpace(d.Metadata.HelpLinkUri))
                {
                    d.Metadata.HelpLinkUri = null;
                }
            }
        }

        var ser = new SerializerBuilder()
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
            .Build();

        foreach (var akvp in Analyzers)
        {
            var path = Path.Combine(directory, akvp.Key) + ".yml";
            var data = ser.Serialize(akvp.Value);
            File.WriteAllText(path, data.ReplaceLineEndings("\n"));
        }
    }

    public void Merge(Origin origin, string id, Diagnostic diag)
    {
        if (!Analyzers.TryGetValue(origin.AssemblyName!, out var a))
        {
            a = new Analyzer();
            Analyzers.Add(origin.AssemblyName!, a);
        }

        a.Origin = origin;

        if (a.Diagnostics.TryGetValue(id, out var d))
        {
            d.Metadata = diag.Metadata;
        }
        else
        {
            a.Diagnostics.Add(id, diag);
        }
    }

    public void SetSettingForAttribute(string id, string attributeName, DiagnosticSetting setting)
    {
        foreach (var akvp in Analyzers)
        {
            if (akvp.Value.Diagnostics.ContainsKey(id))
            {
                akvp.Value.Diagnostics[id].Attributes[attributeName] = setting;
            }
        }
    }

    public void ExportEditorConfig(IEnumerable<string> attributes, IEnumerable<string> includedAnalyzers, IEnumerable<string> excludedAnalyzers, string file, int maxTier, bool isGlobal)
    {
        var sb = new StringBuilder();

        var attrs = attributes.OrderBy(a => a).ToArray();

        var analyzers = (IEnumerable<KeyValuePair<string, Analyzer>>)Analyzers;
        if (includedAnalyzers.Any())
        {
            analyzers = analyzers.Where(a => includedAnalyzers.Contains(a.Key));
        }

        if (excludedAnalyzers.Any())
        {
            analyzers = analyzers.Where(a => !excludedAnalyzers.Contains(a.Key));
        }

        _ = sb.AppendLine($"# Created by DiagConfig, the diagnostic config generator");
        _ = sb.AppendLine($"# Generated : {DateTimeOffset.UtcNow:u}");
        _ = sb.AppendLine($"# Max Tier  : {maxTier}");
        _ = sb.AppendLine($"# Attributes: {string.Join(", ", attrs)}");
        _ = sb.AppendLine($"# Analyzers : {string.Join(", ", analyzers.Select(a => a.Key).OrderBy(a => a))}");
        _ = sb.AppendLine();

        if (isGlobal)
        {
            _ = sb.AppendLine("is_global = true");
            _ = sb.AppendLine("global_level = -1");
        }
        else
        {
            _ = sb.AppendLine("[*.cs]");
        }

        _ = sb.AppendLine();

        var diags = from x in analyzers
                    from y in x.Value.Diagnostics
                    orderby y.Key
                    select y;

        foreach (var d in diags)
        {
            var id = d.Key;
            var diag = d.Value;
            DiagnosticSetting? setting = null;

            if (diag.Tier <= maxTier)
            {
                // pick the attribute with the highest severity
                foreach (var a in attributes)
                {
                    if (diag.Attributes.ContainsKey(a))
                    {
                        var diagSettings = diag.Attributes[a];
                        if (setting == null)
                        {
                            setting = diagSettings;
                        }
                        else if (setting.Severity < diagSettings!.Severity)
                        {
                            setting = diagSettings;
                        }
                    }
                }
            }

            _ = sb.AppendLine($"# Title    : {diag.Metadata.Title}");
            _ = sb.AppendLine($"# Category : {diag.Metadata.Category}");

            if (!string.IsNullOrWhiteSpace(diag.Metadata.HelpLinkUri))
            {
                _ = sb.AppendLine($"# Help Link: {diag.Metadata.HelpLinkUri}");
            }

            if (setting != null)
            {
                if (!string.IsNullOrWhiteSpace(setting.Comment))
                {
                    _ = sb.AppendLine($"# Comment  : {setting.Comment}");
                }

                if (!string.IsNullOrWhiteSpace(setting.Redundant))
                {
                    _ = sb.AppendLine($"# Redundant: {setting.Redundant}");
                }

                var severity = setting.Severity;
                _ = sb.AppendLine($"dotnet_diagnostic.{id}.severity = {severity.ToString().ToLowerInvariant()}");

                if (setting.Options != null && severity != Severity.None)
                {
                    foreach (var o in setting.Options)
                    {
                        if (o.StartsWith("dotnet_") || o.StartsWith("csharp_"))
                        {
                            _ = sb.AppendLine($"{o}");
                        }
                        else
                        {
                            _ = sb.AppendLine($"dotnet_code_quality.{id}.{o}");
                        }
                    }
                }
            }
            else if (diag.Tier > maxTier)
            {
                _ = sb.AppendLine($"dotnet_diagnostic.{id}.severity = none");
            }

            _ = sb.AppendLine();
        }

        File.WriteAllText(file, sb.ToString().ReplaceLineEndings("\n"));
    }

    public IEnumerable<string> GetAttributes()
    {
        var attributes = new HashSet<string>();

        foreach (var a in Analyzers)
        {
            foreach (var d in a.Value.Diagnostics)
            {
                attributes.UnionWith(d.Value.Attributes.Keys);
            }
        }

        return attributes;
    }
}
