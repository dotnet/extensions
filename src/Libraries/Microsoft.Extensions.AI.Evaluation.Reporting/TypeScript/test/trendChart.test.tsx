// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import React from 'react';
import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { createScoreSummary, ReportContextProvider } from '../components';
import { TrendChart, type BandPoint } from '../components/history/TrendChart';
import { singleExecutionDataset } from './fixtures/richDataset';

// TrendChart pulls only `darkMode` from context, so any valid provider works.
// jsdom has no ResizeObserver, so measuredW stays undefined and the SVG uses the
// 760px fallback width — geometry below is computed against that fixed width.
const renderChart = (ui: React.ReactElement) => {
    const scoreSummary = createScoreSummary(singleExecutionDataset);
    return render(
        <ReportContextProvider dataset={singleExecutionDataset} scoreSummary={scoreSummary}>
            {ui}
        </ReportContextProvider>,
    );
};

const bp = (mean: number, extra: Partial<BandPoint> = {}): BandPoint => ({
    mean,
    median: mean,
    lo: mean,
    hi: mean,
    n: 1,
    ...extra,
});

// The per-run mean dots are the only <circle>s that carry a <title>; median dots do not.
const meanDots = (c: HTMLElement): SVGCircleElement[] =>
    [...c.querySelectorAll('circle')].filter((el) => el.querySelector('title')) as SVGCircleElement[];

describe('TrendChart — point geometry (760px fallback width, score domain 1..5)', () => {
    it('maps a two-point series to the correct x (edges) and y (domain) coordinates', () => {
        // W=760, PAD_L=34, PAD_R=12 → xOf(0)=34, xOf(1)=34+(760-46)=748
        // H=260, PAD_T=12, PAD_B=22 → span 226; yOf(1)=238 (bottom), yOf(5)=12 (top)
        const { container } = renderChart(
            <TrendChart points={[bp(1), bp(5)]} kind="score" ariaLabel="Accuracy trend" />,
        );
        const dots = meanDots(container);
        expect(dots).toHaveLength(2);
        expect(dots[0].getAttribute('cx')).toBe('34');
        expect(dots[0].getAttribute('cy')).toBe('238');
        expect(dots[1].getAttribute('cx')).toBe('748');
        expect(dots[1].getAttribute('cy')).toBe('12');
        expect(dots[0].querySelector('title')?.textContent).toBe('R1: mean 1.0');
        expect(dots[1].querySelector('title')?.textContent).toBe('R2: mean 5.0');
    });

    it('centres a single point horizontally (n<=1) instead of pinning it to the left edge', () => {
        // xOf(0) = PAD_L + (W-PAD_L-PAD_R)/2 = 34 + 714/2 = 391
        // yOf(3) = 12 + (1 - (3-1)/4)*226 = 12 + 113 = 125
        const { container } = renderChart(
            <TrendChart points={[bp(3, { lo: 2, hi: 4 })]} kind="score" ariaLabel="Single" />,
        );
        const dots = meanDots(container);
        expect(dots).toHaveLength(1);
        expect(dots[0].getAttribute('cx')).toBe('391');
        expect(dots[0].getAttribute('cy')).toBe('125');
    });

    it('draws the min–max spread as a filled band polygon for a multi-point series', () => {
        const { container } = renderChart(
            <TrendChart points={[bp(2, { lo: 1, hi: 3 }), bp(4, { lo: 3, hi: 5 })]} kind="score" ariaLabel="Band" />,
        );
        expect(container.querySelector('polygon')).not.toBeNull();
    });
});

describe('TrendChart — accessibility + legend toggle', () => {
    it('exposes the SVG as role=img with the supplied aria-label', () => {
        renderChart(<TrendChart points={[bp(3)]} kind="score" ariaLabel="Latency trend" />);
        expect(screen.getByRole('img', { name: 'Latency trend' })).toBeInTheDocument();
    });

    it('renders nothing when there are no points', () => {
        const { container } = renderChart(<TrendChart points={[]} kind="score" ariaLabel="Empty" />);
        expect(container.querySelector('svg')).toBeNull();
    });

    it('shows the three-item legend by default', () => {
        renderChart(<TrendChart points={[bp(3)]} kind="score" ariaLabel="Legend" />);
        expect(screen.getByText('Mean per run')).toBeInTheDocument();
        expect(screen.getByText('Median per run')).toBeInTheDocument();
        expect(screen.getByText(/spread across cases/)).toBeInTheDocument();
    });

    it('omits the legend when showLegend is false', () => {
        renderChart(<TrendChart points={[bp(3)]} kind="score" ariaLabel="NoLegend" showLegend={false} />);
        expect(screen.queryByText('Mean per run')).not.toBeInTheDocument();
        expect(screen.queryByText('Median per run')).not.toBeInTheDocument();
    });
});
