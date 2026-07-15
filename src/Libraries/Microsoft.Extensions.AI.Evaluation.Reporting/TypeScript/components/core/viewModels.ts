// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ScoreNode, getScoreHistory, type ScoreSummary } from './Summary';
import { isLeafFailed } from './scoring';

export { isLeafFailed };

export type RatingBucket = 'good' | 'fair' | 'weak' | 'unknown';

export type BucketCounts = {
    good: number;
    fair: number;
    weak: number;
    unknown: number;
};

export const ratingBucket = (rating: EvaluationRating | undefined): RatingBucket => {
    switch (rating) {
        case 'exceptional':
        case 'good':
            return 'good';
        case 'average':
            return 'fair';
        case 'poor':
        case 'unacceptable':
            return 'weak';
        case 'unknown':
        case 'inconclusive':
        default:
            return 'unknown';
    }
};

export const bucketMetrics = (scenarios: ScenarioRunResult[]): BucketCounts => {
    const counts: BucketCounts = { good: 0, fair: 0, weak: 0, unknown: 0 };
    for (const scenario of scenarios) {
        for (const metric of Object.values(scenario.evaluationResult?.metrics ?? {})) {
            counts[ratingBucket(metric?.interpretation?.rating)] += 1;
        }
    }
    return counts;
};

export type ScenarioGroupPassRate = {
    group: string;
    passing: number;
    total: number;
    passRate: number;
    deltaRun?: number;
};

const groupKey = (scenario: ScenarioRunResult): string => scenario.scenarioName.split('.')[0];

const executionOrder = (results: ScenarioRunResult[]): string[] => {
    const seen: string[] = [];
    const set = new Set<string>();
    for (const r of results) {
        if (!set.has(r.executionName)) {
            set.add(r.executionName);
            seen.push(r.executionName);
        }
    }
    return seen;
};

export const chronologicalExecutions = (dataset: Dataset): string[] => {
    const results = dataset.scenarioRunResults ?? [];
    const minTime = new Map<string, string>();
    const insertionIndex = new Map<string, number>();
    for (const r of results) {
        if (!insertionIndex.has(r.executionName)) {
            insertionIndex.set(r.executionName, insertionIndex.size);
        }
        const existing = minTime.get(r.executionName);
        if (existing === undefined || r.creationTime < existing) {
            minTime.set(r.executionName, r.creationTime);
        }
    }
    return [...insertionIndex.keys()].sort((a, b) => {
        const ta = minTime.get(a) ?? '';
        const tb = minTime.get(b) ?? '';
        if (ta < tb) return -1;
        if (ta > tb) return 1;
        return (insertionIndex.get(a) ?? 0) - (insertionIndex.get(b) ?? 0);
    });
};

const groupTallies = (results: ScenarioRunResult[]): Map<string, { passing: number; total: number }> => {
    const tallies = new Map<string, { passing: number; total: number }>();
    for (const scenario of results) {
        const key = groupKey(scenario);
        const entry = tallies.get(key) ?? { passing: 0, total: 0 };
        entry.total += 1;
        if (!isLeafFailed(scenario)) {
            entry.passing += 1;
        }
        tallies.set(key, entry);
    }
    return tallies;
};

export const passRateByScenarioGroup = (
    dataset: Dataset,
    execName?: string,
): ScenarioGroupPassRate[] => {
    const results = dataset.scenarioRunResults ?? [];
    const executions = executionOrder(results);
    const primaryExec = executions[0];

    const activeExec = execName ?? primaryExec;
    const activeIdx = executions.indexOf(activeExec);

    const previousExec = activeIdx <= 0 ? executions[1] : executions[activeIdx - 1];

    const activeResults = results.filter((r) => r.executionName === activeExec);
    const activeTallies = groupTallies(activeResults);

    const previousTallies = previousExec
        ? groupTallies(results.filter((r) => r.executionName === previousExec))
        : undefined;

    const rows: ScenarioGroupPassRate[] = [];
    for (const [group, { passing, total }] of activeTallies.entries()) {
        const passRate = total > 0 ? passing / total : 0;

        let deltaRun: number | undefined;
        if (previousTallies) {
            const prev = previousTallies.get(group);
            if (prev && prev.total > 0) {
                deltaRun = passRate - prev.passing / prev.total;
            }
        }

        rows.push({ group, passing, total, passRate, deltaRun });
    }
    return rows;
};

export const scenariosForExecution = (dataset: Dataset, execName?: string): ScenarioRunResult[] => {
    const results = dataset.scenarioRunResults ?? [];
    const activeExec = execName ?? executionOrder(results)[0];
    return results.filter((r) => r.executionName === activeExec);
};

export type KpiCounts = {
    passing: number;
    failing: number;
    total: number;
    passRate: number;
};

export const kpiCountsFromNode = (node: ScoreNode): KpiCounts => {
    const passing = node.numPassingIterations;
    const failing = node.numFailingIterations;
    const total = passing + failing;
    return { passing, failing, total, passRate: total > 0 ? passing / total : 0 };
};

export type MetricHistoryPoint = {
    executionName: string;
    value: number;
};

export type MetricHistorySeries = {
    metricName: string;
    points: MetricHistoryPoint[];
};

export type WeakMetric = {
    scenarioName: string;
    iterationName: string;
    metricName: string;
    rating: EvaluationRating;
    value?: number;
};

const isNumericMetric = (metric: BaseEvaluationMetric): metric is NumericMetric =>
    metric.$type === 'numeric' && typeof (metric as NumericMetric).value === 'number';

export const metricHistoryForScenario = (
    scoreSummary: ScoreSummary,
    scenario: ScenarioRunResult,
): MetricHistorySeries[] => {
    const history = getScoreHistory(scoreSummary, scenario);
    if (history.size < 2) {
        return [];
    }

    const series = new Map<string, MetricHistoryPoint[]>();
    for (const [executionName, run] of history.entries()) {
        for (const metric of Object.values(run.evaluationResult?.metrics ?? {})) {
            if (isNumericMetric(metric)) {
                const points = series.get(metric.name) ?? [];
                points.push({ executionName, value: metric.value! });
                series.set(metric.name, points);
            }
        }
    }

    return [...series.entries()].map(([metricName, points]) => ({ metricName, points }));
};

export type MoverMetricKind = 'fraction' | 'score' | 'severity' | 'count';

export type MoverRow = {
    scenarioName: string;
    metricName: string;
    kind: MoverMetricKind;
    value: number;
    delta: number;
};

const metricKind = (metric: NumericMetric): MoverMetricKind => {
    const declared = metric.metadata?.kind as MoverMetricKind | undefined;
    if (declared === 'fraction' || declared === 'score' || declared === 'severity' || declared === 'count') {
        return declared;
    }
    const v = metric.value ?? 0;
    if (v >= 0 && v <= 1) return 'fraction';
    return 'score';
};

const scaleMaxOf = (kind: MoverMetricKind): number =>
    kind === 'severity' ? 7 : 5;

export const formatScore = (value: number, kind: MoverMetricKind): string => {
    if (kind === 'fraction') return value.toFixed(3);
    const num = value % 1 === 0 ? '' + value : value.toFixed(1);
    if (kind === 'count') return num;
    return `${num}/${scaleMaxOf(kind)}`;
};

type MoverAgg = { scenarioName: string; metricName: string; kind: MoverMetricKind; sum: number; n: number };

const pairKey = (scenarioName: string, metricName: string): string =>
    JSON.stringify([scenarioName, metricName]);

const meanByScenarioMetric = (results: ScenarioRunResult[]): Map<string, MoverAgg> => {
    const agg = new Map<string, MoverAgg>();
    for (const r of results) {
        for (const metric of Object.values(r.evaluationResult?.metrics ?? {})) {
            if (!isNumericMetric(metric)) continue;
            const key = pairKey(r.scenarioName, metric.name);
            const entry =
                agg.get(key) ??
                { scenarioName: r.scenarioName, metricName: metric.name, kind: metricKind(metric), sum: 0, n: 0 };
            entry.sum += metric.value!;
            entry.n += 1;
            agg.set(key, entry);
        }
    }
    return agg;
};

export const moversBetween = (
    results: ScenarioRunResult[],
    selectedExec: string | undefined,
    prevExec: string | undefined,
    limit = 5,
): MoverRow[] => {
    if (!selectedExec || !prevExec) return [];

    const selectedAgg = meanByScenarioMetric(results.filter((r) => r.executionName === selectedExec));
    const prevAgg = meanByScenarioMetric(results.filter((r) => r.executionName === prevExec));

    const rows: MoverRow[] = [];
    for (const [key, sel] of selectedAgg.entries()) {
        const prev = prevAgg.get(key);
        if (!prev || prev.n === 0 || sel.n === 0) continue;
        const selMean = sel.sum / sel.n;
        const prevMean = prev.sum / prev.n;
        rows.push({
            scenarioName: sel.scenarioName,
            metricName: sel.metricName,
            kind: sel.kind,
            value: selMean,
            delta: selMean - prevMean,
        });
    }

    rows.sort((a, b) => Math.abs(b.delta) - Math.abs(a.delta));
    return Number.isFinite(limit) ? rows.slice(0, limit) : rows;
};

const RATING_SEVERITY: EvaluationRating[] = [
    'unacceptable',
    'poor',
    'average',
    'good',
    'exceptional',
    'inconclusive',
    'unknown',
];

const ratingRank = (rating: EvaluationRating): number => {
    const idx = RATING_SEVERITY.indexOf(rating);
    return idx === -1 ? RATING_SEVERITY.length : idx;
};

export const weakestMetrics = (scenarios: ScenarioRunResult[], limit = 5): WeakMetric[] => {
    const weak: WeakMetric[] = [];
    for (const scenario of scenarios) {
        for (const metric of Object.values(scenario.evaluationResult?.metrics ?? {})) {
            const rating = metric?.interpretation?.rating;
            if (rating === undefined) {
                continue;
            }
            weak.push({
                scenarioName: scenario.scenarioName,
                iterationName: scenario.iterationName,
                metricName: metric.name,
                rating,
                value: isNumericMetric(metric) ? metric.value : undefined,
            });
        }
    }

    weak.sort((a, b) => ratingRank(a.rating) - ratingRank(b.rating));
    return Number.isFinite(limit) ? weak.slice(0, limit) : weak;
};
