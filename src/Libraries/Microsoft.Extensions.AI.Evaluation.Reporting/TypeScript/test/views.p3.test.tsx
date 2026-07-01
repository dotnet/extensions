// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import React from 'react';
import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { createScoreSummary } from '../components/Summary';
import { ReportContextProvider } from '../components/ReportContext';
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

    it('renders run history table', () => {
        renderWith(twoExecutionDataset, <HistoryView />);
        const tables = screen.getAllByRole('table');
        expect(tables.length).toBeGreaterThan(0);
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

    it('renders the per-metric change table', () => {
        renderWith(twoExecutionDataset, <ComparisonView />);
        const tables = screen.queryAllByRole('table');
        expect(tables.length).toBeGreaterThan(0);
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
