// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import React from 'react';
import { describe, it, expect } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { MetricPanel } from '../components/cases/MetricPanel';

// MetricPanel only reads scenario.evaluationResult?.metrics and needs no ReportContext,
// so a bare scenario stub with just the metrics is enough to drive it (this mirrors how
// CasesView mounts <MetricPanel scenario={vm.scenario} />).
const scenarioWith = (metrics: EvaluationResult['metrics']): ScenarioRunResult =>
    ({ evaluationResult: { metrics } }) as unknown as ScenarioRunResult;

const numeric = (
    name: string,
    value: number | undefined,
    rating: EvaluationRating,
    failed: boolean,
    diagnostics?: EvaluationDiagnostic[],
    metadata: { [K: string]: string } = {},
): NumericMetric => ({
    $type: 'numeric',
    name,
    value,
    reason: `Reason for ${name}.`,
    interpretation: { rating, failed },
    diagnostics,
    metadata,
});

const boolean = (name: string, value: boolean, rating: EvaluationRating, failed: boolean): BooleanMetric => ({
    $type: 'boolean',
    name,
    value,
    reason: `Reason for ${name}.`,
    interpretation: { rating, failed },
    metadata: {},
});

const none = (name: string, rating: EvaluationRating): MetricWithNoValue => ({
    $type: 'none',
    name,
    value: undefined,
    reason: `Reason for ${name}.`,
    interpretation: { rating, failed: false },
    metadata: {},
});

// The score/severity track renders one <span> per scale segment inside a single track span;
// filled segments carry a non-empty boxShadow ("aura"), empty ones leave it unset.
const segmentTrack = (button: HTMLElement): HTMLElement => {
    const track = Array.from(button.querySelectorAll('span')).find(
        (el) => el.querySelectorAll(':scope > span').length >= 2,
    );
    if (!track) throw new Error('segment track not found');
    return track as HTMLElement;
};
const filledCount = (track: HTMLElement): number =>
    Array.from(track.querySelectorAll(':scope > span')).filter((s) => (s as HTMLElement).style.boxShadow !== '').length;

describe('MetricPanel — boolean metrics', () => {
    it('renders a ✓ segment and a "Yes" hero for a passing boolean', () => {
        render(<MetricPanel scenario={scenarioWith({ ok: boolean('ok', true, 'exceptional', false) })} />);

        expect(screen.getByText('✓')).toBeInTheDocument();

        fireEvent.click(screen.getByRole('button'));
        expect(screen.getByText('Yes')).toBeInTheDocument();
        expect(screen.getByText('Exceptional')).toBeInTheDocument();
    });

    it('renders a ✗ segment and a "No" hero for a failing-value boolean', () => {
        render(<MetricPanel scenario={scenarioWith({ ok: boolean('ok', false, 'unacceptable', false) })} />);

        expect(screen.getByText('✗')).toBeInTheDocument();

        fireEvent.click(screen.getByRole('button'));
        expect(screen.getByText('No')).toBeInTheDocument();
        expect(screen.getByText('Weak')).toBeInTheDocument();
    });
});

describe('MetricPanel — score metrics', () => {
    it('shows an "N / 5" hero and fills round(goodness * segCount) segments', () => {
        render(<MetricPanel scenario={scenarioWith({ quality: numeric('quality', 4, 'good', false) })} />);

        const button = screen.getByRole('button');
        const track = segmentTrack(button);
        // score: segCount = 5, goodness = 4/5 = 0.8, filled = round(0.8 * 5) = 4
        expect(track.querySelectorAll(':scope > span')).toHaveLength(5);
        expect(filledCount(track)).toBe(4);

        fireEvent.click(button);
        expect(screen.getByText('4 / 5')).toBeInTheDocument();
    });
});

describe('MetricPanel — severity metrics', () => {
    it('inverts goodness for severity (betterLow): a low value fills most segments', () => {
        render(
            <MetricPanel
                scenario={scenarioWith({ toxicity: numeric('toxicity', 2, 'good', false, undefined, { kind: 'severity' }) })}
            />,
        );

        const button = screen.getByRole('button');
        const track = segmentTrack(button);
        // severity: segCount = 7, goodness = (7 - 2)/7 ≈ 0.714, filled = round(0.714 * 7) = 5
        expect(track.querySelectorAll(':scope > span')).toHaveLength(7);
        expect(filledCount(track)).toBe(5);

        fireEvent.click(button);
        expect(screen.getByText('2 / 7')).toBeInTheDocument();
    });
});

describe('MetricPanel — neutral (none / unknown) rating', () => {
    it('renders a neutral "?" track for an unknown-rating metric', () => {
        render(<MetricPanel scenario={scenarioWith({ mystery: numeric('mystery', undefined, 'unknown', false) })} />);

        expect(screen.getByText('?')).toBeInTheDocument();

        fireEvent.click(screen.getByRole('button'));
        expect(screen.getByText('Unknown')).toBeInTheDocument();
    });

    it('renders a neutral "?" track for an inconclusive no-value metric', () => {
        render(<MetricPanel scenario={scenarioWith({ creativity: none('creativity', 'inconclusive') })} />);

        expect(screen.getByText('?')).toBeInTheDocument();

        fireEvent.click(screen.getByRole('button'));
        expect(screen.getByText('Inconclusive')).toBeInTheDocument();
    });
});

describe('MetricPanel — metricFailed product rule', () => {
    it('flips status to failed when a diagnostic severity is "error" even though interpretation.failed === false', () => {
        const metric = numeric('accuracy', 4.5, 'good', false, [
            { severity: 'error', message: 'Missing required crystal-structure detail.' },
        ]);
        render(<MetricPanel scenario={scenarioWith({ accuracy: metric })} />);

        // Collapsed row already advertises the failure through its accessible name.
        const button = screen.getByRole('button', { name: /accuracy, failed/i });
        expect(button).toBeInTheDocument();

        fireEvent.click(button);
        // The expanded panel uses the "failed" copy, not "Why this score?".
        expect(screen.getByText('Why this failed?')).toBeInTheDocument();
        expect(screen.queryByText('Why this score?')).not.toBeInTheDocument();
        // ...and surfaces the diagnostic that triggered the failure.
        expect(screen.getByText('Diagnostics')).toBeInTheDocument();
        expect(screen.getByText('Error')).toBeInTheDocument();
    });

    it('does not flip a passing metric that has no error diagnostics', () => {
        render(<MetricPanel scenario={scenarioWith({ clarity: numeric('clarity', 4, 'good', false) })} />);

        expect(screen.queryByRole('button', { name: /failed/i })).not.toBeInTheDocument();

        fireEvent.click(screen.getByRole('button', { name: /clarity/i }));
        expect(screen.getByText('Why this score?')).toBeInTheDocument();
    });
});

describe('MetricPanel — rating vocabulary and status mapping', () => {
    it('maps EvaluationRating values to their display words', () => {
        render(
            <MetricPanel
                scenario={scenarioWith({
                    excM: numeric('excM', 5, 'exceptional', false),
                    goodM: numeric('goodM', 4, 'good', false),
                    avgM: numeric('avgM', 3, 'average', false),
                    poorM: numeric('poorM', 2, 'poor', false),
                    weakM: numeric('weakM', 1, 'unacceptable', false),
                    incM: none('incM', 'inconclusive'),
                })}
            />,
        );

        // ratingWord() mapping is exposed on each row's accessible name.
        expect(screen.getByRole('button', { name: /excM, Exceptional/ })).toBeInTheDocument();
        expect(screen.getByRole('button', { name: /goodM, Good/ })).toBeInTheDocument();
        expect(screen.getByRole('button', { name: /avgM, Fair/ })).toBeInTheDocument();
        expect(screen.getByRole('button', { name: /poorM, Poor/ })).toBeInTheDocument();
        expect(screen.getByRole('button', { name: /weakM, Weak/ })).toBeInTheDocument();
        expect(screen.getByRole('button', { name: /incM, Inconclusive/ })).toBeInTheDocument();
    });

    it('maps ratings to the status color on the row dot', () => {
        render(
            <MetricPanel
                scenario={scenarioWith({
                    goodM: numeric('goodM', 4, 'good', false),
                    avgM: numeric('avgM', 3, 'average', false),
                    weakM: numeric('weakM', 1, 'unacceptable', false),
                    unkM: numeric('unkM', undefined, 'unknown', false),
                })}
            />,
        );

        const dotStyleOf = (name: string): string => {
            const btn = screen.getByRole('button', { name: new RegExp(name) });
            return btn.querySelector('span span')?.getAttribute('style') ?? '';
        };

        expect(dotStyleOf('goodM')).toContain('status-success-background-3'); // statusKeyOf good -> success
        expect(dotStyleOf('avgM')).toContain('status-warning-foreground-2'); // statusKeyOf average -> warning
        expect(dotStyleOf('weakM')).toContain('status-danger-background-3'); // statusKeyOf unacceptable -> danger
        expect(dotStyleOf('unkM')).toContain('neutral-foreground-4'); // statusKeyOf unknown -> neutral
    });
});

describe('MetricPanel — empty state', () => {
    it('shows a placeholder when the scenario has no metrics', () => {
        render(<MetricPanel scenario={scenarioWith({})} />);
        expect(screen.getByText('No metrics for this case.')).toBeInTheDocument();
    });
});
