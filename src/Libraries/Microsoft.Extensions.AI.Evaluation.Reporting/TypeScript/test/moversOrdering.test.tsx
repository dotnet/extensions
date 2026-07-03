// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { describe, it, expect } from 'vitest';
import {
    chronologicalExecutions,
    moversBetween,
    formatScore,
} from '../components/viewModels';

const E1 = 'run-alpha'; // chronologically earliest
const E2 = 'run-bravo'; // middle
const E3 = 'run-charlie'; // chronologically latest

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

// Scenario A: two accuracy cases per exec (E1:2,4→3; E2:3,5→4; E3:4,4→4) so mean-over-cases
// is non-trivial. Scenario B: a fraction metric to exercise fraction formatting.
const makeExec = (exec: string, t: string, accA: [number, number], fracB: number): ScenarioRunResult[] => [
    row('Group.ScenarioA', 'iteration1', exec, t, { accuracy: numeric('accuracy', accA[0]) }),
    row('Group.ScenarioA', 'iteration2', exec, t, { accuracy: numeric('accuracy', accA[1]) }),
    row('Group.ScenarioB', 'iteration1', exec, t, { coverage: numeric('coverage', fracB) }),
];

// INSERTION ORDER: newest-first (E3, then E2, then E1) — the reverse of chrono.
const dataset: Dataset = {
    generatorVersion: '0.0.1',
    createdAt: T3,
    scenarioRunResults: [
        ...makeExec(E3, T3, [4, 4], 0.9),
        ...makeExec(E2, T2, [3, 5], 0.5),
        // Scenario B of the earliest run is inserted before scenario A, so the first E1 row seen
        // isn't the "primary" one — proves min-over-rows (not first-row) picks the representative.
        row('Group.ScenarioB', 'iteration1', E1, T1, { coverage: numeric('coverage', 0.1) }),
        row('Group.ScenarioA', 'iteration1', E1, T1, { accuracy: numeric('accuracy', 2) }),
        row('Group.ScenarioA', 'iteration2', E1, T1, { accuracy: numeric('accuracy', 4) }),
    ],
};

// Inline mean-over-cases for one (scenario, metric, exec), computed independently of the view
// so the parity assertion pins moversBetween to the same numbers by construction.
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

    it('reduces each execution to its MIN row creationTime (row order within an exec is irrelevant)', () => {
        // E1 rows are inserted ScenarioB-first, yet E1 still sorts earliest.
        const chrono = chronologicalExecutions(dataset);
        expect(chrono[0]).toBe(E1);
    });
});

describe('moversBetween — baseline is the chronological predecessor', () => {
    it('resolves prev = chronologically-immediate predecessor (FAILS under an insertion-index impl)', () => {
        const chrono = chronologicalExecutions(dataset);
        const selected = E3;
        const prev = chrono[chrono.indexOf(selected) - 1];
        expect(prev).toBe(E2); // chronological predecessor of E3

        // Guard direction explicitly: selecting E2 must baseline against E1 (chronological),
        // never against E3, which is only insertion-newer.
        const moversE2 = moversBetween(dataset.scenarioRunResults, E2, chrono[chrono.indexOf(E2) - 1]);
        const accE2 = moversE2.find((m) => m.scenarioName === 'Group.ScenarioA' && m.metricName === 'accuracy')!;
        // mean(E2)=4, mean(E1)=3 → delta = +1 (vs E1). If it baselined on E3
        // (mean 4) the delta would be 0 — this pins the correct direction.
        expect(accE2.delta).toBeCloseTo(meanOf('Group.ScenarioA', 'accuracy', E2) - meanOf('Group.ScenarioA', 'accuracy', E1), 10);
        expect(accE2.delta).toBeCloseTo(1, 10);
    });

    it('emits exactly one row per (scenario, metric) — no per-case duplicates', () => {
        const chrono = chronologicalExecutions(dataset);
        const prev = chrono[chrono.indexOf(E3) - 1];
        const movers = moversBetween(dataset.scenarioRunResults, E3, prev, Infinity);
        const keys = movers.map((m) => `${m.scenarioName}::${m.metricName}`);
        expect(new Set(keys).size).toBe(keys.length);
        // Scenario A has 2 cases per exec but must collapse to ONE accuracy row.
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
        const prev = chrono[chrono.indexOf(selected) - 1]; // E2
        const movers = moversBetween(dataset.scenarioRunResults, selected, prev, Infinity);

        const acc = movers.find((m) => m.scenarioName === 'Group.ScenarioA' && m.metricName === 'accuracy')!;
        const expectedSel = meanOf('Group.ScenarioA', 'accuracy', selected); // (4+4)/2 = 4
        const expectedPrev = meanOf('Group.ScenarioA', 'accuracy', prev); // (3+5)/2 = 4
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

describe('formatScore — renders value on its natural scale', () => {
    it('score → N/5 (integer) and x.x/5 (fractional)', () => {
        expect(formatScore(4, 'score')).toBe('4/5');
        expect(formatScore(4.2, 'score')).toBe('4.2/5');
    });

    it('severity → /7 and fraction → toFixed(3)', () => {
        expect(formatScore(5, 'severity')).toBe('5/7');
        expect(formatScore(0.5, 'fraction')).toBe('0.500');
    });

    it("the accuracy mover renders as an N/5 score string", () => {
        const chrono = chronologicalExecutions(dataset);
        const movers = moversBetween(dataset.scenarioRunResults, E3, chrono[chrono.indexOf(E3) - 1], Infinity);
        const acc = movers.find((m) => m.metricName === 'accuracy')!;
        expect(acc.kind).toBe('score');
        expect(formatScore(acc.value, acc.kind)).toBe('4/5');
    });
});
