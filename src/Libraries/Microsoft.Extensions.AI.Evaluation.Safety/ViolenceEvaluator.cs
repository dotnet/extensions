﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.AI.Evaluation.Safety;

/// <summary>
/// An <see cref="IEvaluator"/> that utilizes the Azure AI Content Safety service to evaluate responses produced by an
/// AI model for the presence of violent content.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ViolenceEvaluator"/> returns a <see cref="NumericMetric"/> with a value between 0 and 7, with 0
/// indicating an excellent score, and 7 indicating a poor score.
/// </para>
/// <para>
/// Note that <see cref="ViolenceEvaluator"/> can detect harmful content present within both image and text based
/// responses. Supported file formats include JPG/JPEG, PNG and GIF. Other modalities such as audio and video are
/// currently not supported.
/// </para>
/// </remarks>
public sealed class ViolenceEvaluator()
    : ContentHarmEvaluator(metricNames: new Dictionary<string, string> { ["violence"] = ViolenceMetricName })
{
    /// <summary>
    /// Gets the <see cref="EvaluationMetric.Name"/> of the <see cref="NumericMetric"/> returned by
    /// <see cref="ViolenceEvaluator"/>.
    /// </summary>
    public static string ViolenceMetricName => "Violence";
}
