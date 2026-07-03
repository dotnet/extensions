// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { useLayoutEffect, useMemo, useRef, useState } from 'react';
import { Card, makeStyles, mergeClasses } from '@fluentui/react-components';
import { useReportContext } from '../core/ReportContext';
import { useReportStyles } from '../styles/reportStyles';
import { metricHistoryForScenario, chronologicalExecutions } from '../core/viewModels';
import { TrendChart, type BandPoint, type MetricKind } from './TrendChart';
import { metricScale, posOn, formatRaw, STATUS_TEXT, dumbbellStyles } from './dumbbellGeometry';

const metricKindOf = (metric: BaseEvaluationMetric, sampleValue: number): MetricKind => {
    const hint = metric.metadata?.kind;
    if (hint === 'score' || hint === 'fraction' || hint === 'count') return hint;
    if (sampleValue >= 0 && sampleValue <= 1) return 'fraction';
    if (Number.isInteger(sampleValue) && sampleValue >= 1 && sampleValue <= 5) return 'score';
    return 'score';
};

// "high" (default) means larger-is-better; "low" flips improvement direction;
// "none" means no direction (neutral deltas).
const metricBetterOf = (metric: BaseEvaluationMetric): 'high' | 'low' | 'none' => {
    const b = metric.metadata?.better;
    return b === 'low' || b === 'none' ? b : 'high';
};

const deltaEpsilon = (kind: MetricKind): number =>
    kind === 'fraction' ? 0.005 : kind === 'score' ? 0.05 : 0.5;

const deltaMagnitude = (v: number, kind: MetricKind): string =>
    v.toFixed(kind === 'fraction' ? 3 : kind === 'score' ? 1 : 0);

const spreadTint = (dir: number): string =>
    dir > 0 ? 'var(--spread-tint-pos)' : dir < 0 ? 'var(--spread-tint-neg)' : 'var(--spread-tint-flat)';

const isNumeric = (m: BaseEvaluationMetric): m is NumericMetric =>
    m.$type === 'numeric' && typeof (m as NumericMetric).value === 'number';

const aggregate = (values: number[]): BandPoint | undefined => {
    if (values.length === 0) return undefined;
    const sorted = [...values].sort((a, b) => a - b);
    const mean = sorted.reduce((s, v) => s + v, 0) / sorted.length;
    const mid = Math.floor(sorted.length / 2);
    const median = sorted.length % 2 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
    return { mean, median, lo: sorted[0], hi: sorted[sorted.length - 1], n: sorted.length };
};

const useLocalStyles = makeStyles({
    // Hides the horizontal scrollbar on the metric segmented control.
    segTrack: {
        scrollbarWidth: 'none',
        '&::-webkit-scrollbar': { height: 0, width: 0 },
    },
    // Hover affordance for a non-active segment button (active buttons don't get it).
    segBtn: {
        ':hover': {
            WebkitBackdropFilter: 'var(--eval-nav-bd-hover)',
            backdropFilter: 'var(--eval-nav-bd-hover)',
            color: 'var(--neutral-foreground-1)',
        },
    },
    // Reserves the width of the bold (active) label via a hidden ::after clone so the
    // control doesn't reflow when a metric becomes active.
    segLabel: {
        display: 'inline-flex',
        flexDirection: 'column',
        '&::after': {
            content: 'attr(data-text)',
            fontWeight: 'var(--font-weight-semibold)',
            height: 0,
            overflow: 'hidden',
            visibility: 'hidden',
        },
    },
});

export const HistoryView = () => {
    const s = useReportStyles();
    const local = useLocalStyles();
    const { scoreSummary, dataset, selectedScenarioLevel } = useReportContext();

    const leafScenarios = useMemo(() => {
        const primaryRoot = [...scoreSummary.executionHistory.values()][0];
        if (!primaryRoot) return [] as { scenario: ScenarioRunResult; nodeKey: string }[];
        return primaryRoot.flattenedNodes
            .filter((node) => node.isLeafNode && node.scenario != null)
            .map((node) => ({ scenario: node.scenario!, nodeKey: node.nodeKey }));
    }, [scoreSummary]);

    // History follows the sidebar selection (no in-panel picker). Resolve the selected
    // node key — possibly a group — to the first leaf scenario under it.
    const selectedScenario = useMemo(() => {
        if (!selectedScenarioLevel) return leafScenarios[0]?.scenario;
        const match = leafScenarios.find(
            (l) => l.nodeKey === selectedScenarioLevel || l.nodeKey.startsWith(`${selectedScenarioLevel}.`),
        );
        return (match ?? leafScenarios[0])?.scenario;
    }, [leafScenarios, selectedScenarioLevel]);

    const allSeries = useMemo(
        () => (selectedScenario ? metricHistoryForScenario(scoreSummary, selectedScenario) : []),
        [scoreSummary, selectedScenario],
    );

    const metricNames = useMemo(() => allSeries.map((series) => series.metricName), [allSeries]);

    const [selectedMetric, setSelectedMetric] = useState<string | undefined>(undefined);
    const activeMetric =
        selectedMetric && metricNames.includes(selectedMetric) ? selectedMetric : metricNames[0];

    // Slide the pill indicator to track the active metric button.
    const trackRef = useRef<HTMLDivElement | null>(null);
    const indRef = useRef<HTMLSpanElement | null>(null);
    const placedRef = useRef(false);

    useLayoutEffect(() => {
        const track = trackRef.current;
        const ind = indRef.current;
        if (!track || !ind) return;
        let raf = 0;
        const place = () => {
            const btn = track.querySelector<HTMLElement>('[aria-selected="true"]');
            if (!btn) { ind.style.opacity = '0'; return; }
            const reduce =
                typeof window.matchMedia === 'function' &&
                window.matchMedia('(prefers-reduced-motion: reduce)').matches;
            const first = !placedRef.current;
            const prev = ind.style.transition;
            if (first || reduce) ind.style.transition = 'none';
            ind.style.opacity = '1';
            const left = btn.offsetLeft;
            const top = btn.offsetTop;
            const w = btn.offsetWidth;
            const h = btn.offsetHeight;
            ind.style.transform = `translate(${left}px, ${top}px)`;
            ind.style.width = `${w}px`;
            ind.style.height = `${h}px`;
            if (first && !reduce) {
                void ind.offsetWidth;
                requestAnimationFrame(() => { ind.style.transition = prev; });
            }
            placedRef.current = true;
        };
        place();
        // ResizeObserver is a browser-only enhancement; guard it so non-DOM envs (tests/SSR) no-op gracefully.
        let ro: ResizeObserver | undefined;
        if (typeof ResizeObserver !== 'undefined') {
            ro = new ResizeObserver(() => {
                cancelAnimationFrame(raf);
                raf = requestAnimationFrame(place);
            });
            ro.observe(track);
        }
        window.addEventListener('resize', place);
        return () => {
            cancelAnimationFrame(raf);
            ro?.disconnect();
            window.removeEventListener('resize', place);
        };
    }, [activeMetric, metricNames.length]);

    const hasTrend = allSeries.length > 0;

    // Executions for this scenario ordered oldest → newest (see chronologicalExecutions).
    const activeSeriesPoints = useMemo(() => {
        const points = activeMetric
            ? allSeries.find((series) => series.metricName === activeMetric)?.points ?? []
            : [];
        if (points.length === 0) return points;
        const order = chronologicalExecutions(dataset);
        const rank = new Map(order.map((name, i) => [name, i]));
        return points
            .map((pt, i) => ({ pt, i }))
            .sort((a, b) => {
                const ra = rank.get(a.pt.executionName) ?? Number.MAX_SAFE_INTEGER;
                const rb = rank.get(b.pt.executionName) ?? Number.MAX_SAFE_INTEGER;
                return ra !== rb ? ra - rb : a.i - b.i;
            })
            .map((o) => o.pt);
    }, [allSeries, activeMetric, dataset]);

    // Per-execution mean/median/min/max across ALL cases of the scenario (by scenarioName,
    // not per-iteration) — that's what makes the min–max spread band appear.
    const band = useMemo(() => {
        if (!hasTrend || !activeMetric || !selectedScenario || activeSeriesPoints.length === 0) {
            return { points: [] as (BandPoint | undefined)[], kind: 'score' as MetricKind, better: 'high' as 'high' | 'low' | 'none' };
        }
        const byExec = new Map<string, number[]>();
        let kind: MetricKind = 'score';
        let better: 'high' | 'low' | 'none' = 'high';
        let kindResolved = false;
        for (const r of dataset.scenarioRunResults ?? []) {
            if (r.scenarioName !== selectedScenario.scenarioName) continue;
            const m = r.evaluationResult?.metrics?.[activeMetric];
            if (!m || !isNumeric(m)) continue;
            const v = m.value!;
            if (!kindResolved) {
                kind = metricKindOf(m, v);
                better = metricBetterOf(m);
                kindResolved = true;
            }
            const arr = byExec.get(r.executionName);
            if (arr) arr.push(v);
            else byExec.set(r.executionName, [v]);
        }
        const points = activeSeriesPoints.map((pt) => aggregate(byExec.get(pt.executionName) ?? []));
        return { points, kind, better };
    }, [dataset, activeMetric, selectedScenario, activeSeriesPoints, hasTrend]);

    if (leafScenarios.length === 0) {
        return (
            <div style={emptyStateStyle}>
                <span style={emptyTitleStyle}>No scenario data</span>
                <span style={{ fontSize: 'var(--font-size-300)', color: 'var(--neutral-foreground-3)' }}>
                    No scenarios are available in this report.
                </span>
            </div>
        );
    }

    if (!hasTrend) {
        return (
            <div style={emptyStateStyle}>
                <span style={emptyTitleStyle}>Needs at least 2 executions</span>
                <span style={{ fontSize: 'var(--font-size-300)', color: 'var(--neutral-foreground-3)' }}>
                    Run this scenario across multiple executions to see metric trends over time.
                </span>
            </div>
        );
    }

    const { points: bandPoints, kind, better } = band;
    const valid = bandPoints.filter((p): p is BandPoint => !!p);
    const first = valid[0];
    const last = valid[valid.length - 1];
    const good = better === 'none' ? null : better !== 'low';
    const eps = deltaEpsilon(kind);

    const dMean = first && last ? last.mean - first.mean : 0;
    const netFlat = Math.abs(dMean) < eps;
    const netColor = good === null || netFlat ? 'var(--neutral-foreground-4)' : (dMean > 0) === good ? STATUS_TEXT.success : STATUS_TEXT.danger;
    const netStr = netFlat ? 'stable' : (dMean > 0 ? '▲ ' : '▼ ') + deltaMagnitude(Math.abs(dMean), kind);
    const peak = valid.length ? Math.max(...valid.map((p) => p.mean)) : 0;

    const metricKindLabel = kind === 'fraction' ? 'fraction · 0–1' : kind === 'score' ? 'score · 1–5' : 'count';

    const stats: { label: string; value: string; color: string }[] = [
        { label: 'First run score', value: first ? formatRaw(first.mean, kind) : '—', color: 'var(--neutral-foreground-1)' },
        { label: 'Last run score', value: last ? formatRaw(last.mean, kind) : '—', color: 'var(--neutral-foreground-1)' },
        { label: 'Net change', value: netStr, color: netColor },
        { label: 'Peak', value: formatRaw(peak, kind), color: 'var(--neutral-foreground-1)' },
    ];

    // Shared metric domain so every run's dumbbell normalizes on the same axis.
    const [hMin, hMax] = metricScale(kind, peak);

    let prev: BandPoint | undefined;
    const runs = bandPoints.map((p, i) => {
        const date = activeSeriesPoints[i]?.executionName ?? `R${i + 1}`;
        const scoreStr = p ? formatRaw(p.mean, kind) : '—';
        let changeStr = '—';
        let dir = 0;
        let prevPos: number | null = null;
        const curPos = p ? posOn(p.mean, hMin, hMax) : 50;
        if (p && prev) {
            prevPos = posOn(prev.mean, hMin, hMax);
            const d = p.mean - prev.mean;
            const flat = Math.abs(d) < eps;
            dir = good === null || flat ? 0 : (d > 0) === good ? 1 : -1;
            changeStr = flat ? '—' : (d > 0 ? '▲ ' : '▼ ') + deltaMagnitude(Math.abs(d), kind);
        }
        if (p) prev = p;
        const db = dumbbellStyles(prevPos, curPos, dir, changeStr !== '—');
        const spL = p ? posOn(p.lo, hMin, hMax) : 50;
        const spR = p ? posOn(p.hi, hMin, hMax) : 50;
        const spread: React.CSSProperties =
            p && spR - spL > 0.5
                ? { position: 'absolute', top: '50%', left: `${spL}%`, width: `${spR - spL}%`, height: '3px', transform: 'translateY(-50%)', borderRadius: 'var(--radius-circular)', background: spreadTint(dir), pointerEvents: 'none' }
                : { display: 'none' };
        return { key: `${date}-${i}`, date, scoreStr, changeStr, numColor: STATUS_TEXT[db.sk], spread, connector: db.connector, dotB: db.dotB, dotA: db.dotA };
    });

    return (
        <div style={{ display: 'flex', flexDirection: 'column' }}>
            {metricNames.length > 0 && (
                <div role="tablist" ref={trackRef} className={local.segTrack} style={segTrackStyle}>
                    <span ref={indRef} className={s.slideIndicatorPill} aria-hidden="true" />
                    {metricNames.map((name) => {
                        const isActive = name === activeMetric;
                        return (
                            <button
                                key={name}
                                role="tab"
                                aria-selected={isActive}
                                className={mergeClasses(s.segmentedPill, !isActive && local.segBtn, isActive && s.segmentedPillActive)}
                                style={{ position: 'relative', zIndex: 1 }}
                                onClick={() => setSelectedMetric(name)}
                            >
                                <span className={local.segLabel} data-text={name}>{name}</span>
                            </button>
                        );
                    })}
                </div>
            )}

            {activeMetric && (
                <Card appearance="outline">
                    <div style={{ padding: 'var(--spacing-xs) var(--spacing-s)' }}>
                        <div style={{ display: 'flex', alignItems: 'baseline', gap: 'var(--spacing-m-nudge)', marginBottom: 'var(--spacing-xl)' }}>
                            <h3 style={{ margin: 0, fontSize: 'var(--font-size-500)', fontWeight: 'var(--font-weight-semibold)', color: 'var(--neutral-foreground-1)' }}>
                                {activeMetric}
                            </h3>
                            <span style={{ fontSize: 'var(--font-size-100)', color: 'var(--neutral-foreground-4)', textTransform: 'uppercase', letterSpacing: '.3px' }}>
                                {metricKindLabel}
                            </span>
                        </div>

                        <div className="eval-hist-stats" style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 'var(--spacing-l)', marginBottom: 'var(--spacing-xxl)' }}>
                            {stats.map((st) => (
                                <div key={st.label} style={statCardStyle}>
                                    <div style={{ fontSize: 'var(--font-size-200)', color: 'var(--neutral-foreground-3)', fontWeight: 'var(--font-weight-semibold)' }}>
                                        {st.label}
                                    </div>
                                    <div style={{ fontSize: 'var(--font-size-600)', fontWeight: 'var(--font-weight-semibold)', lineHeight: 1, color: st.color, fontVariantNumeric: 'tabular-nums' }}>
                                        {st.value}
                                    </div>
                                </div>
                            ))}
                        </div>

                        <div style={{ marginBottom: 'var(--spacing-s-nudge)' }}>
                            <TrendChart
                                points={bandPoints}
                                kind={kind}
                                ariaLabel={`${activeMetric} trend across executions${selectedScenario ? ` for ${selectedScenario.scenarioName}` : ''}`}
                                showLegend
                            />
                        </div>

                        <div style={{ marginTop: 'var(--spacing-xxl)' }}>
                            <h3 style={{ fontSize: 'var(--font-size-400)', fontWeight: 'var(--font-weight-semibold)', color: 'var(--neutral-foreground-1)', margin: '0 0 var(--spacing-m)' }}>
                                Run history
                            </h3>
                            <div className={s.tscroll}>
                                <div className="eval-grid4" style={{ display: 'grid', gridTemplateColumns: '1.6fr 1.4fr 2fr', columnGap: 'var(--spacing-l)', padding: 'var(--spacing-m-nudge) 0', fontSize: 'var(--font-size-100)', fontWeight: 'var(--font-weight-semibold)', color: 'var(--neutral-foreground-4)', textTransform: 'uppercase', letterSpacing: '.5px', borderBottom: '1px solid var(--neutral-stroke-2)' }}>
                                    <span>Execution</span>
                                    <span style={{ textAlign: 'center' }}>Metric score</span>
                                    <span style={{ textAlign: 'right' }}>Δ run</span>
                                </div>
                                {runs.map((rn) => (
                                    <div key={rn.key} className="eval-grid4" style={{ display: 'grid', gridTemplateColumns: '1.6fr 1.4fr 2fr', columnGap: 'var(--spacing-l)', alignItems: 'center', padding: 'var(--spacing-m) 0', borderBottom: '1px solid var(--neutral-stroke-3)', fontSize: 'var(--font-size-300)' }}>
                                        <span style={{ color: 'var(--neutral-foreground-1)' }}>{rn.date}</span>
                                        <span style={{ color: 'var(--neutral-foreground-1)', textAlign: 'center', fontVariantNumeric: 'tabular-nums' }}>{rn.scoreStr}</span>
                                        <span style={{ display: 'flex', alignItems: 'center', gap: 'var(--spacing-l)' }}>
                                            <span style={{ position: 'relative', flex: 1, minWidth: '110px', height: '16px' }}>
                                                <span style={{ position: 'absolute', left: 0, right: 0, top: '50%', height: '1.5px', transform: 'translateY(-50%)', borderRadius: 'var(--radius-circular)', background: 'var(--neutral-stroke-2)' }} />
                                                <span style={rn.spread} />
                                                <span style={rn.connector} />
                                                <span style={rn.dotB} />
                                                <span style={rn.dotA} />
                                            </span>
                                            <span style={{ flex: 'none', minWidth: '52px', textAlign: 'right', fontSize: 'var(--font-size-300)', fontVariantNumeric: 'tabular-nums', fontWeight: 'var(--font-weight-semibold)', color: rn.numColor }}>
                                                {rn.changeStr}
                                            </span>
                                        </span>
                                    </div>
                                ))}
                            </div>
                        </div>
                    </div>
                </Card>
            )}
        </div>
    );
};

const emptyStateStyle: React.CSSProperties = {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: 'var(--spacing-xxl) var(--spacing-xl)',
    gap: 'var(--spacing-m)',
    color: 'var(--neutral-foreground-3)',
    textAlign: 'center',
    border: '1px dashed var(--neutral-stroke-2)',
    borderRadius: 'var(--radius-large)',
};

const emptyTitleStyle: React.CSSProperties = {
    fontWeight: 'var(--font-weight-semibold)',
    fontSize: 'var(--font-size-400)',
    color: 'var(--neutral-foreground-2)',
};

const segTrackStyle: React.CSSProperties = {
    display: 'flex',
    width: '100%',
    boxSizing: 'border-box',
    justifyContent: 'safe center',
    gap: 'var(--spacing-xs)',
    padding: 'var(--spacing-xs)',
    overflowX: 'auto',
    backgroundImage: 'var(--acrylic-fill-light)',
    backdropFilter: 'var(--acrylic-blur)',
    WebkitBackdropFilter: 'var(--acrylic-blur)',
    border: '1px solid var(--neutral-stroke-1)',
    borderRadius: 'var(--radius-large)',
    marginBottom: 'var(--spacing-xl)',
    position: 'relative',
};

const statCardStyle: React.CSSProperties = {
    background: 'var(--acrylic-fill-light)',
    backdropFilter: 'var(--acrylic-blur)',
    WebkitBackdropFilter: 'var(--acrylic-blur)',
    border: '1px solid var(--neutral-stroke-1)',
    borderRadius: 'var(--radius-large)',
    padding: 'var(--spacing-l)',
    display: 'flex',
    flexDirection: 'column',
    gap: 'var(--spacing-s)',
};
