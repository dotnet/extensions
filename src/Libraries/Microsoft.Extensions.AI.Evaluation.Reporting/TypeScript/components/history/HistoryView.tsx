// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { useId, useLayoutEffect, useMemo, useRef, useState } from 'react';
import { Card, makeStyles, mergeClasses, useArrowNavigationGroup } from '@fluentui/react-components';
import { useReportContext } from '../core/ReportContext';
import { useReportStyles, srOnlyStyle } from '../styles/reportStyles';
import { metricHistoryForScenario, chronologicalExecutions } from '../core/viewModels';
import { TrendChart, type BandPoint, type MetricKind } from './TrendChart';
import { metricScale, posOn, STATUS_TEXT, dumbbellStyles } from './dumbbellGeometry';
import { metricKind, betterDirectionOf, formatScore } from '../core/metricModel';

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
    root: {
        display: 'flex',
        flexDirection: 'column',
    },
    emptyState: {
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
    },
    emptyTitle: {
        fontWeight: 'var(--font-weight-semibold)',
        fontSize: 'var(--font-size-400)',
        color: 'var(--neutral-foreground-2)',
    },
    emptySubtitle: {
        fontSize: 'var(--font-size-300)',
        color: 'var(--neutral-foreground-3)',
    },
    segTrack: {
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
        scrollbarWidth: 'none',
        '&::-webkit-scrollbar': { height: 0, width: 0 },
    },
    segBtn: {
        ':hover': {
            WebkitBackdropFilter: 'var(--eval-nav-bd-hover)',
            backdropFilter: 'var(--eval-nav-bd-hover)',
            color: 'var(--neutral-foreground-1)',
        },
    },
    pillPositioned: {
        position: 'relative',
        zIndex: 1,
    },
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
    cardPadding: {
        padding: 'var(--spacing-xs) var(--spacing-s)',
    },
    metricHeaderRow: {
        display: 'flex',
        alignItems: 'baseline',
        gap: 'var(--spacing-m-nudge)',
        marginBottom: 'var(--spacing-xl)',
    },
    metricTitle: {
        margin: 0,
        fontSize: 'var(--font-size-500)',
        fontWeight: 'var(--font-weight-semibold)',
        color: 'var(--neutral-foreground-1)',
    },
    metricKindLabel: {
        fontSize: 'var(--font-size-100)',
        color: 'var(--neutral-foreground-4)',
        textTransform: 'uppercase',
        letterSpacing: '.3px',
    },
    statsGrid: {
        display: 'grid',
        gridTemplateColumns: 'repeat(4, 1fr)',
        gap: 'var(--spacing-l)',
        marginBottom: 'var(--spacing-xxl)',
    },
    statCard: {
        background: 'var(--acrylic-fill-light)',
        backdropFilter: 'var(--acrylic-blur)',
        WebkitBackdropFilter: 'var(--acrylic-blur)',
        border: '1px solid var(--neutral-stroke-1)',
        borderRadius: 'var(--radius-large)',
        padding: 'var(--spacing-l)',
        display: 'flex',
        flexDirection: 'column',
        gap: 'var(--spacing-s)',
    },
    statLabel: {
        fontSize: 'var(--font-size-200)',
        color: 'var(--neutral-foreground-3)',
        fontWeight: 'var(--font-weight-semibold)',
    },
    statValueBase: {
        fontSize: 'var(--font-size-600)',
        fontWeight: 'var(--font-weight-semibold)',
        lineHeight: 1,
        fontVariantNumeric: 'tabular-nums',
    },
    chartWrap: {
        marginBottom: 'var(--spacing-s-nudge)',
    },
    historySection: {
        marginTop: 'var(--spacing-xxl)',
    },
    historyHeading: {
        fontSize: 'var(--font-size-400)',
        fontWeight: 'var(--font-weight-semibold)',
        color: 'var(--neutral-foreground-1)',
        margin: '0 0 var(--spacing-m)',
    },
    historyHeaderRow: {
        display: 'grid',
        gridTemplateColumns: '1.6fr 1.4fr 2fr',
        columnGap: 'var(--spacing-l)',
        padding: 'var(--spacing-m-nudge) 0',
        fontSize: 'var(--font-size-100)',
        fontWeight: 'var(--font-weight-semibold)',
        color: 'var(--neutral-foreground-4)',
        textTransform: 'uppercase',
        letterSpacing: '.5px',
        borderBottom: '1px solid var(--neutral-stroke-2)',
    },
    colCenter: {
        textAlign: 'center',
    },
    colRight: {
        textAlign: 'right',
    },
    historyRow: {
        display: 'grid',
        gridTemplateColumns: '1.6fr 1.4fr 2fr',
        columnGap: 'var(--spacing-l)',
        alignItems: 'center',
        padding: 'var(--spacing-m) 0',
        borderBottom: '1px solid var(--neutral-stroke-3)',
        fontSize: 'var(--font-size-300)',
    },
    cellDate: {
        color: 'var(--neutral-foreground-1)',
    },
    cellScore: {
        color: 'var(--neutral-foreground-1)',
        textAlign: 'center',
        fontVariantNumeric: 'tabular-nums',
    },
    cellDeltaWrap: {
        display: 'flex',
        alignItems: 'center',
        gap: 'var(--spacing-l)',
    },
    trackLine: {
        position: 'absolute',
        left: 0,
        right: 0,
        top: '50%',
        height: '1.5px',
        transform: 'translateY(-50%)',
        borderRadius: 'var(--radius-circular)',
        background: 'var(--neutral-stroke-2)',
    },
    changeValueBase: {
        flex: 'none',
        minWidth: '52px',
        textAlign: 'right',
        fontSize: 'var(--font-size-300)',
        fontVariantNumeric: 'tabular-nums',
        fontWeight: 'var(--font-weight-semibold)',
    },
});

export const HistoryView = () => {
    const s = useReportStyles();
    const local = useLocalStyles();
    const idPrefix = useId();
    const { scoreSummary, dataset, selectedScenarioLevel } = useReportContext();
    const metricNav = useArrowNavigationGroup({ axis: 'horizontal', circular: true });

    const leafScenarios = useMemo(() => {
        const primaryRoot = [...scoreSummary.executionHistory.values()][0];
        if (!primaryRoot) return [] as { scenario: ScenarioRunResult; nodeKey: string }[];
        return primaryRoot.flattenedNodes
            .filter((node) => node.isLeafNode && node.scenario != null)
            .map((node) => ({ scenario: node.scenario!, nodeKey: node.nodeKey }));
    }, [scoreSummary]);

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
                // metricKind's declared-kind/fallback branches only ever yield 'score' | 'fraction' | 'count'
                // for the allowedDeclaredKinds passed here, matching this file's narrower MetricKind exactly.
                kind = metricKind(m, { allowedDeclaredKinds: ['score', 'fraction', 'count'] }) as MetricKind;
                better = betterDirectionOf(m);
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
            <div className={local.emptyState}>
                <span className={local.emptyTitle}>No scenario data</span>
                <span className={local.emptySubtitle}>
                    No scenarios are available in this report.
                </span>
            </div>
        );
    }

    if (!hasTrend) {
        return (
            <div className={local.emptyState}>
                <span className={local.emptyTitle}>Needs at least 2 executions</span>
                <span className={local.emptySubtitle}>
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
    const netDirWord = netFlat ? undefined : dMean > 0 ? 'increased' : 'decreased';
    const netStr = netFlat ? 'stable' : (dMean > 0 ? '▲ ' : '▼ ') + deltaMagnitude(Math.abs(dMean), kind);
    const peak = valid.length ? Math.max(...valid.map((p) => p.mean)) : 0;

    const metricKindLabel = kind === 'fraction' ? 'fraction · 0–1' : kind === 'score' ? 'score · 1–5' : 'count';

    const stats: { label: string; value: string; color: string; srOnlySuffix?: string }[] = [
        { label: 'First run score', value: first ? formatScore(first.mean, kind) : '—', color: 'var(--neutral-foreground-1)' },
        { label: 'Last run score', value: last ? formatScore(last.mean, kind) : '—', color: 'var(--neutral-foreground-1)' },
        {
            label: 'Net change',
            value: netStr,
            color: netColor,
            srOnlySuffix: netDirWord && `${netDirWord} ${deltaMagnitude(Math.abs(dMean), kind)}`,
        },
        { label: 'Peak', value: formatScore(peak, kind), color: 'var(--neutral-foreground-1)' },
    ];

    const [hMin, hMax] = metricScale(kind, peak);

    let prev: BandPoint | undefined;
    const runs = bandPoints.map((p, i) => {
        const date = activeSeriesPoints[i]?.executionName ?? `R${i + 1}`;
        const scoreStr = p ? formatScore(p.mean, kind) : '—';
        let changeStr = '—';
        let dir = 0;
        let dirWord: string | undefined;
        let prevPos: number | null = null;
        const curPos = p ? posOn(p.mean, hMin, hMax) : 50;
        if (p && prev) {
            prevPos = posOn(prev.mean, hMin, hMax);
            const d = p.mean - prev.mean;
            const flat = Math.abs(d) < eps;
            dir = good === null || flat ? 0 : (d > 0) === good ? 1 : -1;
            const magnitude = deltaMagnitude(Math.abs(d), kind);
            changeStr = flat ? '—' : (d > 0 ? '▲ ' : '▼ ') + magnitude;
            dirWord = flat ? undefined : `${d > 0 ? 'increased' : 'decreased'} ${magnitude}`;
        }
        if (p) prev = p;
        const db = dumbbellStyles(prevPos, curPos, dir, changeStr !== '—');
        const spL = p ? posOn(p.lo, hMin, hMax) : 50;
        const spR = p ? posOn(p.hi, hMin, hMax) : 50;
        const spread: React.CSSProperties =
            p && spR - spL > 0.5
                ? { position: 'absolute', top: '50%', left: `${spL}%`, width: `${spR - spL}%`, height: '3px', transform: 'translateY(-50%)', borderRadius: 'var(--radius-circular)', background: spreadTint(dir), pointerEvents: 'none' }
                : { display: 'none' };
        return { key: `${date}-${i}`, date, scoreStr, changeStr, dirWord, numColor: STATUS_TEXT[db.sk], spread, connector: db.connector, dotB: db.dotB, dotA: db.dotA };
    });

    return (
        <div className={local.root}>
            {metricNames.length > 0 && (
                <div role="tablist" aria-label="Metrics" ref={trackRef} className={local.segTrack} {...metricNav}>
                    <span ref={indRef} className={s.slideIndicatorPill} aria-hidden="true" />
                    {metricNames.map((name, i) => {
                        const isActive = name === activeMetric;
                        return (
                            <button
                                key={name}
                                role="tab"
                                id={`${idPrefix}metric-tab-${i}`}
                                aria-selected={isActive}
                                aria-controls={`${idPrefix}metric-tabpanel`}
                                tabIndex={0}
                                className={mergeClasses(s.segmentedPill, local.pillPositioned, !isActive && local.segBtn, isActive && s.segmentedPillActive)}
                                onClick={() => setSelectedMetric(name)}
                            >
                                <span className={local.segLabel} data-text={name}>{name}</span>
                            </button>
                        );
                    })}
                </div>
            )}

            {activeMetric && (
                <Card
                    appearance="outline"
                    role="tabpanel"
                    id={`${idPrefix}metric-tabpanel`}
                    aria-labelledby={`${idPrefix}metric-tab-${metricNames.indexOf(activeMetric)}`}
                >
                    <div className={local.cardPadding}>
                        <div className={local.metricHeaderRow}>
                            <h2 className={local.metricTitle}>
                                {activeMetric}
                            </h2>
                            <span className={local.metricKindLabel}>
                                {metricKindLabel}
                            </span>
                        </div>

                        <div className={mergeClasses('eval-hist-stats', local.statsGrid)}>
                            {stats.map((st) => (
                                <div key={st.label} className={local.statCard}>
                                    <div className={local.statLabel}>
                                        {st.label}
                                    </div>
                                    <div
                                        className={local.statValueBase}
                                        style={{ color: st.color }}
                                        aria-hidden={st.srOnlySuffix ? true : undefined}
                                    >
                                        {st.value}
                                    </div>
                                    {st.srOnlySuffix && <span style={srOnlyStyle}>{st.srOnlySuffix}</span>}
                                </div>
                            ))}
                        </div>

                        <div className={local.chartWrap}>
                            <TrendChart
                                points={bandPoints}
                                kind={kind}
                                ariaLabel={`${activeMetric} trend across executions${selectedScenario ? ` for ${selectedScenario.scenarioName}` : ''}`}
                                showLegend
                            />
                        </div>

                        <div className={local.historySection}>
                            <h2 className={local.historyHeading}>
                                Run history
                            </h2>
                            <div className={s.tscroll} role="table" aria-label="Run history" tabIndex={0}>
                                <div className={mergeClasses('eval-grid4', local.historyHeaderRow)} role="row">
                                    <span role="columnheader">Execution</span>
                                    <span role="columnheader" className={local.colCenter}>Metric score</span>
                                    <span role="columnheader" className={local.colRight}>Δ run</span>
                                </div>
                                {runs.map((rn) => (
                                    <div key={rn.key} className={mergeClasses('eval-grid4', local.historyRow)} role="row">
                                        <span role="cell" className={local.cellDate}>{rn.date}</span>
                                        <span role="cell" className={local.cellScore}>{rn.scoreStr}</span>
                                        <span role="cell" className={local.cellDeltaWrap}>
                                            <span style={{ position: 'relative', flex: 1, minWidth: '110px', height: '16px' }}>
                                                <span className={local.trackLine} />
                                                <span style={rn.spread} />
                                                <span style={rn.connector} />
                                                <span style={rn.dotB} />
                                                <span style={rn.dotA} />
                                            </span>
                                            <span
                                                className={local.changeValueBase}
                                                style={{ color: rn.numColor }}
                                                aria-hidden={rn.dirWord ? true : undefined}
                                            >
                                                {rn.changeStr}
                                            </span>
                                            {rn.dirWord && <span style={srOnlyStyle}>{rn.dirWord}</span>}
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

