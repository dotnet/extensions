// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { describe, it, expect } from 'vitest';
import {
    metricScale,
    posOn,
    formatRaw,
    dumbbellStyles,
    type MetricScaleKind,
} from '../components/history/dumbbellGeometry';

// Pure geometry used by both HistoryView and ComparisonView dumbbell rows.
// These are the exact numeric/string contracts the rendered rows depend on.

describe('metricScale — domain per metric kind', () => {
    it.each<[MetricScaleKind, number, [number, number]]>([
        ['fraction', 3, [0, 1]],
        ['fraction', 0, [0, 1]],
        ['score', 3, [1, 5]],
        ['score', 99, [1, 5]],
        ['severity', 3, [0, 3]],
        // peak `|| 1` guard: a 0 peak collapses to a [0,1] scale, never [0,0].
        ['severity', 0, [0, 1]],
        ['count', 10, [0, 10]],
        // Math.max(1, peak): a sub-1 peak still floors the top at 1.
        ['count', 0.5, [0, 1]],
        ['count', 0, [0, 1]],
    ])('metricScale(%s, %d) === %j', (kind, peak, expected) => {
        expect(metricScale(kind, peak)).toEqual(expected);
    });
});

describe('posOn — value → 0..100 with clamping', () => {
    it.each<[number, number, number, number]>([
        [0.5, 0, 1, 50],
        [0, 0, 1, 0],
        [1, 0, 1, 100],
        // below min clamps to 0
        [-0.5, 0, 1, 0],
        // above max clamps to 100
        [2, 0, 1, 100],
        [3, 1, 5, 50],
        [1, 1, 5, 0],
        [5, 1, 5, 100],
        // degenerate domain (max not > min) falls back to the 50% midpoint
        [5, 5, 5, 50],
    ])('posOn(%d, %d, %d) === %d', (v, min, max, expected) => {
        expect(posOn(v, min, max)).toBe(expected);
    });
});

describe('formatRaw — raw display string per kind', () => {
    it.each<[number, MetricScaleKind, string]>([
        [0.5, 'fraction', '0.500'],
        [0.25, 'fraction', '0.250'],
        [4, 'score', '4/5'],
        [4.5, 'score', '4.5/5'],
        [3, 'count', '3'],
        [3.5, 'count', '3.5'],
        // NOTE: formatRaw gives severity NO '/7' suffix (it shares the count branch) —
        // this diverges from viewModels.formatScore, which renders severity as 'n/7'.
        [2, 'severity', '2'],
        [2.5, 'severity', '2.5'],
    ])('formatRaw(%d, %s) === %s', (v, kind, expected) => {
        expect(formatRaw(v, kind)).toBe(expected);
    });
});

describe('dumbbellStyles — status key + connector/dot geometry', () => {
    const HALO = '0 0 0 2px var(--neutral-background-1)';
    const SUCCESS = 'var(--status-success-background-3)';
    const DANGER = 'var(--status-danger-foreground-2)';
    const NEUTRAL = 'var(--neutral-foreground-4)';

    it('maps dir sign to status key (success / danger / neutral)', () => {
        expect(dumbbellStyles(10, 20, 1, true).sk).toBe('success');
        expect(dumbbellStyles(10, 20, -1, true).sk).toBe('danger');
        expect(dumbbellStyles(10, 20, 0, true).sk).toBe('neutral');
    });

    it('renders the full connector + both dots when there is a prev, a delta, and a gap > connEpsilon', () => {
        const db = dumbbellStyles(20, 80, 1, true);
        expect(db.sk).toBe('success');
        expect(db.connector).toStrictEqual({
            position: 'absolute',
            top: '50%',
            left: '20%',
            width: '60%',
            height: '1.5px',
            transform: 'translateY(-50%)',
            borderRadius: 'var(--radius-circular)',
            background: SUCCESS,
        });
        expect(db.dotB).toStrictEqual({
            position: 'absolute',
            top: '50%',
            left: '20%',
            width: '8px',
            height: '8px',
            boxSizing: 'border-box',
            transform: 'translate(-50%,-50%)',
            borderRadius: '50%',
            background: 'var(--neutral-background-1)',
            border: '1.5px solid var(--neutral-foreground-3)',
            boxShadow: HALO,
        });
        expect(db.dotA).toStrictEqual({
            position: 'absolute',
            top: '50%',
            left: '80%',
            width: '8px',
            height: '8px',
            boxSizing: 'border-box',
            transform: 'translate(-50%,-50%)',
            borderRadius: '50%',
            background: SUCCESS,
            boxShadow: HALO,
        });
    });

    it('hides the connector AND dotB when prevPos is null (first point / no baseline)', () => {
        const db = dumbbellStyles(null, 60, -1, true);
        expect(db.sk).toBe('danger');
        expect(db.connector).toStrictEqual({ display: 'none' });
        expect(db.dotB).toStrictEqual({ display: 'none' });
        // dotA still renders at the current position with the danger color.
        expect(db.dotA.display).toBeUndefined();
        expect(db.dotA.left).toBe('60%');
        expect(db.dotA.background).toBe(DANGER);
    });

    it('hides dotB when prevPos is undefined', () => {
        expect(dumbbellStyles(undefined as unknown as number | null, 40, 1, true).dotB).toStrictEqual({
            display: 'none',
        });
    });

    it('hides the connector when hasDelta is false, but keeps dotB visible (prev exists)', () => {
        const db = dumbbellStyles(20, 80, 0, false);
        expect(db.sk).toBe('neutral');
        expect(db.connector).toStrictEqual({ display: 'none' });
        expect(db.dotB.display).toBeUndefined();
        expect(db.dotB.left).toBe('20%');
        expect(db.dotA.left).toBe('80%');
        expect(db.dotA.background).toBe(NEUTRAL);
    });

    it('hides the connector when the gap is within connEpsilon (dots still shown)', () => {
        // gap 0.005 <= default epsilon 0.01 → connector suppressed, both dots kept.
        const db = dumbbellStyles(50, 50.005, 1, true);
        expect(db.connector).toStrictEqual({ display: 'none' });
        expect(db.dotB.left).toBe('50%');
        expect(db.dotA.left).toBe('50.005%');
    });

    it('honours a connEpsilon of 0 (ComparisonView passes 0): equal positions hide, any gap shows', () => {
        expect(dumbbellStyles(50, 50, 1, true, 0).connector).toStrictEqual({ display: 'none' });
        const shown = dumbbellStyles(50, 51, 1, true, 0).connector;
        expect(shown.display).toBeUndefined();
        expect(shown.left).toBe('50%');
        expect(shown.width).toBe('1%');
    });

    it('clamps out-of-range positions into 0..100 before emitting left offsets', () => {
        const db = dumbbellStyles(-10, 150, 1, true);
        // prv clamps to 0, cur clamps to 100
        expect(db.dotB.left).toBe('0%');
        expect(db.dotA.left).toBe('100%');
        expect(db.connector.left).toBe('0%');
        expect(db.connector.width).toBe('100%');
    });
});
