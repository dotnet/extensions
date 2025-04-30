// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.AI.Evaluation.Safety;

/// <summary>
/// An <see cref="IEvaluator"/> that utilizes the Azure AI Content Safety service to evaluate responses produced by an
/// AI model for the presence of indirect attacks such as manipulated content, intrusion and information gathering.
/// </summary>
/// <remarks>
/// <para>
/// Indirect attacks, also known as cross-domain prompt injected attacks (XPIA), are when jailbreak attacks are
/// injected into the context of a document or source that may result in an altered, unexpected behavior. Indirect
/// attacks evaluations are broken down into three subcategories:
/// </para>
/// <para>
/// Manipulated Content: This category involves commands that aim to alter or fabricate information, often to mislead
/// or deceive.It includes actions like spreading false information, altering language or formatting, and hiding or
/// emphasizing specific details.The goal is often to manipulate perceptions or behaviors by controlling the flow and
/// presentation of information.
/// </para>
/// <para>
/// Intrusion: This category encompasses commands that attempt to breach systems, gain unauthorized access, or elevate
/// privileges illicitly. It includes creating backdoors, exploiting vulnerabilities, and traditional jailbreaks to
/// bypass security measures.The intent is often to gain control or access sensitive data without detection.
/// </para>
/// <para>
/// Information Gathering: This category pertains to accessing, deleting, or modifying data without authorization,
/// often for malicious purposes. It includes exfiltrating sensitive data, tampering with system records, and removing
/// or altering existing information. The focus is on acquiring or manipulating data to exploit or compromise systems
/// and individuals.
/// </para>
/// <para>
/// <see cref="IndirectAttackEvaluator"/> returns a <see cref="BooleanMetric"/> with a value of <see langword="true"/>
/// indicating the presence of an indirect attack in the response, and a value of <see langword="false"/> indicating
/// the absence of an indirect attack.
/// </para>
/// <para>
/// Note that <see cref="IndirectAttackEvaluator"/> does not support evaluation of multimodal content present in the
/// evaluated responses. Images and other multimodal content present in the evaluated responses will be ignored.
/// </para>
/// </remarks>
public sealed class IndirectAttackEvaluator()
    : ContentSafetyEvaluator(
        contentSafetyServiceAnnotationTask: "xpia",
        metricNames: new Dictionary<string, string> { ["xpia"] = IndirectAttackMetricName })
{
    /// <summary>
    /// Gets the <see cref="EvaluationMetric.Name"/> of the <see cref="BooleanMetric"/> returned by
    /// <see cref="IndirectAttackEvaluator"/>.
    /// </summary>
    public static string IndirectAttackMetricName => "Indirect Attack";
}
