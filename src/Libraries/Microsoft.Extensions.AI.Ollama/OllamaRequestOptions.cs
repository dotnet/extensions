// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

#pragma warning disable IDE1006 // Naming Styles

internal sealed class OllamaRequestOptions
{
    public bool? embedding_only { get; set; }
    public bool? f16_kv { get; set; }
    public float? frequency_penalty { get; set; }
    public bool? logits_all { get; set; }
    public bool? low_vram { get; set; }
    public int? main_gpu { get; set; }
    public float? min_p { get; set; }
    public int? mirostat { get; set; }
    public float? mirostat_eta { get; set; }
    public float? mirostat_tau { get; set; }
    public int? num_batch { get; set; }
    public int? num_ctx { get; set; }
    public int? num_gpu { get; set; }
    public int? num_keep { get; set; }
    public int? num_predict { get; set; }
    public int? num_thread { get; set; }
    public bool? numa { get; set; }
    public bool? penalize_newline { get; set; }
    public float? presence_penalty { get; set; }
    public int? repeat_last_n { get; set; }
    public float? repeat_penalty { get; set; }
    public long? seed { get; set; }
    public string[]? stop { get; set; }
    public float? temperature { get; set; }
    public float? tfs_z { get; set; }
    public int? top_k { get; set; }
    public float? top_p { get; set; }
    public float? typical_p { get; set; }
    public bool? use_mlock { get; set; }
    public bool? use_mmap { get; set; }
    public bool? vocab_only { get; set; }
}
