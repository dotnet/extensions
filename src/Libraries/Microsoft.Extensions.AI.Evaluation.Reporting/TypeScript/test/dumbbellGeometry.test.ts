// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { describe, it, expect } from 'vitest';
import { posOn, dumbbellStyles } from '../components/history/dumbbellGeometry';
import { axisDomain } from '../components/history/axisDomain';

// Pure geometry used by both HistoryView and ComparisonView dumbbell rows.
// These are the exact numeric/string contracts the rendered rows depend on.

describe('axisDomain — presentation-only chart/dumbbell framing', () => {
    it('frames a genuine [0,1] fraction-shaped series to the unit interval', () => {
        expect(axisDomain([0.2, 0.5, 0.8])).toMatchObject({ min: 0, max: 1, ticks: 5 });
    });

    it('anchors a conforming 1..5 series to the stable [1,5] frame (no per-scenario breathing)', () => {
        expect(axisDomain([1, 2, 3, 4, 5])).toMatchObject({ min: 1, max: 5, ticks: 4 });
    });

    it('expands the anchored frame only for a genuine outlier above 5', () => {
        expect(axisDomain([2, 6, 4])).toMatchObject({ min: 1, max: 6, ticks: 5 });
    });

    it('expands the anchored frame only for a genuine outlier below 1', () => {
        expect(axisDomain([-2, 3])).toMatchObject({ min: -2, max: 5, ticks: 7 });
    });

    it('falls back to the unit frame when given no finite values', () => {
        expect(axisDomain([])).toMatchObject({ min: 0, max: 1, ticks: 5 });
    });

    it('bounds the gridline count for wide numeric ranges instead of one line per integer', () => {
        const pct = axisDomain([0, 40, 100]);
        expect(pct.ticks).toBeLessThanOrEqual(12);
        expect(pct.min).toBeLessThanOrEqual(0);
        expect(pct.max).toBeGreaterThanOrEqual(100);

        const tokens = axisDomain([1200, 3000, 4800]);
        expect(tokens.ticks).toBeLessThanOrEqual(12);
        expect(tokens.min).toBeLessThanOrEqual(1200);
        expect(tokens.max).toBeGreaterThanOrEqual(4800);
    });

    it('routes fmt through the pinned formatNumber policy (settles tick decimals, no denominator)', () => {
        const dom = axisDomain([0.2, 0.5, 0.8]);
        expect(dom.fmt(0.2)).toBe('0.2');
        expect(dom.fmt(1)).toBe('1');
        expect(dom.fmt(0.1 + 0.2)).toBe('0.3');
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

describe('dumbbellStyles — connector/dot geometry', () => {
    const HALO = '0 0 0 2px var(--neutral-background-1)';
    const NEUTRAL_SOLID = 'var(--neutral-foreground-4)';

    it('renders the full connector + both dots when there is a prev, a delta, and a gap > connEpsilon', () => {
        const db = dumbbellStyles(20, 80, true);
        expect(db.sk).toBe('neutral');
        expect(db.connector).toStrictEqual({
            position: 'absolute',
            top: '50%',
            left: '20%',
            width: '60%',
            height: '1.5px',
            transform: 'translateY(-50%)',
            borderRadius: 'var(--radius-circular)',
            background: NEUTRAL_SOLID,
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
            background: NEUTRAL_SOLID,
            boxShadow: HALO,
        });
    });

    it('hides the connector AND dotB when prevPos is null (first point / no baseline)', () => {
        const db = dumbbellStyles(null, 60, true);
        expect(db.sk).toBe('neutral');
        expect(db.connector).toStrictEqual({ display: 'none' });
        expect(db.dotB).toStrictEqual({ display: 'none' });
        // dotA still renders at the current position with the neutral color.
        expect(db.dotA.display).toBeUndefined();
        expect(db.dotA.left).toBe('60%');
        expect(db.dotA.background).toBe(NEUTRAL_SOLID);
    });

    it('hides dotB when prevPos is undefined', () => {
        expect(dumbbellStyles(undefined as unknown as number | null, 40, true).dotB).toStrictEqual({
            display: 'none',
        });
    });

    it('hides the connector when hasDelta is false, but keeps dotB visible (prev exists)', () => {
        const db = dumbbellStyles(20, 80, false);
        expect(db.sk).toBe('neutral');
        expect(db.connector).toStrictEqual({ display: 'none' });
        expect(db.dotB.display).toBeUndefined();
        expect(db.dotB.left).toBe('20%');
        expect(db.dotA.left).toBe('80%');
        expect(db.dotA.background).toBe(NEUTRAL_SOLID);
    });

    it('hides the connector when the gap is within connEpsilon (dots still shown)', () => {
        // gap 0.005 <= default epsilon 0.01 → connector suppressed, both dots kept.
        const db = dumbbellStyles(50, 50.005, true);
        expect(db.connector).toStrictEqual({ display: 'none' });
        expect(db.dotB.left).toBe('50%');
        expect(db.dotA.left).toBe('50.005%');
    });

    it('honours a connEpsilon of 0 (ComparisonView passes 0): equal positions hide, any gap shows', () => {
        expect(dumbbellStyles(50, 50, true, 'neutral', 0).connector).toStrictEqual({ display: 'none' });
        const shown = dumbbellStyles(50, 51, true, 'neutral', 0).connector;
        expect(shown.display).toBeUndefined();
        expect(shown.left).toBe('50%');
        expect(shown.width).toBe('1%');
    });

    it('clamps out-of-range positions into 0..100 before emitting left offsets', () => {
        const db = dumbbellStyles(-10, 150, true);
        // prv clamps to 0, cur clamps to 100
        expect(db.dotB.left).toBe('0%');
        expect(db.dotA.left).toBe('100%');
        expect(db.connector.left).toBe('0%');
        expect(db.connector.width).toBe('100%');
    });
});
