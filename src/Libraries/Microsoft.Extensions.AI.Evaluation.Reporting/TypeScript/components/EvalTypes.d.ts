// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

type Dataset = {
    scenarioRunResults: ScenarioRunResult[];
    generatorVersion?: string;
    createdAt?: string;
};

type ScenarioRunResult = {
    scenarioName: string;
    iterationName: string;
    executionName: string;
    creationTime?: string;
    messages: ChatMessage[];
    modelResponse: ChatMessage;
    evaluationResult: EvaluationResult;
};

type ChatMessage = {
    authorName?: string;
    role: string;
    contents: AIContent[]
};

type AIContent = {
    $type: string;
};

// TODO: Model other types of AIContent such as function calls, function call results, images, audio etc.
type TextContent = AIContent & {
    $type: "text";
    text: string;
};

type EvaluationResult = {
    metrics: {
        [K: string]: MetricWithNoValue | NumericMetric | BooleanMetric | StringMetric;
    };
};

type EvaluationDiagnostic = {
    severity: "informational" | "warning" | "error";
    message: string;
};

type EvaluationRating = "unknown" | "inconclusive" | "exceptional" | "good" | "average" | "poor" | "unacceptable";

type EvaluationMetricInterpretation = {
   rating: EvaluationRating;
   reason?: string;
   failed: boolean;
};

type BaseEvaluationMetric = {
    $type: string;
    name: string;
    interpretation?: EvaluationMetricInterpretation;
    diagnostics: EvaluationDiagnostic[];
};

type MetricWithNoValue = BaseEvaluationMetric & {
    $type: "none";
    value: undefined;
};

type NumericMetric = BaseEvaluationMetric & {
    $type: "numeric";
    value?: number;
};

type BooleanMetric = BaseEvaluationMetric & {
    $type: "boolean";
    value?: boolean;
};

type StringMetric = BaseEvaluationMetric & {
    $type: "string";
    value?: string;
};
