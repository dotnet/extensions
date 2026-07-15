// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import React from 'react';
import { describe, it, expect } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { createScoreSummary, ReportContextProvider, useReportContext, type ReportContextType } from '../components';
import { OverviewView } from '../components/overview/OverviewView';

const numeric = (name: string, value: number, rating: EvaluationRating, failed: boolean, better?: string): NumericMetric =>
    ({
        $type: 'numeric',
        name,
        value,
        reason: 'test',
        interpretation: { rating, failed },
        metadata: better ? { better } : {},
    }) as NumericMetric;

const run = (
    scenarioName: string,
    executionName: string,
    creationTime: string,
    metrics: Record<string, NumericMetric>,
): ScenarioRunResult =>
    ({
        scenarioName,
        iterationName: 'iteration1',
        executionName,
        creationTime,
        messages: [],
        modelResponse: { messages: [] },
        evaluationResult: { metrics },
        formatVersion: 1 as unknown as int,
    }) as ScenarioRunResult;

const T_OLD = '2026-01-01T00:00:00.000Z';
const T_NEW = '2026-02-01T00:00:00.000Z';
const NEW = 'run-new';
const OLD = 'run-old';

// primaryResult / activeExecution === the FIRST-inserted execution. To exercise the
// two-pane "biggest movers" branch, the chronologically-later run must be inserted
// first so it becomes active AND has a predecessor (compareLabel) to diff against.
const moversDataset: Dataset = {
    generatorVersion: '0.0.1',
    createdAt: T_NEW,
    scenarioRunResults: [
        run('Alpha.Retrieval', NEW, T_NEW, {
            accuracy: numeric('accuracy', 4, 'good', false),
            safety: numeric('safety', 2, 'poor', true), // weak + failed → needs attention
            coverage: numeric('coverage', 3, 'good', false), // unchanged → zero-delta mover
        }),
        run('Alpha.Retrieval', OLD, T_OLD, {
            accuracy: numeric('accuracy', 2, 'poor', false),
            safety: numeric('safety', 5, 'exceptional', false),
            coverage: numeric('coverage', 3, 'good', false),
        }),
    ],
};

// Single execution → no predecessor → no movers → single-pane layout.
// The lone metric is 'average' (fair) so a needs-attention row still renders.
const noMoversDataset: Dataset = {
    generatorVersion: '0.0.1',
    createdAt: T_NEW,
    scenarioRunResults: [
        run('Beta.QA', 'only', T_NEW, {
            clarity: numeric('clarity', 3, 'average', false),
        }),
    ],
};

const renderOverview = (dataset: Dataset, extra?: React.ReactNode) => {
    const scoreSummary = createScoreSummary(dataset);
    return render(
        <ReportContextProvider dataset={dataset} scoreSummary={scoreSummary}>
            <OverviewView />
            {extra}
        </ReportContextProvider>,
    );
};

describe('OverviewView — pass-rate hero', () => {
    it('renders the overall pass-rate KPI (eyebrow + progress bar with a percentage)', () => {
        renderOverview(moversDataset);
        expect(screen.getByText('Overall pass rate')).toBeInTheDocument();
        expect(screen.getByLabelText(/Overall pass rate \d+%/)).toBeInTheDocument();
        expect(screen.getByText('Cases failing')).toBeInTheDocument();
    });
});

describe('OverviewView — movers vs needs-attention layout switch', () => {
    it('uses the two-pane layout when there are movers (predecessor exists)', () => {
        const { container } = renderOverview(moversDataset);
        expect(container.querySelector('.eval-twopane')).not.toBeNull();
        expect(screen.getByText('Biggest movers')).toBeInTheDocument();
        expect(screen.getByText('Needs attention')).toBeInTheDocument();
    });

    it('uses the single-pane layout (no movers) when the active run has no predecessor', () => {
        const { container } = renderOverview(noMoversDataset);
        expect(container.querySelector('.eval-twopane')).toBeNull();
        expect(screen.queryByText('Biggest movers')).not.toBeInTheDocument();
        expect(screen.getByText('Needs attention')).toBeInTheDocument();
        // fair rating surfaces a needs-attention row even without a comparison run
        expect(screen.getByText('Beta.QA · clarity')).toBeInTheDocument();
    });
});

describe('OverviewView — needs-attention row + View action', () => {
    it('lists a weak/failing metric as a needs-attention row', () => {
        renderOverview(moversDataset);
        // The label also appears as a mover, so scope to the needs-attention row's own class.
        const attnNames = [...document.querySelectorAll('.eval-attn-name')].map((e) => e.textContent);
        expect(attnNames).toContain('Alpha.Retrieval · safety');
        expect(screen.getByRole('button', { name: 'View cases for Alpha.Retrieval · safety' })).toBeInTheDocument();
    });

    it('clicking "View" opens cases for that scenario (setView(cases) + selectScenarioLevel)', () => {
        let ctx: ReportContextType | undefined;
        const Probe = () => {
            ctx = useReportContext();
            return null;
        };
        renderOverview(moversDataset, <Probe />);

        expect(ctx?.view).toBe('overview');
        expect(ctx?.selectedScenarioLevel).toBeUndefined();

        fireEvent.click(screen.getByRole('button', { name: 'View cases for Alpha.Retrieval · safety' }));

        expect(ctx?.view).toBe('cases');
        expect(ctx?.selectedScenarioLevel).toBeTruthy();
        expect(ctx?.selectedScenarioLevel).toContain('Alpha');
    });
});

describe('OverviewView — mover delta rendering', () => {
    it('renders a zero-delta mover as a plain em dash (no ▲/▼ badge)', () => {
        renderOverview(moversDataset);
        // coverage is identical across runs → numDeltaChip returns the informative "—".
        const coverage = screen.getByText('Alpha.Retrieval · coverage').closest('span');
        const rowRoot = coverage?.parentElement?.parentElement; // grid emits 3 sibling cells per mover
        expect(rowRoot?.textContent).toContain('—');
    });

    it('colours movers by the SIGN of the delta only — it does NOT honour metadata.better', () => {
        // OverviewView.numDeltaChip is sign-based: a decrease is always "danger", an
        // increase always "success", regardless of a metric's better:'low'. The
        // better:'low' / better:'none' direction inversion lives in ComparisonView /
        // HistoryView (see views.test.tsx), not here.
        const inverted: Dataset = {
            generatorVersion: '0.0.1',
            createdAt: T_NEW,
            scenarioRunResults: [
                run('Gamma.Toxicity', NEW, T_NEW, {
                    toxicity: numeric('toxicity', 1, 'exceptional', false, 'low'), // big drop = genuine improvement
                }),
                run('Gamma.Toxicity', OLD, T_OLD, {
                    toxicity: numeric('toxicity', 5, 'unacceptable', true, 'low'),
                }),
            ],
        };
        renderOverview(inverted);
        // Scope to the toxicity mover's own delta cell (KPI chips elsewhere also use arrows).
        // MoversCard emits 3 sibling grid cells per mover: [name, value, delta].
        const nameCell = screen.getByText('Gamma.Toxicity · toxicity').parentElement;
        const deltaCell = nameCell?.nextElementSibling?.nextElementSibling;
        // delta = 1 - 5 = -4 → a downward ▼ badge (danger), NOT flipped to ▲/success by better:'low'.
        expect(deltaCell?.textContent).toContain('▼');
        expect(deltaCell?.textContent).not.toContain('▲');
    });
});
