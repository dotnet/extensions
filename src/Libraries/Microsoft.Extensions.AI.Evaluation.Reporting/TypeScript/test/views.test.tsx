// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import React, { useEffect } from 'react';
import { describe, it, expect } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { createScoreSummary, ReportContextProvider, useReportContext, HistoryView, ComparisonView } from '../components';
import { twoExecutionDataset, singleExecutionDataset } from './fixtures/richDataset';

const renderWith = (dataset: Dataset, ui: React.ReactElement) => {
    const scoreSummary = createScoreSummary(dataset);
    return render(
        <ReportContextProvider dataset={dataset} scoreSummary={scoreSummary}>
            {ui}
        </ReportContextProvider>,
    );
};

const ScenarioSelector = ({ scenarioName, children }: { scenarioName: string; children: React.ReactNode }) => {
    const { activeNode, selectedScenarioLevel, selectScenarioLevel } = useReportContext();
    const targetKey = activeNode.flattenedNodes.find(
        (n) => n.isLeafNode && n.scenario?.scenarioName === scenarioName,
    )?.nodeKey;
    useEffect(() => {
        if (targetKey && selectedScenarioLevel !== targetKey) {
            selectScenarioLevel(targetKey);
        }
    }, [targetKey, selectedScenarioLevel]);
    return <>{children}</>;
};

describe('HistoryView — twoExecutionDataset', () => {
    it('renders one metric tab per numeric metric of the default scenario', () => {
        // Default scenario = first leaf of the primary execution (Comparison.TextSummary),
        // whose numeric metrics with ≥2-execution history are coherence and safety.
        renderWith(twoExecutionDataset, <HistoryView />);
        const tabs = screen.getAllByRole('tab');
        expect(tabs.map((t) => t.textContent)).toEqual(['coherence', 'safety']);
    });

    it('renders exactly one trend chart (role=img) labelled for the active metric + scenario', () => {
        renderWith(twoExecutionDataset, <HistoryView />);
        const charts = screen.getAllByRole('img');
        expect(charts.length).toBe(1);
        expect(charts[0]).toHaveAttribute(
            'aria-label',
            expect.stringMatching(/trend across executions for Comparison\./),
        );
    });

    it('renders the run history section', () => {
        renderWith(twoExecutionDataset, <HistoryView />);
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
        expect(screen.getByText(/per-metric change/i)).toBeInTheDocument();
    });

    const metricRowNames = (container: HTMLElement): string[] =>
        [...container.querySelectorAll('[role="rowgroup"] .eval-grid3[role="row"]')].map(
            (row) => row.firstElementChild?.textContent?.trim() ?? '',
        );

    it('renders all metric rows when no scenario is selected', () => {
        const { container } = renderWith(twoExecutionDataset, <ComparisonView />);
        expect(metricRowNames(container).sort()).toEqual(
            ['accuracy', 'coherence', 'fluency', 'safety'],
        );
    });

    it('hides other scenarios once a sidebar scenario is selected', async () => {
        const { container } = renderWith(
            twoExecutionDataset,
            <ScenarioSelector scenarioName="Comparison.TextSummary">
                <ComparisonView />
            </ScenarioSelector>,
        );
        await waitFor(() =>
            expect(metricRowNames(container).sort()).toEqual(['coherence', 'safety']),
        );
    });
});

describe('ComparisonView — better direction inversion (lower-is-better / none)', () => {
    const inv = (name: string, value: number, better: 'high' | 'low' | 'none'): NumericMetric =>
        ({
            $type: 'numeric',
            name,
            value,
            reason: 'test',
            interpretation: { rating: 'good', failed: false },
            metadata: { better },
        }) as NumericMetric;

    const invRow = (executionName: string, creationTime: string, metrics: Record<string, NumericMetric>): ScenarioRunResult =>
        ({
            scenarioName: 'Inv.Scenario',
            iterationName: 'iteration1',
            executionName,
            creationTime,
            messages: [],
            modelResponse: { messages: [] },
            evaluationResult: { metrics },
            formatVersion: 1 as unknown as int,
        }) as ScenarioRunResult;

    // Baseline (older) → current (newer). toxicity DROPS 5→2; with better:'low' that is an
    // improvement. flat CHANGES 3→5 but better:'none' means neither improved nor regressed.
    const inversionDataset: Dataset = {
        generatorVersion: '0.0.1',
        createdAt: '2026-04-01T00:00:00.000Z',
        scenarioRunResults: [
            invRow('exec-old', '2026-03-01T00:00:00.000Z', { toxicity: inv('toxicity', 5, 'low'), flat: inv('flat', 3, 'none') }),
            invRow('exec-new', '2026-04-01T00:00:00.000Z', { toxicity: inv('toxicity', 2, 'low'), flat: inv('flat', 5, 'none') }),
        ],
    };

    it('counts a decrease in a lower-is-better metric as an improvement, not a regression', () => {
        renderWith(inversionDataset, <ComparisonView />);
        // toxicity 5→2 (▼) is the single improvement; the better:'none' metric is not counted.
        expect(screen.getByText('Metrics improved').nextElementSibling?.textContent).toBe('1');
        expect(screen.getByText('Metrics regressed').nextElementSibling?.textContent).toBe('0');
    });

    it('surfaces the downward toxicity delta as the biggest (improving) mover', () => {
        renderWith(inversionDataset, <ComparisonView />);
        const biggest = screen.getByText('Biggest mover');
        expect(biggest.nextElementSibling?.textContent).toContain('▼');
        expect(biggest.nextElementSibling?.nextElementSibling?.textContent).toBe('toxicity');
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
