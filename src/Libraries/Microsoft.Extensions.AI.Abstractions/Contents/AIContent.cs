﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI;

/// <summary>Represents content used by AI services.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(DataContent), typeDiscriminator: "data")]
[JsonDerivedType(typeof(ErrorContent), typeDiscriminator: "error")]
[JsonDerivedType(typeof(FunctionCallContent), typeDiscriminator: "functionCall")]
[JsonDerivedType(typeof(FunctionResultContent), typeDiscriminator: "functionResult")]
[JsonDerivedType(typeof(HostedFileContent), typeDiscriminator: "hostedFile")]
[JsonDerivedType(typeof(HostedVectorStoreContent), typeDiscriminator: "hostedVectorStore")]
[JsonDerivedType(typeof(TextContent), typeDiscriminator: "text")]
[JsonDerivedType(typeof(TextReasoningContent), typeDiscriminator: "reasoning")]
[JsonDerivedType(typeof(UriContent), typeDiscriminator: "uri")]
[JsonDerivedType(typeof(UsageContent), typeDiscriminator: "usage")]

// These should be added in once they're no longer [Experimental]. If they're included while still
// experimental, any JsonSerializerContext that includes AIContent will incur errors about using
// experimental types in its source generated files. When [Experimental] is removed from these types,
// these lines should be uncommented and the corresponding lines in AIJsonUtilities.CreateDefaultOptions
// as well as the [JsonSerializable] attributes for them on the JsonContext should be removed.
// [JsonDerivedType(typeof(FunctionApprovalRequestContent), typeDiscriminator: "functionApprovalRequest")]
// [JsonDerivedType(typeof(FunctionApprovalResponseContent), typeDiscriminator: "functionApprovalResponse")]
// [JsonDerivedType(typeof(McpServerToolCallContent), typeDiscriminator: "mcpServerToolCall")]
// [JsonDerivedType(typeof(McpServerToolResultContent), typeDiscriminator: "mcpServerToolResult")]
// [JsonDerivedType(typeof(McpServerToolApprovalRequestContent), typeDiscriminator: "mcpServerToolApprovalRequest")]
// [JsonDerivedType(typeof(McpServerToolApprovalResponseContent), typeDiscriminator: "mcpServerToolApprovalResponse")]

public class AIContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIContent"/> class.
    /// </summary>
    public AIContent()
    {
    }

    /// <summary>
    /// Gets or sets a list of annotations on this content.
    /// </summary>
    public IList<AIAnnotation>? Annotations { get; set; }

    /// <summary>Gets or sets the raw representation of the content from an underlying implementation.</summary>
    /// <remarks>
    /// If an <see cref="AIContent"/> is created to represent some underlying object from another object
    /// model, this property can be used to store that original object. This can be useful for debugging or
    /// for enabling a consumer to access the underlying object model, if needed.
    /// </remarks>
    [JsonIgnore]
    public object? RawRepresentation { get; set; }

    /// <summary>Gets or sets additional properties for the content.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }
}
