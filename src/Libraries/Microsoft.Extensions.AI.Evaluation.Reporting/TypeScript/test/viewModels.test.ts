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

    it('produces one row per distinct scenario group', () => {
        const rows = passRateByScenarioGroup(multiGroupDataset);
        const distinctGroups = new Set(
            multiGroupDataset.scenarioRunResults.map(s => s.scenarioName.split('.')[0]),
        );
        expect(rows.length).toBe(distinctGroups.size);
        for (const r of rows) {
            expect(r.passRate).toBeCloseTo(r.total > 0 ? r.passing / r.total : 0, 10);
        }
    });
});

describe('Overview derivations == Cases-tree counts (diagnosticsErrorDataset)', () => {
    it('KPI counts and group-sum counts equal the Cases-tree numPassing/numFailing', () => {
        const summary = createScoreSummary(diagnosticsErrorDataset);
        const tree = summary.primaryResult;
        const treePassing = tree.numPassingIterations;
        const treeFailing = tree.numFailingIterations;

        const kpi = kpiCountsFromNode(tree);
        expect(kpi.passing).toBe(treePassing);
        expect(kpi.failing).toBe(treeFailing);
        expect(kpi.total).toBe(treePassing + treeFailing);

        const rows = passRateByScenarioGroup(diagnosticsErrorDataset);
        const groupPassing = rows.reduce((acc, r) => acc + r.passing, 0);
        const groupTotal = rows.reduce((acc, r) => acc + r.total, 0);
        expect(groupPassing).toBe(treePassing);
        expect(groupTotal - groupPassing).toBe(treeFailing);

        expect(treeFailing).toBe(1);
        expect(treePassing).toBe(2);
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

    it('buckets every metric exactly once (counts sum to metric count)', () => {
        const primary = multiGroupDataset.scenarioRunResults;
        const counts = bucketMetrics(primary);
        const totalMetrics = primary.reduce(
            (acc, s) => acc + Object.values(s.evaluationResult.metrics).length,
            0,
        );
        expect(counts.good + counts.fair + counts.weak + counts.unknown).toBe(totalMetrics);
    });
});

describe('passRateByScenarioGroup — deltaRun sign across two executions', () => {
    it('deltaRun is signed current(primary) − previous and undefined for single execution', () => {
        const rows = passRateByScenarioGroup(twoExecutionDataset);
        for (const r of rows) {
            expect(r.deltaRun).toBeDefined();
        }
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
