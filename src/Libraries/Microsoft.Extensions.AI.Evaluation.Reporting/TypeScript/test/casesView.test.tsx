// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import React from 'react';
import { describe, it, expect } from 'vitest';
import { render, screen, fireEvent, within } from '@testing-library/react';
import { ReportContextProvider, createScoreSummary, CasesView } from '../components';
import { toolCallDataset, richDataset } from './fixtures/richDataset';

const renderCases = (dataset: Dataset) => {
    const scoreSummary = createScoreSummary(dataset);
    return render(
        <ReportContextProvider dataset={dataset} scoreSummary={scoreSummary}>
            <CasesView />
        </ReportContextProvider>,
    );
};

// One scenario with two cases: a passing case first (lower input index) then a failing case.
// Default 'Scenario order' preserves input order (passing first); 'Failing first' must flip them.
const sortCase = (iterationName: string, value: number, rating: EvaluationRating, failed: boolean) => ({
    scenarioName: 'Group.Scenario',
    iterationName,
    executionName: 'Run1',
    creationTime: '2026-06-30T10:00:00.000Z',
    messages: [{ role: 'user', contents: [{ $type: 'text', text: 'q' }] }],
    modelResponse: {
        messages: [{ role: 'assistant', contents: [{ $type: 'text', text: 'a' }] }],
        modelId: 'gpt-4o',
        usage: { inputTokenCount: 1, outputTokenCount: 1, totalTokenCount: 2 },
    },
    evaluationResult: {
        metrics: {
            Quality: { $type: 'numeric', name: 'Quality', value, reason: 'r', interpretation: { rating, failed }, metadata: {} },
        },
    },
    formatVersion: 1,
    tags: [],
});

const sortDataset = {
    scenarioRunResults: [
        sortCase('pass-1', 5, 'good', false),
        sortCase('fail-2', 1, 'unacceptable', true),
    ],
} as unknown as Dataset;

const rowLabels = () =>
    screen
        .getAllByRole('button', { name: /passed|failed/i })
        .map((r) => (r.getAttribute('aria-label') ?? '').toLowerCase());

describe('CasesView — rows + expand + focus contract', () => {
    it('renders a case row for the single tool-call scenario', () => {
        renderCases(toolCallDataset);
        const rows = screen.getAllByRole('button', { name: /passed|failed/i });
        expect(rows.length).toBeGreaterThan(0);
    });

    it('expands the inline detail and focuses the detail heading on open', () => {
        renderCases(toolCallDataset);
        const row = screen.getAllByRole('button', { name: /passed|failed/i })[0];

        expect(screen.queryByRole('region', { name: /detail/i })).not.toBeInTheDocument();

        fireEvent.click(row);

        const detail = screen.getByRole('region', { name: /detail/i });
        expect(detail).toBeInTheDocument();
        expect(within(detail).getByText('Transcript')).toBeInTheDocument();
        expect(within(detail).getByText('Metrics')).toBeInTheDocument();
        expect(detail).toHaveFocus();
    });

    it('"Failing only" filters out passing cases', () => {
        renderCases(richDataset);
        const allRows = screen.getAllByRole('button', { name: /passed|failed/i });
        const failingBefore = allRows.filter((r) => /failed/i.test(r.getAttribute('aria-label') ?? ''));
        expect(failingBefore.length).toBeGreaterThan(0);

        fireEvent.click(screen.getByRole('switch', { name: /show failed/i }));

        const afterRows = screen.getAllByRole('button', { name: /passed|failed/i });
        expect(afterRows.length).toBe(failingBefore.length);
        for (const r of afterRows) {
            expect(r.getAttribute('aria-label')).toMatch(/failed/i);
        }
    });
});

describe('CasesView — scenario sort control', () => {
    it('defaults to "Scenario order" (input order) and keeps the passing case first', () => {
        renderCases(sortDataset);
        const sort = screen.getByRole('combobox', { name: /sort cases/i });
        expect(sort).toHaveValue('Scenario order');

        const labels = rowLabels();
        expect(labels).toHaveLength(2);
        expect(labels[0]).toMatch(/passed/);
        expect(labels[1]).toMatch(/failed/);
    });

    it('"Failing first" reorders failing cases ahead of passing ones within a scenario', () => {
        renderCases(sortDataset);

        fireEvent.click(screen.getByRole('combobox', { name: /sort cases/i }));
        fireEvent.click(screen.getByRole('option', { name: /failing first/i }));

        expect(screen.getByRole('combobox', { name: /sort cases/i })).toHaveValue('Failing first');

        const labels = rowLabels();
        expect(labels[0]).toMatch(/failed/);
        expect(labels[1]).toMatch(/passed/);
    });
});
