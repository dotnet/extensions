// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

// Case rows label themselves "<name> (passed|failed)". Expanded MetricRow buttons use
// ", failed" (no parens), so this parenthesized query never matches them and can't inflate.
const CASE_ROW = /\((?:passed|failed)\)/i;

describe('CasesView — rows + expand + focus contract', () => {
    it('renders the single tool-call scenario as a passed case row', () => {
        renderCases(toolCallDataset);
        const rows = screen.getAllByRole('button', { name: CASE_ROW });
        expect(rows).toHaveLength(1);
        // The tool-call scenario passes; a broken isLeafFailed would flip this to "(failed)".
        expect(rows[0].getAttribute('aria-label')).toMatch(/\(passed\)/i);
    });

    it('expands the inline detail without moving focus away from the disclosure button', () => {
        renderCases(toolCallDataset);
        const row = screen.getAllByRole('button', { name: CASE_ROW })[0];

        row.focus();

        expect(screen.queryByRole('region', { name: /detail/i })).not.toBeInTheDocument();

        fireEvent.click(row);

        const detail = screen.getByRole('region', { name: /detail/i });
        expect(detail).toBeInTheDocument();
        expect(within(detail).getByText('Transcript')).toBeInTheDocument();
        expect(within(detail).getByText('Metrics')).toBeInTheDocument();
        expect(row).toHaveFocus();
        expect(detail).not.toHaveAttribute('tabindex');
    });

    it('does not put a Tabster Mover on the expanded detail', () => {
        renderCases(toolCallDataset);
        fireEvent.click(screen.getAllByRole('button', { name: CASE_ROW })[0]);

        const detail = screen.getByRole('region', { name: /detail/i });
        expect(detail).not.toHaveAttribute('data-tabster');
    });

    it('"Failing only" filters out passing cases', () => {
        renderCases(richDataset);
        const allRows = screen.getAllByRole('button', { name: CASE_ROW });
        const failingBefore = allRows.filter((r) => /\(failed\)/i.test(r.getAttribute('aria-label') ?? ''));
        expect(failingBefore.length).toBeGreaterThan(0);

        fireEvent.click(screen.getByRole('switch', { name: /show failed/i }));

        const afterRows = screen.getAllByRole('button', { name: CASE_ROW });
        expect(afterRows.length).toBe(failingBefore.length);
        for (const r of afterRows) {
            expect(r.getAttribute('aria-label')).toMatch(/\(failed\)/i);
        }
    });
});
