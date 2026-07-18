// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { describe, it, expect } from 'vitest';
import {
    chronologicalExecutions,
    moversBetween,
} from '../components/core/viewModels';
import { formatNumber } from '../components/core/metricModel';

const E1 = 'run-alpha';
const E2 = 'run-bravo';
const E3 = 'run-charlie';

const T1 = '2026-01-01T00:00:00.000Z';
const T2 = '2026-02-01T00:00:00.000Z';
const T3 = '2026-03-01T00:00:00.000Z';

const numeric = (name: string, value: number): NumericMetric =>
    ({
        $type: 'numeric',
        name,
        value,
        reason: 'test',
        interpretation: { rating: 'good', failed: false },
        metadata: {},
    }) as NumericMetric;

const row = (
    scenarioName: string,
    iterationName: string,
    executionName: string,
    creationTime: string,
    metrics: Record<string, NumericMetric>,
): ScenarioRunResult =>
    ({
        scenarioName,
        iterationName,
        executionName,
        creationTime,
        messages: [],
        modelResponse: { messages: [] },
        evaluationResult: { metrics },
        formatVersion: 1 as unknown as int,
    }) as ScenarioRunResult;

const makeExec = (exec: string, t: string, accA: [number, number], fracB: number): ScenarioRunResult[] => [
    row('Group.ScenarioA', 'iteration1', exec, t, { accuracy: numeric('accuracy', accA[0]) }),
    row('Group.ScenarioA', 'iteration2', exec, t, { accuracy: numeric('accuracy', accA[1]) }),
    row('Group.ScenarioB', 'iteration1', exec, t, { coverage: numeric('coverage', fracB) }),
];

const dataset: Dataset = {
    generatorVersion: '0.0.1',
    createdAt: T3,
    scenarioRunResults: [
        ...makeExec(E3, T3, [4, 4], 0.9),
        ...makeExec(E2, T2, [3, 5], 0.5),
        row('Group.ScenarioB', 'iteration1', E1, T1, { coverage: numeric('coverage', 0.1) }),
        row('Group.ScenarioA', 'iteration1', E1, T1, { accuracy: numeric('accuracy', 2) }),
        row('Group.ScenarioA', 'iteration2', E1, T1, { accuracy: numeric('accuracy', 4) }),
    ],
};

const meanOf = (scenario: string, metricName: string, exec: string): number => {
    const vals = dataset.scenarioRunResults
        .filter((r) => r.executionName === exec && r.scenarioName === scenario)
        .map((r) => (r.evaluationResult.metrics[metricName] as NumericMetric | undefined)?.value)
        .filter((v): v is number => typeof v === 'number');
    return vals.reduce((a, b) => a + b, 0) / vals.length;
};

describe('chronologicalExecutions — orders by min creationTime, not insertion order', () => {
    it('sorts ascending by creationTime even when inserted newest-first', () => {
        expect(chronologicalExecutions(dataset)).toEqual([E1, E2, E3]);
    });

    it("orders by each execution's MIN row creationTime, not its first/last row or insertion order", () => {
        const A = 'exec-A';
        const B = 'exec-B';
        const t10 = '2026-01-10T00:00:00.000Z';
        const t20 = '2026-01-20T00:00:00.000Z';
        const t30 = '2026-01-30T00:00:00.000Z';
        // B is inserted first; A's rows are interleaved in time — a late t30 row appears
        // before an early t10 row. Only a per-exec MIN reduction (A.min=t10 < B.min=t20)
        // yields [A, B]. Insertion order, first-row time, and max-row time all give [B, A],
        // so this fixture genuinely exercises the MIN logic (the old test could not).
        const interleaved: Dataset = {
            generatorVersion: '0.0.1',
            createdAt: t30,
            scenarioRunResults: [
                row('S.One', 'it1', B, t20, { m: numeric('m', 1) }),
                row('S.One', 'it1', A, t30, { m: numeric('m', 2) }),
                row('S.One', 'it2', A, t10, { m: numeric('m', 3) }),
            ],
        };
        expect(chronologicalExecutions(interleaved)).toEqual([A, B]);
    });
});

describe('moversBetween — baseline is the chronological predecessor', () => {
    it('resolves prev = chronologically-immediate predecessor (FAILS under an insertion-index impl)', () => {
        const chrono = chronologicalExecutions(dataset);
        const selected = E3;
        const prev = chrono[chrono.indexOf(selected) - 1];
        expect(prev).toBe(E2);

        const moversE2 = moversBetween(dataset.scenarioRunResults, E2, chrono[chrono.indexOf(E2) - 1]);
        const accE2 = moversE2.find((m) => m.scenarioName === 'Group.ScenarioA' && m.metricName === 'accuracy')!;
        expect(accE2.delta).toBeCloseTo(meanOf('Group.ScenarioA', 'accuracy', E2) - meanOf('Group.ScenarioA', 'accuracy', E1), 10);
        expect(accE2.delta).toBeCloseTo(1, 10);
    });

    it('emits exactly one row per (scenario, metric) — no per-case duplicates', () => {
        const chrono = chronologicalExecutions(dataset);
        const prev = chrono[chrono.indexOf(E3) - 1];
        const movers = moversBetween(dataset.scenarioRunResults, E3, prev, Infinity);
        const keys = movers.map((m) => `${m.scenarioName}::${m.metricName}`);
        expect(new Set(keys).size).toBe(keys.length);
        expect(keys.filter((k) => k === 'Group.ScenarioA::accuracy').length).toBe(1);
    });

    it('is empty when the selected run is the chronologically-earliest (no predecessor)', () => {
        const chrono = chronologicalExecutions(dataset);
        const earliest = chrono[0];
        expect(earliest).toBe(E1);
        const prev = chrono.indexOf(earliest) > 0 ? chrono[chrono.indexOf(earliest) - 1] : undefined;
        expect(moversBetween(dataset.scenarioRunResults, earliest, prev)).toEqual([]);
    });

    it('per-pair value/delta equal an inline mean-over-cases recompute (parity, no view import)', () => {
        const chrono = chronologicalExecutions(dataset);
        const selected = E3;
        const prev = chrono[chrono.indexOf(selected) - 1];
        const movers = moversBetween(dataset.scenarioRunResults, selected, prev, Infinity);

        const acc = movers.find((m) => m.scenarioName === 'Group.ScenarioA' && m.metricName === 'accuracy')!;
        const expectedSel = meanOf('Group.ScenarioA', 'accuracy', selected);
        const expectedPrev = meanOf('Group.ScenarioA', 'accuracy', prev);
        expect(expectedSel).toBe(4);
        expect(expectedPrev).toBe(4);
        expect(acc.value).toBeCloseTo(expectedSel, 10);
        expect(acc.delta).toBeCloseTo(expectedSel - expectedPrev, 10);

        const cov = movers.find((m) => m.metricName === 'coverage')!;
        expect(cov.value).toBeCloseTo(meanOf('Group.ScenarioB', 'coverage', selected), 10);
        expect(cov.delta).toBeCloseTo(
            meanOf('Group.ScenarioB', 'coverage', selected) - meanOf('Group.ScenarioB', 'coverage', prev),
            10,
        );
    });
});

