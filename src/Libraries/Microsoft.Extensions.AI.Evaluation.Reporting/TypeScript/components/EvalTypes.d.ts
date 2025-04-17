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
    creationTime: string;
    messages: ChatMessage[];
    modelResponse: ChatResponse;
    evaluationResult: EvaluationResult;
    chatDetails?: ChatDetails;
    tags?: string[];
    formatVersion: int;
};

type ChatResponse = {
    messages: ChatMessage[];
    modelId?: string;
    usage?: UsageDetails;
}

type ChatMessage = {
    authorName?: string;
    role: string;
    contents: AIContent[];
};

type ChatDetails = {
    turnDetails: ChatTurnDetails[];
}

type ChatTurnDetails = {
    latency: number;
    model?: string;
    usage?: UsageDetails;
    cacheKey?: string;
    cacheHit?: boolean;
}

type UsageDetails = {
    inputTokenCount?: number;
    outputTokenCount?: number;
    totalTokenCount?: number;
};

type AIContent = {
    $type: string;
};

// TODO: Model other types of AIContent such as function calls, function call results, audio etc.
type TextContent = AIContent & {
    $type: "text";
    text: string;
};

type UriContent = AIContent & {
    $type: "uri";
    uri: string;
    mediaType: string;
};

type DataContent = AIContent & {
    $type: "data";
    uri: string;
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
    context?: {
        [K: string]: AIContent[]
    };
    diagnostics?: EvaluationDiagnostic[];
    metadata: { 
        [K: string]: string 
    };
};

type MetricWithNoValue = BaseEvaluationMetric & {
    $type: "none";
    reason?: string;
    value: undefined;
};

type NumericMetric = BaseEvaluationMetric & {
    $type: "numeric";
    reason?: string;
    value?: number;
};

type BooleanMetric = BaseEvaluationMetric & {
    $type: "boolean";
    reason?: string;
    value?: boolean;
};

type StringMetric = BaseEvaluationMetric & {
    $type: "string";
    reason?: string;
    value?: string;
};
