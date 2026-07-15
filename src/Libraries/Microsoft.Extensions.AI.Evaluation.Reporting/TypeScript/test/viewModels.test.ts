// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { describe, it, expect } from 'vitest';
import { createScoreSummary } from '../components/core/Summary';
import {
    isLeafFailed,
    passRateByScenarioGroup,
    kpiCountsFromNode,
    ratingBucket,
    bucketMetrics,
} from '../components/core/viewModels';
import {
    multiGroupDataset,
    diagnosticsErrorDataset,
    twoExecutionDataset,
} from './fixtures/richDataset';

describe('passRateByScenarioGroup — group rows reconcile to the whole (multiGroupDataset)', () => {
    it('sum of per-group passing/total equals the tree iteration totals', () => {
        const summary = createScoreSummary(multiGroupDataset);
        const root = summary.primaryResult;

        const rows = passRateByScenarioGroup(multiGroupDataset);
        const sumPassing = rows.reduce((acc, r) => acc + r.passing, 0);
        const sumTotal = rows.reduce((acc, r) => acc + r.total, 0);

        const treePassing = root.numPassingIterations;
        const treeFailing = root.numFailingIterations;
        const treeTotal = treePassing + treeFailing;

        expect(sumTotal).toBe(treeTotal);
        expect(sumPassing).toBe(treePassing);
        expect(sumTotal - sumPassing).toBe(treeFailing);
    });

    it('produces one row per distinct scenario group with literal pass rates', () => {
        const rows = passRateByScenarioGroup(multiGroupDataset);

        // Hardcoded expected groups (not re-derived from the source's split('.')[0]).
        expect(rows.map(r => r.group).sort()).toEqual(['GroupA', 'GroupB', 'GroupC']);

        // Literal pass rates read off the fixture (isLeafFailed per scenario):
        //   GroupA: 2/2 pass · GroupB: 2/2 pass · GroupC: 2/3 pass (safety=unacceptable fails).
        const byGroup = (g: string) => rows.find(r => r.group === g)!;
        expect(byGroup('GroupA')).toMatchObject({ passing: 2, total: 2 });
        expect(byGroup('GroupA').passRate).toBeCloseTo(1, 10);
        expect(byGroup('GroupB')).toMatchObject({ passing: 2, total: 2 });
        expect(byGroup('GroupB').passRate).toBeCloseTo(1, 10);
        expect(byGroup('GroupC')).toMatchObject({ passing: 2, total: 3 });
        expect(byGroup('GroupC').passRate).toBeCloseTo(2 / 3, 10);
    });
});

describe('Overview derivations == Cases-tree counts (diagnosticsErrorDataset)', () => {
    it('KPI counts and group-sum counts equal the Cases-tree numPassing/numFailing', () => {
        const summary = createScoreSummary(diagnosticsErrorDataset);
        const tree = summary.primaryResult;

        // Concrete anchors: diagnosticsErrorDataset = 2 passing + 1 diagnostic-error failure.
        expect(tree.numPassingIterations).toBe(2);
        expect(tree.numFailingIterations).toBe(1);

        // kpiCountsFromNode surfaces those counts plus a real pass-rate value (2/3),
        // asserted against literals rather than re-read from the same tree fields.
        const kpi = kpiCountsFromNode(tree);
        expect(kpi.passing).toBe(2);
        expect(kpi.failing).toBe(1);
        expect(kpi.total).toBe(3);
        expect(kpi.passRate).toBeCloseTo(2 / 3, 10);

        // Per-group sums reconcile to the same 2 passing / 1 failing via an
        // independent code path (groupTallies, not ScoreNode.aggregate).
        const rows = passRateByScenarioGroup(diagnosticsErrorDataset);
        const groupPassing = rows.reduce((acc, r) => acc + r.passing, 0);
        const groupTotal = rows.reduce((acc, r) => acc + r.total, 0);
        expect(groupPassing).toBe(2);
        expect(groupTotal).toBe(3);
    });

    it('isLeafFailed alone flags the diagnostics-only failure and clears the clean pass', () => {
        const failing = diagnosticsErrorDataset.scenarioRunResults.find(
            s => s.scenarioName === 'DiagTest.DiagnosticFailOnly',
        )!;
        const clean = diagnosticsErrorDataset.scenarioRunResults.find(
            s => s.scenarioName === 'DiagTest.CleanPass',
        )!;
        const info = diagnosticsErrorDataset.scenarioRunResults.find(
            s => s.scenarioName === 'DiagTest.InfoDiagnosticPass',
        )!;

        expect(failing.evaluationResult.metrics['mathematicalAccuracy'].interpretation?.failed).toBe(false);
        expect(isLeafFailed(failing)).toBe(true);
        expect(isLeafFailed(clean)).toBe(false);
        expect(isLeafFailed(info)).toBe(false);
    });
});

describe('isLeafFailed — empty-metrics semantics', () => {
    it('absent/empty metrics → false (uses .some, never .every)', () => {
        const noMetrics = {
            scenarioName: 'Empty.NoMetrics',
            iterationName: 'iteration1',
            executionName: 'exec-empty',
            creationTime: '2026-06-30T10:00:00.000Z',
            messages: [],
            modelResponse: { messages: [] },
            evaluationResult: { metrics: {} },
            formatVersion: 1 as unknown as int,
        } as ScenarioRunResult;
        expect(isLeafFailed(noMetrics)).toBe(false);
    });
});

describe('ratingBucket + bucketMetrics', () => {
    it('maps ratings into good/fair/weak/unknown buckets', () => {
        expect(ratingBucket('exceptional')).toBe('good');
        expect(ratingBucket('good')).toBe('good');
        expect(ratingBucket('average')).toBe('fair');
        expect(ratingBucket('poor')).toBe('weak');
        expect(ratingBucket('unacceptable')).toBe('weak');
        expect(ratingBucket('unknown')).toBe('unknown');
        expect(ratingBucket('inconclusive')).toBe('unknown');
        expect(ratingBucket(undefined)).toBe('unknown');
    });

    it('buckets every metric into the exact good/fair/weak/unknown distribution', () => {
        const primary = multiGroupDataset.scenarioRunResults;
        const counts = bucketMetrics(primary);

        // Exact distribution derived from every metric's interpretation.rating:
        //   good    = coherence, relevance, groundedness       (exceptional/good) → 3
        //   fair    = correctness, fidelity, fluency           (average)          → 3
        //   weak    = codeStyle(poor), safety(unacceptable)                       → 2
        //   unknown = creativity(inconclusive), knowledgeCheck(unknown)           → 2
        expect(counts).toEqual({ good: 3, fair: 3, weak: 2, unknown: 2 });

        // Conservation: all 10 metrics counted exactly once.
        const totalMetrics = primary.reduce(
            (acc, s) => acc + Object.values(s.evaluationResult.metrics).length,
            0,
        );
        expect(totalMetrics).toBe(10);
        expect(counts.good + counts.fair + counts.weak + counts.unknown).toBe(totalMetrics);
    });
});

describe('passRateByScenarioGroup — deltaRun sign across two executions', () => {
    // twoExecutionDataset has one group ('Comparison'):
    //   exec-v1 → 2/2 pass (passRate 1.0);  exec-v2 → 1/2 pass (safety fails → passRate 0.5).
    // executionOrder = [exec-v1, exec-v2]. For the primary (exec-v1, idx 0) the "previous"
    // is executions[1] (exec-v2); for exec-v2 the previous is the one before it (exec-v1).
    // deltaRun = activePassRate − previousPassRate.

    it('deltaRun is POSITIVE (+0.5) when the active exec outperforms its comparison', () => {
        const rows = passRateByScenarioGroup(twoExecutionDataset); // default active = primary exec-v1
        const comparison = rows.find(r => r.group === 'Comparison')!;
        expect(comparison).toMatchObject({ passing: 2, total: 2 });
        expect(comparison.passRate).toBeCloseTo(1, 10);
        // 1.0 (exec-v1) − 0.5 (exec-v2) = +0.5
        expect(comparison.deltaRun).toBeCloseTo(0.5, 10);
    });

    it('deltaRun is NEGATIVE (−0.5) when the active exec underperforms the previous', () => {
        const rows = passRateByScenarioGroup(twoExecutionDataset, 'exec-v2');
        const comparison = rows.find(r => r.group === 'Comparison')!;
        expect(comparison).toMatchObject({ passing: 1, total: 2 });
        expect(comparison.passRate).toBeCloseTo(0.5, 10);
        // 0.5 (exec-v2) − 1.0 (exec-v1) = −0.5  → proves the sign is active − previous
        expect(comparison.deltaRun).toBeCloseTo(-0.5, 10);
    });

    it('deltaRun is undefined for a single-execution dataset', () => {
        const single: Dataset = {
            ...twoExecutionDataset,
            scenarioRunResults: twoExecutionDataset.scenarioRunResults.filter(
                s => s.executionName === 'exec-v1',
            ),
        };
        for (const r of passRateByScenarioGroup(single)) {
            expect(r.deltaRun).toBeUndefined();
        }
    });
});
