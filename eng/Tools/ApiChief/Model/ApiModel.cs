// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using ApiChief.Format;
using ApiChief.Processing;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;

namespace ApiChief.Model;

internal sealed class ApiModel
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        IncludeFields = true
    };

    static ApiModel()
    {
        _serializerOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public string Name { get; set; } = string.Empty;
    public ISet<ApiType> Types { get; set; } = new HashSet<ApiType>();
    public ISet<ApiType>? Additions { get; set; }
    public ISet<ApiType>? Removals { get; set; }

    public static ApiModel LoadFromAssembly(string path)
    {
        var decompiler = DecompilerFactory.Create(path);
        var finalApi = new ApiModel { Name = decompiler.TypeSystem.MainModule.FullAssemblyName };

        ApiProcessor.Process(finalApi, decompiler);

        return finalApi;
    }

    public static ApiModel LoadFromFile(string path)
    {
        return JsonSerializer.Deserialize<ApiModel>(File.ReadAllText(path))!;
    }

    public void EvaluateDelta(ApiModel current)
    {
        current.Additions = FindAdditions(this, current);
        current.Removals = FindAdditions(current, this);
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, _serializerOptions).ReplaceLineEndings("\n");
    }

    private static ISet<ApiType>? FindAdditions(ApiModel baseline, ApiModel delta)
    {
        ISet<ApiType>? result = null;

        foreach (var t in delta.Types)
        {
            if (!baseline.Types.Contains(t))
            {
                result ??= new HashSet<ApiType>();
                _ = result.Add(t);
            }
            else
            {
                var baseType = baseline.Types.First(bt => bt.Equals(t));

                if (t.Methods != null)
                {
                    foreach (var m in t.Methods)
                    {
                        if (baseType.Methods == null || !baseType.Methods.Contains(m))
                        {
                            result ??= new HashSet<ApiType>();

                            var type = result.First(dt => dt.Equals(t));
                            if (type == null)
                            {
                                type = new ApiType
                                {
                                    Type = t.Type,
                                    Stage = t.Stage,
                                };

                                result.Add(type);
                            }

                            type.Methods ??= new HashSet<ApiMember>();
                            _ = type.Methods!.Add(m);
                        }
                    }
                }

                if (t.Fields != null)
                {
                    foreach (var f in t.Fields)
                    {
                        if (baseType.Fields == null || !baseType.Fields.Contains(f))
                        {
                            result ??= new HashSet<ApiType>();

                            var type = result.First(dt => dt.Equals(t));
                            if (type == null)
                            {
                                type = new ApiType
                                {
                                    Type = t.Type,
                                    Stage = t.Stage,
                                };

                                result.Add(type);
                            }

                            type.Fields ??= new HashSet<ApiMember>();
                            _ = type.Fields.Add(f);
                        }
                    }
                }
            }
        }

        return result;
    }
}
