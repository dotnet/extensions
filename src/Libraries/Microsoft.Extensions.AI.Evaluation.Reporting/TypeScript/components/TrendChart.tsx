// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { useEffect, useRef, useState } from 'react';
import { useReportContext } from './ReportContext';

// A single execution's per-metric distribution across its cases: the mean +
// median lines and the min/max spread band are all derived from these.
export type BandPoint = {
    mean: number;
    median: number;
    lo: number;
    hi: number;
    n: number;
};

// Metric axis kind. `score` spans a fixed 1–5 axis, `fraction` a fixed 0–1
// axis; `count` (or anything else) is data-driven.
export type MetricKind = 'score' | 'fraction' | 'count';

type Domain = {
    min: number;
    max: number;
    fmt: (v: number) => string;
    unit: string;
    ticks: number;
};

// y-domain per kind. Bounded scales (score 1–5, fraction 0–1) always span their
// full declared range so the axis shows every possible value; the unit ("/5")
// rides the top tick.
const domainFor = (kind: MetricKind, pts: (BandPoint | undefined)[]): Domain => {
    if (kind !== 'score' && kind !== 'fraction') {
        let lo = Infinity;
        let hi = -Infinity;
        for (const p of pts) {
            if (p) {
                lo = Math.min(lo, p.lo);
                hi = Math.max(hi, p.hi);
            }
        }
        if (!Number.isFinite(lo)) {
            lo = 0;
            hi = 1;
        }
        const pad = Math.max(1, (hi - lo) * 0.2);
        lo = Math.max(0, Math.floor(lo - pad));
        hi = Math.ceil(hi + pad);
        if (lo === hi) hi = lo + 1;
        return { min: lo, max: hi, fmt: (v) => `${Math.round(v)}`, unit: '', ticks: 4 };
    }
    const score = kind === 'score';
    const min = score ? 1 : 0;
    const max = score ? 5 : 1;
    const unit = score ? '/5' : '';
    const fmt = score ? (v: number) => v.toFixed(0) : (v: number) => v.toFixed(1);
    const ticks = score ? Math.max(1, Math.round(max - min)) : Math.max(1, Math.round((max - min) / 0.2));
    return { min, max, fmt, unit, ticks };
};

// Fallback plot width (px) when the wrapper hasn't been measured yet (SSR /
// tests / first paint). Equals the previous fixed viewBox width, so unmeasured
// renders are byte-for-byte identical to the old fixed-760 output.
const W_FALLBACK = 760;
const H = 260;
const PAD_L = 34;
const PAD_R = 12;
const PAD_T = 12;
const PAD_B = 22;

export type TrendChartProps = {
    points: (BandPoint | undefined)[];
    kind: MetricKind;
    ariaLabel: string;
    showLegend?: boolean;
};

// mean line + dashed median line + min/max spread band per metric, across
// executions. Mirrors the mockup's bandChart + chartLegend. Every color is a DS
// token so it flips in dark mode; the band opacity is the only value that keys
// off the theme directly.
export const TrendChart = ({ points, kind, ariaLabel, showLegend = true }: TrendChartProps) => {
    const { darkMode } = useReportContext();

    // Measure the wrapper and drive the SVG's user-space width from it, so 1
    // user unit = 1 CSS px at any container width. With a fixed viewBox the
    // fixed `fontSize`/`strokeWidth` user units scaled with the container; by
    // making viewBox width == rendered px width, tick fonts and line strokes
    // stay a literal, constant px size regardless of window width. `xOf`
    // already interpolates across `W - PAD`, so the plot reflows.
    const wrapRef = useRef<HTMLDivElement | null>(null);
    const [measuredW, setMeasuredW] = useState<number | undefined>(undefined);

    useEffect(() => {
        const el = wrapRef.current;
        // ResizeObserver is a browser-only enhancement; guard it so non-DOM envs
        // (tests/SSR) no-op gracefully and fall back to the fixed W below.
        if (!el || typeof ResizeObserver === 'undefined') return;
        const ro = new ResizeObserver((entries) => {
            const w = entries[0]?.contentRect.width;
            if (w && w > 0) setMeasuredW(w);
        });
        ro.observe(el);
        return () => ro.disconnect();
    }, []);

    if (points.length === 0) return null;

    const W = measuredW && measuredW > 0 ? measuredW : W_FALLBACK;

    const color = 'var(--brand-foreground-1)';
    const medColor = 'var(--neutral-foreground-3)';

    const dom = domainFor(kind, points);
    const n = points.length;
    const xOf = (i: number) =>
        PAD_L + (n <= 1 ? (W - PAD_L - PAD_R) / 2 : (i * (W - PAD_L - PAD_R)) / (n - 1));
    const yOf = (v: number) => PAD_T + (1 - (v - dom.min) / (dom.max - dom.min)) * (H - PAD_T - PAD_B);

    const gridEls: React.ReactNode[] = [];
    for (let g = 0; g <= dom.ticks; g++) {
        const v = dom.min + ((dom.max - dom.min) * g) / dom.ticks;
        const y = yOf(v);
        gridEls.push(
            <line key={`g${g}`} x1={PAD_L} y1={y} x2={W - PAD_R} y2={y} strokeWidth={1} style={{ stroke: 'var(--neutral-stroke-2)' }} />,
        );
        gridEls.push(
            <text key={`gl${g}`} x={PAD_L - 6} y={y + 3} textAnchor="end" fontSize={10} style={{ fill: 'var(--neutral-foreground-4)' }}>
                {dom.fmt(v) + (g === dom.ticks ? dom.unit : '')}
            </text>,
        );
    }

    const valid = points.map((p, i) => ({ p, i })).filter((o): o is { p: BandPoint; i: number } => !!o.p);

    const bandEls: React.ReactNode[] = [];
    if (valid.length > 1) {
        const up = valid.map((o) => `${xOf(o.i)},${yOf(o.p.hi)}`);
        const dn = valid.slice().reverse().map((o) => `${xOf(o.i)},${yOf(o.p.lo)}`);
        bandEls.push(
            <polygon
                key="band"
                points={up.concat(dn).join(' ')}
                style={{ fill: color, fillOpacity: darkMode ? 0.2 : 0.13, stroke: color, strokeOpacity: 0.32, strokeWidth: 1 }}
            />,
        );
    } else if (valid.length === 1) {
        const o = valid[0];
        bandEls.push(
            <line key="band1" x1={xOf(o.i)} y1={yOf(o.p.lo)} x2={xOf(o.i)} y2={yOf(o.p.hi)} strokeWidth={6} strokeLinecap="round" style={{ stroke: color, strokeOpacity: 0.3 }} />,
        );
    }

    const medEls: React.ReactNode[] = [];
    if (valid.length > 1) {
        medEls.push(
            <polyline key="median" points={valid.map((o) => `${xOf(o.i)},${yOf(o.p.median)}`).join(' ')} fill="none" strokeWidth={1.5} strokeDasharray="5 3" strokeLinejoin="round" strokeLinecap="round" style={{ stroke: medColor }} />,
        );
    } else if (valid.length === 1) {
        const o = valid[0];
        medEls.push(
            <line key="median1" x1={xOf(o.i) - 7} y1={yOf(o.p.median)} x2={xOf(o.i) + 7} y2={yOf(o.p.median)} strokeWidth={1.5} strokeDasharray="5 3" strokeLinecap="round" style={{ stroke: medColor }} />,
        );
    }
    for (const o of valid) {
        medEls.push(
            <circle key={`md${o.i}`} cx={xOf(o.i)} cy={yOf(o.p.median)} r={3.25} strokeWidth={1.5} style={{ fill: 'var(--neutral-background-1)', stroke: medColor }} />,
        );
    }

    const meanEls: React.ReactNode[] = [];
    if (valid.length > 1) {
        meanEls.push(
            <polyline key="mean" points={valid.map((o) => `${xOf(o.i)},${yOf(o.p.mean)}`).join(' ')} fill="none" strokeWidth={2} strokeLinejoin="round" strokeLinecap="round" style={{ stroke: color }} />,
        );
    }
    for (const o of valid) {
        meanEls.push(
            <circle key={`d${o.i}`} cx={xOf(o.i)} cy={yOf(o.p.mean)} r={3.25} strokeWidth={1.5} style={{ fill: 'var(--neutral-background-1)', stroke: color }}>
                <title>{`R${o.i + 1}: mean ${o.p.mean.toFixed(kind === 'fraction' ? 3 : 1)}`}</title>
            </circle>,
        );
    }

    const xLabels = points.map((_p, i) => (
        <text key={`xl${i}`} x={xOf(i)} y={H - 6} textAnchor="middle" fontSize={10} style={{ fill: 'var(--neutral-foreground-4)' }}>
            {`R${i + 1}`}
        </text>
    ));

    // hollow donut swatch matching the chart marker geometry (r=3.25 + 1.5px stroke → 8px painted, = dumbbell dot)
    const donut = (stroke: string): React.CSSProperties => ({
        position: 'absolute',
        left: '50%',
        top: '50%',
        width: '8px',
        height: '8px',
        borderRadius: '50%',
        background: 'var(--neutral-background-1)',
        border: `1.5px solid ${stroke}`,
        transform: 'translate(-50%,-50%)',
        boxSizing: 'border-box',
    });

    return (
        <div ref={wrapRef} style={{ display: 'flex', flexDirection: 'column' }}>
            <svg
                role="img"
                aria-label={ariaLabel}
                width={W}
                height={H}
                viewBox={`0 0 ${W} ${H}`}
                style={{ width: '100%', height: 'auto', display: 'block' }}
            >
                {gridEls}
                {bandEls}
                {medEls}
                {meanEls}
                {xLabels}
            </svg>

            {showLegend && (
                <div style={{ display: 'flex', gap: 'var(--spacing-xl)', alignItems: 'center', flexWrap: 'wrap', marginTop: 'var(--spacing-m)', paddingLeft: '34px' }}>
                    <span style={{ display: 'inline-flex', alignItems: 'center', gap: 'var(--spacing-s)' }}>
                        <span style={{ position: 'relative', display: 'inline-block', width: '22px', height: '2px', background: color, borderRadius: '2px' }}>
                            <span style={donut(color)} />
                        </span>
                        <span style={{ fontSize: 'var(--font-size-200)', color: 'var(--neutral-foreground-3)' }}>Mean per run</span>
                    </span>
                    <span style={{ display: 'inline-flex', alignItems: 'center', gap: 'var(--spacing-s)' }}>
                        <span style={{ position: 'relative', display: 'inline-block', width: '22px', height: '5.5px' }}>
                            <span style={{ position: 'absolute', left: 0, right: 0, top: '50%', height: 0, borderTop: '1.5px dashed var(--neutral-foreground-3)', transform: 'translateY(-50%)' }} />
                            <span style={donut('var(--neutral-foreground-3)')} />
                        </span>
                        <span style={{ fontSize: 'var(--font-size-200)', color: 'var(--neutral-foreground-3)' }}>Median per run</span>
                    </span>
                    <span style={{ display: 'inline-flex', alignItems: 'center', gap: 'var(--spacing-s)' }}>
                        <span
                            style={{
                                display: 'inline-block',
                                width: '22px',
                                height: '13px',
                                borderRadius: '3px',
                                background: `color-mix(in srgb, ${color} 13%, transparent)`,
                                border: `1px solid color-mix(in srgb, ${color} 32%, transparent)`,
                                boxSizing: 'border-box',
                            }}
                        />
                        <span style={{ fontSize: 'var(--font-size-200)', color: 'var(--neutral-foreground-3)' }}>Min–max spread across cases</span>
                    </span>
                </div>
            )}
        </div>
    );
};
