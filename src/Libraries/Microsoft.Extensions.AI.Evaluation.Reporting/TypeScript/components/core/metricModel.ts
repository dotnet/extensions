// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Canonical, behavior-preserving metric classification/formatting helpers.
export type MetricKind = 'score' | 'fraction' | 'severity' | 'count' | 'boolean' | 'none' | 'string';

type ScoredMetric = NumericMetric | BooleanMetric | StringMetric | MetricWithNoValue;

const NUMERIC_METRIC_KINDS: readonly MetricKind[] = ['score', 'fraction', 'severity', 'count'];

export type MetricKindOptions = {
    /**
     * Declared `metadata.kind` values accepted as a valid classification. Defaults to the four
     * numeric kinds (score/fraction/severity/count). Ignored when `trustDeclaredKind` is set.
     */
    allowedDeclaredKinds?: readonly MetricKind[];
    /**
     * When true, any truthy `metadata.kind` is trusted unvalidated .
     */
    trustDeclaredKind?: boolean;
};

/**
 * Classifies a metric's rendering kind. A faithful superset of the four call sites it replaces:
 * `$type` short-circuits to boolean/none/string (only MetricPanel's caller exercises this — the
 * others only ever pass numeric metrics, so this branch is simply unreached for them); otherwise a
 * declared `metadata.kind` is honored per `options`; otherwise a numeric fallback (value in [0,1] =>
 * 'fraction', else => 'score') mirrors every current call site's fallback exactly — including
 * viewModels'/ComparisonView's `value ?? 0` default for an absent value (=> 'fraction').
 */
export const metricKind = (metric: ScoredMetric, options: MetricKindOptions = {}): MetricKind => {
    if (metric.$type === 'boolean') return 'boolean';
    if (metric.$type === 'string') return 'string';
    if (metric.$type === 'none') return 'none';

    const declared = metric.metadata?.kind as MetricKind | undefined;
    if (declared) {
        if (options.trustDeclaredKind) return declared;
        if ((options.allowedDeclaredKinds ?? NUMERIC_METRIC_KINDS).includes(declared)) return declared;
    }

    const v = metric.value ?? 0;
    return v >= 0 && v <= 1 ? 'fraction' : 'score';
};

/**
 * Scale denominator for a kind, or null when the kind has no fixed scale (count/boolean/none/string).
 */
export const scaleMaxOf = (kind: MetricKind): number | null => {
    switch (kind) {
        case 'severity': return 7;
        case 'score': return 5;
        case 'fraction': return 1;
        default: return null;
    }
};

/**
 * Formats a numeric value on its scale.
 */
export const formatScore = (value: number, kind: MetricKind): string => {
    if (kind === 'fraction') return value.toFixed(3);
    const num = value % 1 === 0 ? String(value) : value.toFixed(1);
    if (kind === 'count') return num;
    const max = scaleMaxOf(kind);
    return max === null ? num : `${num}/${max}`;
};

export type BetterDirection = 'high' | 'low' | 'none';

/**
 * Reads `metadata.better`, defaulting to 'high'.
 */
export const betterDirectionOf = (metric: { metadata?: { better?: string } }): BetterDirection => {
    const b = metric.metadata?.better;
    return b === 'low' || b === 'none' ? b : 'high';
};
