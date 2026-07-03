// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import React, { useEffect } from 'react';
import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ReportContextProvider, useReportContext, createScoreSummary, CasesView } from '../components';

// A case is "New" when its `${scenarioName}#${iterationName}` key is absent from the
// immediately-previous execution; the earliest execution has no previous run and flags nothing.

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

// Emitted newest-first (like the generator): the latest run carries one case (`kept-in-latest`)
// absent from the earliest, so exactly one case is "New" and none in the earliest run.
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

describe('CasesView — "New" badge derivation', () => {
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
