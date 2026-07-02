// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import React, { useEffect } from 'react';
import { describe, it, expect } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { createScoreSummary } from '../components/Summary';
import { ReportContextProvider, useReportContext } from '../components/ReportContext';
import { HistoryView } from '../components/HistoryView';
import { ComparisonView } from '../components/ComparisonView';
import { twoExecutionDataset, singleExecutionDataset } from './fixtures/richDataset';

const renderWith = (dataset: Dataset, ui: React.ReactElement) => {
    const scoreSummary = createScoreSummary(dataset);
    return render(
        <ReportContextProvider dataset={dataset} scoreSummary={scoreSummary}>
            {ui}
        </ReportContextProvider>,
    );
};

// Selects the sidebar node whose leaf scenario matches `scenarioName` (item 10:
// Comparison must follow the sidebar selection). selectedScenarioLevel is a
// nodeKey, not a scenarioName, so we resolve the leaf's nodeKey at runtime.
// selectScenarioLevel TOGGLES, so we only call it while the target key is not
// already the active selection — otherwise the effect would flip it back off and
// re-fire in a render loop.
const ScenarioSelector = ({ scenarioName, children }: { scenarioName: string; children: React.ReactNode }) => {
    const { activeNode, selectedScenarioLevel, selectScenarioLevel } = useReportContext();
    const targetKey = activeNode.flattenedNodes.find(
        (n) => n.isLeafNode && n.scenario?.scenarioName === scenarioName,
    )?.nodeKey;
    useEffect(() => {
        if (targetKey && selectedScenarioLevel !== targetKey) {
            selectScenarioLevel(targetKey);
        }
        // selectScenarioLevel is intentionally excluded: it is a fresh reference
        // each render and toggles, so depending on it would loop.
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [targetKey, selectedScenarioLevel]);
    return <>{children}</>;
};

describe('HistoryView — twoExecutionDataset', () => {
    it('renders metric tabs for numeric metrics', () => {
        renderWith(twoExecutionDataset, <HistoryView />);
        const tabs = screen.getAllByRole('tab');
        expect(tabs.length).toBeGreaterThan(0);
    });

    it('renders the trend chart SVG with role=img', () => {
        renderWith(twoExecutionDataset, <HistoryView />);
        const charts = screen.getAllByRole('img');
        expect(charts.length).toBeGreaterThan(0);
    });

    it('renders the run history section', () => {
        renderWith(twoExecutionDataset, <HistoryView />);
        // Run history is now a CSS grid (matches the v3.1 mockup), not a <table>.
        expect(screen.getByText(/run history/i)).toBeInTheDocument();
    });
});

describe('HistoryView — singleExecutionDataset (empty state)', () => {
    it('renders the "Needs at least 2 executions" message', () => {
        renderWith(singleExecutionDataset, <HistoryView />);
        expect(screen.getByText(/needs at least 2 executions/i)).toBeInTheDocument();
    });

    it('does NOT render any metric tabs', () => {
        renderWith(singleExecutionDataset, <HistoryView />);
        const tabs = screen.queryAllByRole('tab');
        expect(tabs.length).toBe(0);
    });

    it('does NOT render any SVG chart', () => {
        renderWith(singleExecutionDataset, <HistoryView />);
        const charts = screen.queryAllByRole('img');
        expect(charts.length).toBe(0);
    });
});

describe('ComparisonView — twoExecutionDataset', () => {
    it('renders execution dropdowns for A and B', () => {
        renderWith(twoExecutionDataset, <ComparisonView />);
        expect(screen.getByLabelText(/baseline execution/i)).toBeInTheDocument();
        expect(screen.getByLabelText(/current execution/i)).toBeInTheDocument();
    });

    it('renders the per-metric change section', () => {
        renderWith(twoExecutionDataset, <ComparisonView />);
        // Per-metric change is now a CSS grid (matches the v3.1 mockup), not a <table>.
        expect(screen.getByText(/per-metric change/i)).toBeInTheDocument();
    });

    // The metric-name labels also appear in the "Biggest mover" KPI sub-label,
    // so read the per-metric TABLE rows (`.eval-cmp-row`) directly rather than a
    // free-text query.
    const metricRowNames = (container: HTMLElement): string[] =>
        [...container.querySelectorAll('.eval-cmp-row')].map(
            (row) => row.firstElementChild?.textContent?.trim() ?? '',
        );

    it('scopes rows to the sidebar-selected scenario (item 10)', () => {
        // Unscoped: all four metrics from both scenarios are present in the rows.
        const { container } = renderWith(twoExecutionDataset, <ComparisonView />);
        expect(metricRowNames(container).sort()).toEqual(
            ['accuracy', 'coherence', 'fluency', 'safety'],
        );
    });

    it('hides other scenarios once a sidebar scenario is selected (item 10)', async () => {
        const { container } = renderWith(
            twoExecutionDataset,
            <ScenarioSelector scenarioName="Comparison.TextSummary">
                <ComparisonView />
            </ScenarioSelector>,
        );
        // Only Comparison.TextSummary's metrics (coherence, safety) remain in the
        // rows; QAAccuracy's (accuracy, fluency) are scoped out.
        await waitFor(() =>
            expect(metricRowNames(container).sort()).toEqual(['coherence', 'safety']),
        );
    });
});

describe('ComparisonView — singleExecutionDataset (empty state)', () => {
    it('renders the "Needs at least 2 executions" message', () => {
        renderWith(singleExecutionDataset, <ComparisonView />);
        expect(screen.getByText(/needs at least 2 executions/i)).toBeInTheDocument();
    });

    it('does NOT render execution dropdowns', () => {
        renderWith(singleExecutionDataset, <ComparisonView />);
        expect(screen.queryByLabelText(/baseline execution/i)).not.toBeInTheDocument();
        expect(screen.queryByLabelText(/current execution/i)).not.toBeInTheDocument();
    });
});
