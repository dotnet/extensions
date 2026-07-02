// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import React, { useEffect } from 'react';
import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ReportContextProvider, useReportContext } from '../components/ReportContext';
import { createScoreSummary } from '../components/Summary';
import { CasesView } from '../components/CasesView';

// Positive guard for the "New" case badge. It is derived client-side by CasesView from the data
// (a case whose `${scenarioName}#${iterationName}` key is absent from the immediately-previous
// execution is New; the earliest execution — having no previous run — flags nothing). This test
// guards that contract from silently vanishing, and replaces reliance on the dev-data generator's
// former hand-labeled "new-in-latest" rig: with the generator now growing case sets organically
// per run, the badge must still appear in the latest execution and be absent in the earliest.

type Metrics = ScenarioRunResult['evaluationResult']['metrics'];

const passMetric: Metrics = {
    quality: {
        $type: 'numeric',
        name: 'quality',
        value: 5,
        reason: 'ok',
        interpretation: { rating: 'exceptional', failed: false },
        metadata: {},
    } as NumericMetric,
};

const scenario = (
    scenarioName: string,
    iterationName: string,
    executionName: string,
    creationTime: string,
): ScenarioRunResult => ({
    scenarioName,
    iterationName,
    executionName,
    creationTime,
    messages: [{ role: 'user', contents: [{ $type: 'text', text: 'q' } as unknown as AIContent] }],
    modelResponse: {
        messages: [{ role: 'assistant', contents: [{ $type: 'text', text: 'a' } as unknown as AIContent] }],
        modelId: 'gpt-test',
    },
    evaluationResult: { metrics: passMetric },
    formatVersion: 1 as unknown as int,
    tags: ['difficulty:easy'],
});

const LATEST = 'exec-latest';
const EARLIEST = 'exec-earliest';

// Results are emitted newest-first (same as the generator), so the first-seen execution is the
// latest. The latest run carries one case (`kept-in-latest`) that never appeared in the earliest
// run, so exactly one case first-appears in the latest execution and none in the earliest.
const newCaseDataset: Dataset = {
    generatorVersion: '0.0.1-test',
    createdAt: '2026-06-22T12:00:00.000Z',
    scenarioRunResults: [
        // latest execution (first-seen -> primary)
        scenario('RAG.Answer', 'shared-case-001', LATEST, '2026-06-22T12:00:00Z'),
        scenario('RAG.Answer', 'shared-case-002', LATEST, '2026-06-22T12:00:00Z'),
        scenario('RAG.Answer', 'new-in-latest-003', LATEST, '2026-06-22T12:00:00Z'),
        // earliest execution — a strict subset of the latest run's case set
        scenario('RAG.Answer', 'shared-case-001', EARLIEST, '2026-06-15T09:00:00Z'),
        scenario('RAG.Answer', 'shared-case-002', EARLIEST, '2026-06-15T09:00:00Z'),
    ],
};

const ExecSetter = ({ exec, children }: { exec?: string; children: React.ReactNode }) => {
    const { setExec } = useReportContext();
    useEffect(() => {
        if (exec !== undefined) setExec(exec);
    }, [exec, setExec]);
    return <>{children}</>;
};

const renderCasesWith = (exec?: string) => {
    const summary = createScoreSummary(newCaseDataset);
    return render(
        <ReportContextProvider dataset={newCaseDataset} scoreSummary={summary}>
            <ExecSetter exec={exec}>
                <CasesView />
            </ExecSetter>
        </ReportContextProvider>,
    );
};

describe('CasesView — "New" badge derivation (guards the organic new-case data)', () => {
    it('shows >=1 New badge on the latest execution', () => {
        renderCasesWith(undefined); // default active execution == primary == latest
        const badges = screen.getAllByText('New', { selector: '.eval-new-badge' });
        expect(badges.length).toBeGreaterThanOrEqual(1);
    });

    it('shows no New badge on the earliest execution', async () => {
        renderCasesWith(EARLIEST);
        // Let the ExecSetter effect re-scope to the earliest run before asserting absence.
        await screen.findAllByRole('button', { name: /passed|failed/i });
        expect(screen.queryByText('New', { selector: '.eval-new-badge' })).not.toBeInTheDocument();
    });
});
