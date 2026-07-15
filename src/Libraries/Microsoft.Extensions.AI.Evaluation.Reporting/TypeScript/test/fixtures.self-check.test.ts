// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { describe, it, expect } from 'vitest';
import { createScoreSummary } from '../components';
import {
    multiGroupDataset,
    diagnosticsErrorDataset,
    toolCallDataset,
    toolCallScenario,
    twoExecutionDataset,
    singleExecutionDataset,
    richDataset,
} from './fixtures/richDataset';

describe('multiGroupDataset', () => {
    it('aggregates without throwing', () => {
        expect(() => createScoreSummary(multiGroupDataset)).not.toThrow();
    });

    it('has ≥3 distinct groups via scenarioName.split(".")[0]', () => {
        const groups = new Set(
            multiGroupDataset.scenarioRunResults.map(s => s.scenarioName.split('.')[0]),
        );
        expect(groups.size).toBeGreaterThanOrEqual(3);
    });

    it('covers ALL ratings across its metrics', () => {
        const ratings = new Set<string>();
        for (const s of multiGroupDataset.scenarioRunResults) {
            for (const m of Object.values(s.evaluationResult.metrics)) {
                if (m.interpretation) {
                    ratings.add(m.interpretation.rating);
                }
            }
        }
        // NOTE: hand-maintained mirror of the `EvaluationRating` union. If that union in
        // components/EvalTypes.d.ts ever changes, this literal must be updated manually.
        const allRatings: EvaluationRating[] = [
            'unknown', 'inconclusive', 'exceptional', 'good', 'average', 'poor', 'unacceptable',
        ];
        for (const r of allRatings) {
            expect(ratings.has(r), `missing rating: ${r}`).toBe(true);
        }
    });

    it('has at least one failing scenario (unacceptable/failed)', () => {
        const summary = createScoreSummary(multiGroupDataset);
        expect(summary.primaryResult.numFailingIterations).toBeGreaterThan(0);
    });
});

describe('diagnosticsErrorDataset', () => {
    it('aggregates without throwing', () => {
        expect(() => createScoreSummary(diagnosticsErrorDataset)).not.toThrow();
    });

    it('has exactly 1 failing iteration (diagnostic error) and 2 passing', () => {
        const summary = createScoreSummary(diagnosticsErrorDataset);
        const root = summary.primaryResult;
        expect(root.numFailingIterations).toBe(1);
        expect(root.numPassingIterations).toBe(2);
    });

    it('the failing scenario has interpretation.failed===false but diagnostic severity==="error"', () => {
        const failing = diagnosticsErrorDataset.scenarioRunResults.find(s =>
            s.scenarioName === 'DiagTest.DiagnosticFailOnly',
        )!;
        const m = failing.evaluationResult.metrics['mathematicalAccuracy'];
        expect(m.interpretation?.failed).toBe(false);
        expect(m.diagnostics?.some(d => d.severity === 'error')).toBe(true);
    });
});

describe('toolCallDataset', () => {
    it('aggregates without throwing', () => {
        expect(() => createScoreSummary(toolCallDataset)).not.toThrow();
    });

    it('toolCallScenario messages contain functionCall and functionResult blocks', () => {
        const allContents = toolCallScenario.messages.flatMap(m => m.contents);
        expect(allContents.some(c => (c as AIContent & { $type: string }).$type === 'functionCall')).toBe(true);
        expect(allContents.some(c => (c as AIContent & { $type: string }).$type === 'functionResult')).toBe(true);
    });

    it('toolCallScenario functionCall has name and arguments', () => {
        const fc = toolCallScenario.messages
            .flatMap(m => m.contents)
            .find(c => (c as AIContent & { $type: string }).$type === 'functionCall') as
            AIContent & { name: string; callId: string; arguments: Record<string, unknown> };
        expect(fc.name).toBe('get_current_weather');
        expect(fc.callId).toBe('call-abc-001');
        expect(fc.arguments).toHaveProperty('location');
    });

    it('toolCallScenario functionResult has matching callId', () => {
        const fr = toolCallScenario.messages
            .flatMap(m => m.contents)
            .find(c => (c as AIContent & { $type: string }).$type === 'functionResult') as
            AIContent & { callId: string; result: Record<string, unknown> };
        expect(fr.callId).toBe('call-abc-001');
        expect(fr.result).toHaveProperty('temperature');
    });
});

describe('twoExecutionDataset', () => {
    it('aggregates without throwing', () => {
        expect(() => createScoreSummary(twoExecutionDataset)).not.toThrow();
    });

    it('has exactly 2 distinct executionNames', () => {
        const summary = createScoreSummary(twoExecutionDataset);
        expect(summary.executionHistory.size).toBe(2);
        expect(summary.includesReportHistory).toBe(true);
    });

    it('coherence improves exec-v1 → exec-v2', () => {
        const v1 = twoExecutionDataset.scenarioRunResults.find(
            s => s.scenarioName === 'Comparison.TextSummary' && s.executionName === 'exec-v1',
        )!;
        const v2 = twoExecutionDataset.scenarioRunResults.find(
            s => s.scenarioName === 'Comparison.TextSummary' && s.executionName === 'exec-v2',
        )!;
        const coherenceV1 = (v1.evaluationResult.metrics['coherence'] as NumericMetric).value!;
        const coherenceV2 = (v2.evaluationResult.metrics['coherence'] as NumericMetric).value!;
        expect(coherenceV2).toBeGreaterThan(coherenceV1);
    });

    it('safety regresses exec-v1 → exec-v2', () => {
        const v1 = twoExecutionDataset.scenarioRunResults.find(
            s => s.scenarioName === 'Comparison.TextSummary' && s.executionName === 'exec-v1',
        )!;
        const v2 = twoExecutionDataset.scenarioRunResults.find(
            s => s.scenarioName === 'Comparison.TextSummary' && s.executionName === 'exec-v2',
        )!;
        const safetyV1 = (v1.evaluationResult.metrics['safety'] as NumericMetric).value!;
        const safetyV2 = (v2.evaluationResult.metrics['safety'] as NumericMetric).value!;
        expect(safetyV2).toBeLessThan(safetyV1);
        expect(v2.evaluationResult.metrics['safety'].interpretation?.failed).toBe(true);
    });
});

describe('singleExecutionDataset', () => {
    it('aggregates without throwing', () => {
        expect(() => createScoreSummary(singleExecutionDataset)).not.toThrow();
    });

    it('has exactly 1 execution → includesReportHistory is false', () => {
        const summary = createScoreSummary(singleExecutionDataset);
        expect(summary.executionHistory.size).toBe(1);
        expect(summary.includesReportHistory).toBe(false);
    });
});

describe('richDataset', () => {
    it('aggregates without throwing', () => {
        expect(() => createScoreSummary(richDataset)).not.toThrow();
    });

    it('has ≥2 distinct executionNames (multi-execution history)', () => {
        const summary = createScoreSummary(richDataset);
        expect(summary.executionHistory.size).toBeGreaterThanOrEqual(2);
        expect(summary.includesReportHistory).toBe(true);
    });

    it('has both passing and failing iterations', () => {
        const summary = createScoreSummary(richDataset);
        const primaryExec = [...summary.executionHistory.values()][0];
        expect(primaryExec.numPassingIterations).toBeGreaterThan(0);
        expect(primaryExec.numFailingIterations).toBeGreaterThan(0);
    });

    it('primary execution (exec-2026-06-30) has exactly 10 scenarios', () => {
        const primaryScenarios = richDataset.scenarioRunResults.filter(
            s => s.executionName === 'exec-2026-06-30',
        );
        expect(primaryScenarios.length).toBe(10);
    });

    it('has at least one scenario with tags', () => {
        expect(richDataset.scenarioRunResults.some(s => s.tags && s.tags.length > 0)).toBe(true);
    });

    it('has at least one metric with interpretation.failed===true', () => {
        const hasFailedMetric = richDataset.scenarioRunResults.some(s =>
            Object.values(s.evaluationResult.metrics).some(m => m.interpretation?.failed === true),
        );
        expect(hasFailedMetric).toBe(true);
    });

    it('dotted scenarioNames produce intended groups', () => {
        const groups = new Set(
            richDataset.scenarioRunResults
                .filter(s => s.executionName === 'exec-2026-06-30')
                .map(s => s.scenarioName.split('.')[0]),
        );
        expect(groups.has('GroupA')).toBe(true);
        expect(groups.has('GroupB')).toBe(true);
        expect(groups.has('GroupC')).toBe(true);
        expect(groups.has('ToolCalls')).toBe(true);
    });
});
