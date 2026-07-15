// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import React from 'react';
import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { createScoreSummary, ReportContextProvider, HistoryView } from '../components';

const E1 = 'exec-alpha';
const E2 = 'exec-bravo';
const T1 = '2026-01-01T00:00:00.000Z';
const T2 = '2026-02-01T00:00:00.000Z';

const numeric = (name: string, value: number): NumericMetric =>
    ({
        $type: 'numeric',
        name,
        value,
        reason: 'test',
        interpretation: { rating: 'good', failed: false },
        metadata: {},
    }) as NumericMetric;

const row = (executionName: string, creationTime: string): ScenarioRunResult =>
    ({
        scenarioName: 'Group.Scenario',
        iterationName: 'iteration1',
        executionName,
        creationTime,
        messages: [],
        modelResponse: { messages: [] },
        evaluationResult: {
            metrics: {
                hitRateAt1: numeric('Hit Rate@1', 3),
                hitRateDash1: numeric('Hit Rate-1', 4),
            },
        },
        formatVersion: 1 as unknown as int,
    }) as ScenarioRunResult;

const dataset: Dataset = {
    generatorVersion: '0.0.1',
    createdAt: T2,
    scenarioRunResults: [row(E1, T1), row(E2, T2)],
};

const renderHistory = () => {
    const scoreSummary = createScoreSummary(dataset);
    return render(
        <ReportContextProvider dataset={dataset} scoreSummary={scoreSummary}>
            <HistoryView />
        </ReportContextProvider>,
    );
};

describe('HistoryView — metric-tab ids are index-based and unique', () => {
    it('renders a distinct tab per numeric metric, labelled by the metric name', () => {
        // 'Hit Rate@1' / 'Hit Rate-1' are names that WOULD collide if ids were derived by
        // slugifying the name (both slug to "hit-rate-1"). They must still render two tabs.
        renderHistory();
        const tabs = screen.getAllByRole('tab');
        expect(tabs.map((t) => t.textContent)).toEqual(
            expect.arrayContaining(['Hit Rate@1', 'Hit Rate-1']),
        );
        expect(tabs.length).toBe(2);
    });

    it('assigns index-based tab ids (…metric-tab-0, …metric-tab-1), never name-derived slugs', () => {
        renderHistory();
        const ids = screen.getAllByRole('tab').map((t) => t.id);
        expect(ids).toHaveLength(2);
        // ids are `${useId()}metric-tab-${i}` — index positional, so no slug collision is possible.
        expect(ids[0].endsWith('metric-tab-0')).toBe(true);
        expect(ids[1].endsWith('metric-tab-1')).toBe(true);
        // ...and they carry no trace of the (colliding) metric names.
        expect(ids.some((id) => /hit|rate/i.test(id))).toBe(false);
    });

    it('assigns no duplicate DOM ids anywhere in the rendered container', () => {
        const { container } = renderHistory();
        const ids = [...container.querySelectorAll('[id]')].map((el) => el.id);
        expect(ids.length).toBeGreaterThan(0);
        expect(new Set(ids).size).toBe(ids.length);
    });

    it("every [role=tab]'s aria-controls resolves to the rendered tabpanel", () => {
        const { container } = renderHistory();
        const tabs = screen.getAllByRole('tab');
        const tabpanel = screen.getByRole('tabpanel');
        expect(tabs.length).toBe(2);
        for (const tab of tabs) {
            const controlsId = tab.getAttribute('aria-controls');
            expect(controlsId).toBeTruthy();
            const target = container.querySelector(`#${CSS.escape(controlsId!)}`);
            expect(target).not.toBeNull();
            expect(target).toBe(tabpanel);
        }
    });

    it('keeps every tab at tabIndex=0 so the Fluent Mover can arrow-navigate between them', () => {
        renderHistory();
        const tabs = screen.getAllByRole('tab');
        expect(tabs.length).toBe(2);
        for (const tab of tabs) {
            expect((tab as HTMLElement).tabIndex).toBe(0);
        }
    });
});
