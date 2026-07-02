// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import React from 'react';
import { describe, it, expect } from 'vitest';
import { render, screen, fireEvent, within } from '@testing-library/react';
import { ReportContextProvider } from '../components/ReportContext';
import { createScoreSummary } from '../components/Summary';
import { CasesView } from '../components/CasesView';
import { toolCallDataset, richDataset } from './fixtures/richDataset';

const renderCases = (dataset: Dataset) => {
    const scoreSummary = createScoreSummary(dataset);
    return render(
        <ReportContextProvider dataset={dataset} scoreSummary={scoreSummary}>
            <CasesView />
        </ReportContextProvider>,
    );
};

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
