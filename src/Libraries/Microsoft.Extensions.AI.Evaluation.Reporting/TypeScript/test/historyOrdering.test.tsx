// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import React from 'react';
import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { createScoreSummary } from '../components/Summary';
import { ReportContextProvider } from '../components/ReportContext';
import { HistoryView } from '../components/HistoryView';

const E1 = 'EvaluationRun-alpha'; // chronologically earliest
const E2 = 'EvaluationRun-bravo'; // middle
const E3 = 'EvaluationRun-charlie'; // chronologically latest

const T1 = '2026-01-01T00:00:00.000Z';
const T2 = '2026-02-01T00:00:00.000Z';
const T3 = '2026-03-01T00:00:00.000Z';

const numeric = (name: string, value: number): NumericMetric =>
    ({
        $type: 'numeric',
        name,
        value,
        reason: 'test',
        interpretation: { rating: 'good', failed: false },
        metadata: {},
    }) as NumericMetric;

const row = (
    executionName: string,
    creationTime: string,
    quality: number,
): ScenarioRunResult =>
    ({
        scenarioName: 'Group.Scenario',
        iterationName: 'iteration1',
        executionName,
        creationTime,
        messages: [],
        modelResponse: { messages: [] },
        evaluationResult: { metrics: { quality: numeric('quality', quality) } },
        formatVersion: 1 as unknown as int,
    }) as ScenarioRunResult;

// INSERTION ORDER: newest-first (E3, E2, E1) — the reverse of chronological.
const dataset: Dataset = {
    generatorVersion: '0.0.1',
    createdAt: T3,
    scenarioRunResults: [
        row(E3, T3, 4),
        row(E2, T2, 3),
        row(E1, T1, 2),
    ],
};

const renderHistory = () => {
    const scoreSummary = createScoreSummary(dataset);
    return render(
        <ReportContextProvider dataset={dataset} scoreSummary={scoreSummary}>
            <HistoryView />
        </ReportContextProvider>,
    );
};

// Read the "Execution" column of the run-history rows, top → bottom, skipping
// the header row ("Execution").
const runExecColumn = (): string[] =>
    [...document.querySelectorAll('.eval-tscroll .eval-grid4')]
        .map((r) => (r.querySelector(':scope > span')?.textContent ?? '').trim())
        .filter((t) => t && t !== 'Execution');

const statValue = (label: string): string => {
    const card = [...document.querySelectorAll('.eval-hist-stats > div')].find(
        (d) => d.querySelector('div')?.textContent?.trim() === label,
    );
    return (card?.querySelectorAll('div')[1]?.textContent ?? '').trim();
};

describe('HistoryView — chronological ordering', () => {
    it('renders run-history rows oldest-first even when inserted newest-first', () => {
        renderHistory();
        // Chronological order is E1 (oldest) → E2 → E3 (newest); insertion order
        // was E3, E2, E1. An insertion-order impl would render [E3, E2, E1].
        expect(runExecColumn()).toEqual([E1, E2, E3]);
    });

    it('labels First run score = oldest run and Last run score = newest run', () => {
        renderHistory();
        // E1 (oldest) mean = 2 → "2/5"; E3 (newest) mean = 4 → "4/5".
        expect(statValue('First run score')).toBe('2/5');
        expect(statValue('Last run score')).toBe('4/5');
    });

    it('reports Net change as newest − oldest (an improvement, not a decline)', () => {
        renderHistory();
        // Chronological delta = 4 (newest) − 2 (oldest) = +2 → "▲ 2.0". Under the
        // backwards insertion order this read "▼ 2.0" (a spurious decline).
        expect(statValue('Net change')).toBe('▲ 2.0');
    });

    it("the middle row's dumbbell baselines against the chronologically-earlier run", () => {
        renderHistory();
        const rows = [...document.querySelectorAll('.eval-tscroll .eval-grid4')].filter(
            (r) => (r.querySelector(':scope > span')?.textContent ?? '').trim() === E2,
        );
        expect(rows.length).toBe(1);
        const middle = rows[0];

        // Change cell = delta vs the chronological predecessor: E1(2) → E2(3) = "▲ 1.0"
        // (an E3-mean-4 baseline would instead read "▼ 1.0").
        const changeSpan = [...middle.querySelectorAll('span')].find(
            (s) => /[▲▼]/.test(s.textContent ?? '') && s.children.length === 0,
        );
        expect(changeSpan?.textContent?.trim()).toBe('▲ 1.0');

        // The hollow "previous" dot (dotB) must sit LEFT of the filled "current" dot (dotA):
        // prev = E1 (mean 2) < cur = E2 (mean 3), proving the connector runs earlier → later.
        const track = middle.querySelector('span[style*="min-width"]');
        const lefts = track
            ? [...track.querySelectorAll('span')]
                  .map((s) => s.getAttribute('style') ?? '')
                  .map((st) => /width:\s*8px/.test(st) ? st.match(/left:\s*([\d.]+)%/)?.[1] : undefined)
                  .filter((v): v is string => v !== undefined)
                  .map(Number)
            : [];
        // Two dots (dotB hollow prev, dotA filled cur). dotB (prev, E1) is emitted
        // before dotA (cur, E2); prev% must be strictly less than cur%.
        expect(lefts.length).toBe(2);
        const [prevPct, curPct] = lefts;
        expect(prevPct).toBeLessThan(curPct);
    });
});
