// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { useMemo, useEffect, useState } from 'react';
import { makeStyles, Badge, Card, Dropdown, Option } from '@fluentui/react-components';
import { ChevronRight20Regular } from '@fluentui/react-icons';
import { useReportContext } from './ReportContext';
import { useReportStyles } from './reportStyles';
import { chronologicalExecutions } from './viewModels';
import { DUMBBELL_D, DUMBBELL_RING, DUMBBELL_CONN } from './dumbbellGeometry';

// Every color references a DS token so it flips in dark mode. SOLID = saturated
// dot/connector fill; TEXT = readable foreground for the current value + delta.
type StatusKey = 'success' | 'warning' | 'danger' | 'caution' | 'neutral';

const STATUS_SOLID: Record<StatusKey, string> = {
    success: 'var(--status-success-background-3)',
    warning: 'var(--status-warning-foreground-2)',
    danger: 'var(--status-danger-foreground-2)',
    caution: 'var(--palette-yellow-foreground1)',
    neutral: 'var(--neutral-foreground-4)',
};
const STATUS_TEXT: Record<StatusKey, string> = {
    success: 'var(--status-success-foreground-1)',
    warning: 'var(--status-warning-foreground-1)',
    danger: 'var(--status-danger-foreground-1)',
    caution: 'var(--palette-yellow-foreground1)',
    neutral: 'var(--neutral-foreground-3)',
};

const BUCKET_ORDER: StatusKey[] = ['success', 'warning', 'danger', 'neutral'];

const RATING_STATUS: Record<string, StatusKey> = {
    exceptional: 'success',
    good: 'success',
    average: 'warning',
    poor: 'danger',
    unacceptable: 'danger',
    inconclusive: 'caution',
};

const statusKey = (rating: EvaluationRating | undefined): StatusKey =>
    RATING_STATUS[rating ?? 'unknown'] ?? 'neutral';

type MetricKind = 'fraction' | 'score' | 'severity' | 'count';

const metricKind = (m: NumericMetric): MetricKind => {
    const k = m.metadata?.kind as MetricKind | undefined;
    if (k) return k;
    const v = m.value ?? 0;
    if (v >= 0 && v <= 1) return 'fraction';
    if (Number.isInteger(v) && v >= 1 && v <= 5) return 'score';
    return 'score';
};

const metricBetter = (m: NumericMetric): string => m.metadata?.better ?? 'high';

const metricScale = (kind: MetricKind, peak: number): [number, number] =>
    kind === 'fraction' ? [0, 1] : kind === 'score' ? [1, 5] : [0, Math.max(1, peak || 1)];

const posOn = (v: number, min: number, max: number): number =>
    max > min ? Math.max(0, Math.min(100, ((v - min) / (max - min)) * 100)) : 50;

const formatRaw = (v: number, kind: MetricKind): string => {
    if (kind === 'fraction') return v.toFixed(3);
    return (v % 1 === 0 ? '' + v : v.toFixed(1)) + '/5';
};

const HALO = 'box-shadow:0 0 0 2px var(--neutral-background-1);';

type Dumbbell = { connectorStyle: string; dotBStyle: string; dotAStyle: string };

const dumbbellStyles = (
    prevPos: number | null,
    currPos: number,
    dir: number,
    hasDelta: boolean,
): Dumbbell => {
    const sk: StatusKey = dir > 0 ? 'success' : dir < 0 ? 'danger' : 'neutral';
    const color = STATUS_SOLID[sk];
    const hasPrev = prevPos !== null && prevPos !== undefined && isFinite(prevPos);
    const cur = Math.max(0, Math.min(100, currPos));
    const prv = hasPrev ? Math.max(0, Math.min(100, prevPos as number)) : cur;
    const lo = Math.min(prv, cur);
    const hi = Math.max(prv, cur);
    const connVisible = hasPrev && hasDelta && hi - lo > 0;
    return {
        connectorStyle: connVisible
            ? `position:absolute; top:50%; left:${lo}%; width:${hi - lo}%; height:${DUMBBELL_CONN}px; transform:translateY(-50%); border-radius:var(--radius-circular); background:${color};`
            : 'display:none;',
        dotBStyle: hasPrev
            ? `position:absolute; top:50%; left:${prv}%; width:${DUMBBELL_D}px; height:${DUMBBELL_D}px; box-sizing:border-box; transform:translate(-50%,-50%); border-radius:50%; background:var(--neutral-background-1); border:${DUMBBELL_RING}px solid var(--neutral-foreground-3); ${HALO}`
            : 'display:none;',
        dotAStyle: `position:absolute; top:50%; left:${cur}%; width:${DUMBBELL_D}px; height:${DUMBBELL_D}px; box-sizing:border-box; transform:translate(-50%,-50%); border-radius:50%; background:${color}; ${HALO}`,
    };
};

// Per-scenario mean of each metric across its cases; scenario is the only grouping
// key (no metric-family field in the data).
type MetricAgg = { kind: MetricKind; better: string; sum: number; n: number; statusDist: Record<string, number> };
type ScenAgg = { name: string; cases: number; metricAgg: Record<string, MetricAgg>; metricOrder: string[] };

const scenarioMetrics = (results: ScenarioRunResult[]): { byScen: Record<string, ScenAgg>; order: string[] } => {
    const byScen: Record<string, ScenAgg> = {};
    const order: string[] = [];
    for (const r of results) {
        const sn = r.scenarioName;
        if (!byScen[sn]) {
            byScen[sn] = { name: sn, cases: 0, metricAgg: {}, metricOrder: [] };
            order.push(sn);
        }
        const S = byScen[sn];
        S.cases++;
        for (const [k, m] of Object.entries(r.evaluationResult?.metrics ?? {})) {
            if (!m || m.$type !== 'numeric') continue;
            const nm = m as NumericMetric;
            if (typeof nm.value !== 'number') continue;
            if (!S.metricAgg[k]) {
                S.metricAgg[k] = { kind: metricKind(nm), better: metricBetter(nm), sum: 0, n: 0, statusDist: {} };
                S.metricOrder.push(k);
            }
            const a = S.metricAgg[k];
            a.sum += nm.value;
            a.n++;
            const sk = statusKey(nm.interpretation?.rating);
            a.statusDist[sk] = (a.statusDist[sk] ?? 0) + 1;
        }
    }
    return { byScen, order };
};

type CmpDelta = { color: 'success' | 'danger' | 'subtle'; txt: string };

const deltaStyle = (d: number, unit: 'frac' | '', good: boolean | null): CmpDelta => {
    const flat = Math.abs(d) < 0.0005;
    const up = d > 0;
    const sk: StatusKey = good === null || flat ? 'neutral' : up === good ? 'success' : 'danger';
    const color: CmpDelta['color'] = sk === 'success' ? 'success' : sk === 'danger' ? 'danger' : 'subtle';
    const txt = flat
        ? '—'
        : (up ? '▲ ' : '▼ ') + Math.abs(d).toFixed(unit === 'frac' ? 3 : 2);
    return { color, txt };
};

type CmpRow = {
    name: string;
    a: string;
    b: string;
    bColor: string;
    delta: string;
    deltaColor: 'success' | 'danger' | 'subtle';
    connectorStyle: string;
    dotBStyle: string;
    dotAStyle: string;
    av: number;
    bv: number;
    d: number;
    dir: number;
    mag: number;
};

const buildCmpRow = (k: string, ba: MetricAgg | undefined, bb: MetricAgg | undefined): CmpRow => {
    const kind = (bb ?? ba)!.kind;
    const better = (bb ?? ba)!.better;
    const hasA = !!(ba && ba.n);
    const hasB = !!(bb && bb.n);
    const av = hasA ? ba!.sum / ba!.n : 0;
    const bv = hasB ? bb!.sum / bb!.n : 0;
    const d = bv - av;
    const good = better === 'none' ? null : better !== 'low';
    const unit: 'frac' | '' = kind === 'fraction' ? 'frac' : '';
    const x = deltaStyle(d, unit, good);
    const [dmin, dmax] = metricScale(kind, Math.max(av, bv));
    const isFlat = x.txt === '—';
    const dir = good === null || isFlat || !hasA ? 0 : (d > 0) === good ? 1 : -1;
    const prevPos = hasA ? posOn(av, dmin, dmax) : null;
    const curPos = posOn(bv, dmin, dmax);
    const db = dumbbellStyles(prevPos, curPos, dir, !isFlat);
    let domB: StatusKey = 'neutral';
    let best = -1;
    if (bb) {
        for (const sk of BUCKET_ORDER) {
            if ((bb.statusDist[sk] ?? 0) > best) {
                best = bb.statusDist[sk] ?? 0;
                domB = sk;
            }
        }
    }
    const aStr = hasA ? formatRaw(av, kind) : '—';
    const bStr = hasB ? formatRaw(bv, kind) : '—';
    return {
        name: k,
        a: aStr,
        b: bStr,
        bColor: STATUS_TEXT[domB],
        delta: x.txt,
        deltaColor: x.color,
        connectorStyle: db.connectorStyle,
        dotBStyle: db.dotBStyle,
        dotAStyle: db.dotAStyle,
        av,
        bv,
        d,
        dir,
        mag: Math.abs(d) / (dmax - dmin || 1),
    };
};

type SortKey = 'name' | 'a' | 'b' | 'change';

const useLocalStyles = makeStyles({
    sortBtn: {
        display: 'inline-flex',
        alignItems: 'center',
        gap: 'var(--spacing-xxs)',
        width: '100%',
        font: 'inherit',
        color: 'inherit',
        background: 'none',
        border: 'none',
        padding: 0,
        cursor: 'pointer',
        letterSpacing: 'inherit',
        textTransform: 'inherit',
        transition: 'color var(--duration-faster) var(--curve-easy-ease)',
        '&:hover': { color: 'var(--neutral-foreground-2)' },
    },
    emptyCard: {
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        textAlign: 'center',
        gap: 'var(--spacing-s)',
        padding: 'var(--spacing-xxxl) var(--spacing-xl)',
    },
    emptyTitle: {
        fontSize: 'var(--font-size-400)',
        fontWeight: 'var(--font-weight-semibold)',
        color: 'var(--neutral-foreground-1)',
    },
    emptyReason: {
        fontSize: 'var(--font-size-300)',
        color: 'var(--neutral-foreground-3)',
        maxWidth: '400px',
        lineHeight: 1.5,
    },
});

const CMP_COLS = '1.6fr 1.4fr 2fr';

export const ComparisonView = () => {
    const local = useLocalStyles();
    const s = useReportStyles();
    const { dataset, scoreSummary, cmpA, setCmpA, cmpB, setCmpB, selectedScenarioLevel, scopedNode } = useReportContext();

    const [sortKey, setSortKey] = useState<SortKey>('name');
    const [sortDir, setSortDir] = useState<'asc' | 'desc'>('asc');

    const executions = useMemo(
        () => [...scoreSummary.executionHistory.keys()],
        [scoreSummary],
    );

    // Default by creationTime (oldest → newest): Current (B) = latest run,
    // Baseline (A) = its predecessor. Users can still override via the dropdowns.
    const chrono = useMemo(() => chronologicalExecutions(dataset), [dataset]);
    const defaultB = chrono.length >= 1 ? chrono[chrono.length - 1] : undefined;
    const defaultA = chrono.length >= 2 ? chrono[chrono.length - 2] : undefined;

    useEffect(() => {
        if (!cmpA && defaultA) setCmpA(defaultA);
        if (!cmpB && defaultB) setCmpB(defaultB);
    }, [defaultA, defaultB, cmpA, cmpB, setCmpA, setCmpB]);

    const effectiveA = cmpA ?? defaultA;
    const effectiveB = cmpB ?? defaultB;

    const hasTwoExecs = executions.length >= 2;

    // The sidebar selection is a nodeKey, not a scenarioName; resolve it to the leaf
    // scenarioNames under the scoped node. undefined = show all.
    const scopedScenarioNames = useMemo(() => {
        if (!selectedScenarioLevel) return undefined;
        return new Set(
            scopedNode.flattenedNodes
                .filter((n) => n.isLeafNode && n.scenario)
                .map((n) => n.scenario!.scenarioName),
        );
    }, [scopedNode, selectedScenarioLevel]);

    const resultsFor = (execName: string | undefined): ScenarioRunResult[] => {
        if (!execName) return [];
        return (dataset.scenarioRunResults ?? []).filter(
            (r) =>
                r.executionName === execName &&
                (!scopedScenarioNames || scopedScenarioNames.has(r.scenarioName)),
        );
    };

    const groups = useMemo(() => {
        if (!hasTwoExecs || !effectiveA || !effectiveB || effectiveA === effectiveB) return [];

        const scnA = scenarioMetrics(resultsFor(effectiveA));
        const scnB = scenarioMetrics(resultsFor(effectiveB));
        const scenNames = [...new Set([...scnB.order, ...scnA.order])];

        const sortRows = (rows: CmpRow[]): CmpRow[] => {
            const sdir = sortDir === 'asc' ? 1 : -1;
            return rows.slice().sort((p, q) => {
                let r = 0;
                if (sortKey === 'name') r = p.name.localeCompare(q.name);
                else if (sortKey === 'a') r = p.av - q.av;
                else if (sortKey === 'b') r = p.bv - q.bv;
                else if (sortKey === 'change') r = p.d - q.d;
                return r * sdir;
            });
        };

        return scenNames
            .map((sn) => {
                const A = scnA.byScen[sn];
                const B = scnB.byScen[sn];
                const order = B && B.metricOrder.length ? B.metricOrder : A ? A.metricOrder : [];
                const rows = sortRows(order.map((k) => buildCmpRow(k, A?.metricAgg[k], B?.metricAgg[k])));
                return { scenario: sn, rows, cases: (B ?? A ?? { cases: 0 }).cases };
            })
            .filter((g) => g.rows.length > 0);
    }, [dataset, effectiveA, effectiveB, hasTwoExecs, sortKey, sortDir, selectedScenarioLevel, scopedScenarioNames]);

    const allRows = useMemo(() => groups.reduce<CmpRow[]>((acc, g) => acc.concat(g.rows), []), [groups]);
    const multiScenario = groups.length > 1;

    const improved = allRows.filter((r) => r.dir > 0).length;
    const regressed = allRows.filter((r) => r.dir < 0).length;
    const biggest = useMemo(() => {
        let b: CmpRow | null = null;
        let bestMag = -1;
        for (const r of allRows) {
            if (r.dir !== 0 && r.mag > bestMag) {
                bestMag = r.mag;
                b = r;
            }
        }
        return b;
    }, [allRows]);

    const DIV = '1px solid var(--neutral-stroke-2)';
    const headline = [
        {
            label: 'Metrics improved',
            value: '' + improved,
            valueColor: improved ? STATUS_TEXT.success : 'var(--neutral-foreground-1)',
            sub: 'of ' + allRows.length + ' metrics',
            borderRight: DIV,
        },
        {
            label: 'Metrics regressed',
            value: '' + regressed,
            valueColor: regressed ? STATUS_TEXT.danger : 'var(--neutral-foreground-1)',
            sub: 'of ' + allRows.length + ' metrics',
            borderRight: DIV,
        },
        biggest
            ? {
                  label: 'Biggest mover',
                  value: biggest.delta,
                  valueColor: biggest.dir > 0 ? STATUS_TEXT.success : STATUS_TEXT.danger,
                  sub: biggest.name,
                  borderRight: 'none',
              }
            : {
                  label: 'Biggest mover',
                  value: 'stable',
                  valueColor: 'var(--neutral-foreground-1)',
                  sub: 'no significant change',
                  borderRight: 'none',
              },
    ];

    const onSort = (key: SortKey) => {
        if (sortKey === key) {
            setSortDir((d) => (d === 'asc' ? 'desc' : 'asc'));
        } else {
            setSortKey(key);
            setSortDir('asc');
        }
    };
    const sortArrow = (key: SortKey) => (sortKey === key ? (sortDir === 'asc' ? ' ▲' : ' ▼') : '');
    const ariaSort = (key: SortKey): 'ascending' | 'descending' | 'none' =>
        sortKey === key ? (sortDir === 'asc' ? 'ascending' : 'descending') : 'none';
    const ariaLabel = (key: SortKey, label: string) =>
        sortKey === key
            ? `${label}, sorted ${sortDir === 'asc' ? 'ascending' : 'descending'}. Activate to sort ${sortDir === 'asc' ? 'descending' : 'ascending'}.`
            : `Sort by ${label}`;

    if (!hasTwoExecs) {
        return (
            <div className={s.card}>
                <div className={local.emptyCard}>
                    <span className={local.emptyTitle}>Needs at least 2 executions</span>
                    <span className={local.emptyReason}>
                        Run the evaluation suite across multiple executions to compare results side by side.
                    </span>
                </div>
            </div>
        );
    }

    return (
        <div>
            <div
                style={{
                    marginBottom: 'var(--spacing-xl)',
                    backgroundImage: 'var(--acrylic-fill-light)',
                    backdropFilter: 'var(--acrylic-blur)',
                    WebkitBackdropFilter: 'var(--acrylic-blur)',
                    border: '1px solid var(--neutral-stroke-1)',
                    borderRadius: 'var(--radius-card)',
                    width: '100%',
                    maxWidth: '100%',
                    position: 'relative',
                    zIndex: 20,
                }}
            >
                <div
                    className="eval-cmp-selector"
                    style={{ display: 'flex', alignItems: 'stretch', gap: 0, flexWrap: 'wrap', padding: 'var(--spacing-m) var(--spacing-l)' }}
                >
                    <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--spacing-s-nudge)', minWidth: 0, flex: 1 }}>
                        <span style={{ display: 'flex', alignItems: 'center', gap: 'var(--spacing-s-nudge)', fontSize: 'var(--font-size-100)', fontWeight: 'var(--font-weight-semibold)', textTransform: 'uppercase', letterSpacing: '.5px', color: 'var(--neutral-foreground-3)' }}>
                            <span aria-hidden="true" style={{ width: '8px', height: '8px', borderRadius: '50%', background: 'var(--neutral-foreground-4)', flex: 'none' }} />
                            Baseline
                        </span>
                        <div style={{ width: '100%' }}>
                            <Dropdown
                                className="eval-cmp-drop"
                                aria-label="Baseline execution"
                                value={effectiveA ?? ''}
                                selectedOptions={effectiveA ? [effectiveA] : []}
                                onOptionSelect={(_ev, data) => setCmpA(data.optionValue)}
                            >
                                {executions.map((name) => (
                                    <Option key={name} value={name}>{name}</Option>
                                ))}
                            </Dropdown>
                        </div>
                    </div>

                    <div className="eval-cmp-arrowwrap" style={{ display: 'flex', alignItems: 'flex-end', padding: '0 var(--spacing-l)' }}>
                        <span className="eval-cmp-arrow" style={{ display: 'inline-flex', alignItems: 'center', justifyContent: 'center', width: '24px', height: '32px', color: 'var(--neutral-foreground-3)' }}>
                            <ChevronRight20Regular />
                        </span>
                    </div>

                    <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--spacing-s-nudge)', minWidth: 0, flex: 1 }}>
                        <span style={{ display: 'flex', alignItems: 'center', gap: 'var(--spacing-s-nudge)', fontSize: 'var(--font-size-100)', fontWeight: 'var(--font-weight-semibold)', textTransform: 'uppercase', letterSpacing: '.5px', color: 'var(--brand-foreground-1)' }}>
                            <span aria-hidden="true" style={{ width: '8px', height: '8px', borderRadius: '50%', background: 'var(--brand-background)', flex: 'none' }} />
                            Current
                        </span>
                        <div style={{ width: '100%' }}>
                            <Dropdown
                                className="eval-cmp-drop"
                                aria-label="Current execution"
                                value={effectiveB ?? ''}
                                selectedOptions={effectiveB ? [effectiveB] : []}
                                onOptionSelect={(_ev, data) => setCmpB(data.optionValue)}
                            >
                                {executions.map((name) => (
                                    <Option key={name} value={name}>{name}</Option>
                                ))}
                            </Dropdown>
                        </div>
                    </div>
                </div>

                {effectiveA !== effectiveB && allRows.length > 0 && (
                    <div style={{ display: 'flex', alignItems: 'stretch', borderTop: '1px solid var(--neutral-stroke-2)' }}>
                        {headline.map((c) => (
                            <div key={c.label} style={{ flex: 1, minWidth: 0, padding: 'var(--spacing-m-nudge) var(--spacing-l)', borderRight: c.borderRight }}>
                                <div style={{ fontSize: 'var(--font-size-200)', color: 'var(--neutral-foreground-3)', fontWeight: 'var(--font-weight-semibold)', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                                    {c.label}
                                </div>
                                <div style={{ fontSize: 'var(--font-size-600)', fontWeight: 'var(--font-weight-semibold)', lineHeight: 1.1, color: c.valueColor, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis', marginTop: 'var(--spacing-xs)', fontVariantNumeric: 'tabular-nums' }}>
                                    {c.value}
                                </div>
                                <div style={{ fontSize: 'var(--font-size-200)', color: 'var(--neutral-foreground-4)', marginTop: 'var(--spacing-xxs)', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                                    {c.sub}
                                </div>
                            </div>
                        ))}
                    </div>
                )}
            </div>

            {effectiveA === effectiveB && (
                <div className={s.card}>
                    <div className={local.emptyCard}>
                        <span className={local.emptyTitle}>Select two different executions</span>
                        <span className={local.emptyReason}>
                            The baseline and current executions are the same. Choose different executions to see the delta.
                        </span>
                    </div>
                </div>
            )}

            {effectiveA !== effectiveB && allRows.length > 0 && (
                <Card appearance="outline">
                    <div style={{ margin: '-12px' }}>
                        <div style={{ display: 'flex', alignItems: 'baseline', justifyContent: 'space-between', gap: 'var(--spacing-l)', padding: 'var(--spacing-l) var(--spacing-xl) var(--spacing-m)', flexWrap: 'wrap' }}>
                            <h3 style={{ margin: 0, fontSize: 'var(--font-size-400)', fontWeight: 'var(--font-weight-semibold)', color: 'var(--neutral-foreground-1)' }}>
                                Per-metric change
                            </h3>
                            <span style={{ display: 'inline-flex', alignItems: 'center', gap: 'var(--spacing-m)', fontSize: 'var(--font-size-200)', color: 'var(--neutral-foreground-3)', whiteSpace: 'nowrap', flexWrap: 'wrap' }}>
                                <span style={{ display: 'inline-flex', alignItems: 'center', gap: 'var(--spacing-xs)' }}>
                                    <span aria-hidden="true" style={{ width: '8px', height: '8px', boxSizing: 'border-box', borderRadius: '50%', background: 'var(--neutral-background-1)', border: '1.5px solid var(--neutral-foreground-3)', display: 'inline-block', flex: 'none' }} /> baseline
                                </span>
                                <span style={{ display: 'inline-flex', alignItems: 'center', gap: 'var(--spacing-xs)' }}>
                                    <span aria-hidden="true" style={{ width: '8px', height: '8px', boxSizing: 'border-box', borderRadius: '50%', background: 'var(--status-success-background-3)', display: 'inline-block', flex: 'none' }} /> current
                                </span>
                                <span style={{ width: '1px', height: 'var(--spacing-m)', background: 'var(--neutral-stroke-2)' }} />
                                <span style={{ display: 'inline-flex', alignItems: 'center', gap: 'var(--spacing-xs)' }}>
                                    <span aria-hidden="true" style={{ color: 'var(--status-success-foreground-1)' }}>▲</span> improved <span aria-hidden="true" style={{ color: 'var(--neutral-foreground-4)' }}>·</span> <span aria-hidden="true" style={{ color: 'var(--status-danger-foreground-1)' }}>▼</span> regressed
                                </span>
                            </span>
                        </div>

                        <div className="eval-tscroll">
                            <div
                                role="row"
                                className="eval-grid3"
                                style={{ display: 'grid', gridTemplateColumns: CMP_COLS, columnGap: 'var(--spacing-l)', alignItems: 'center', padding: 'var(--spacing-m-nudge) var(--spacing-xl)', fontSize: 'var(--font-size-100)', fontWeight: 'var(--font-weight-semibold)', color: 'var(--neutral-foreground-4)', textTransform: 'uppercase', letterSpacing: '.5px', borderBottom: '1px solid var(--neutral-stroke-2)' }}
                            >
                                <button type="button" className={local.sortBtn} onClick={() => onSort('name')} aria-sort={ariaSort('name')} aria-label={ariaLabel('name', 'Metric')} style={{ whiteSpace: 'nowrap' }}>
                                    Metric{sortArrow('name')}
                                </button>
                                <span style={{ display: 'grid', gridTemplateColumns: '1fr auto 1fr', alignItems: 'center', gap: 'var(--spacing-xs)', whiteSpace: 'nowrap' }}>
                                    <button type="button" className={local.sortBtn} onClick={() => onSort('a')} aria-sort={ariaSort('a')} aria-label={ariaLabel('a', 'Baseline')} style={{ justifyContent: 'flex-end', textAlign: 'right' }}>
                                        Baseline{sortArrow('a')}
                                    </button>
                                    <span aria-hidden="true" style={{ color: 'var(--neutral-foreground-4)', fontWeight: 'var(--font-weight-regular)', textAlign: 'center' }}>→</span>
                                    <button type="button" className={local.sortBtn} onClick={() => onSort('b')} aria-sort={ariaSort('b')} aria-label={ariaLabel('b', 'Current')} style={{ justifyContent: 'flex-start', textAlign: 'left' }}>
                                        Current{sortArrow('b')}
                                    </button>
                                </span>
                                <button type="button" className={local.sortBtn} onClick={() => onSort('change')} aria-sort={ariaSort('change')} aria-label={ariaLabel('change', 'Δ run')} style={{ whiteSpace: 'nowrap', justifyContent: 'flex-end' }}>
                                    Δ run{sortArrow('change')}
                                </button>
                            </div>

                            {groups.map((g) => (
                                <div key={g.scenario}>
                                    {multiScenario && (
                                        <div style={{ display: 'flex', alignItems: 'center', gap: 'var(--spacing-s)', padding: 'var(--spacing-s) var(--spacing-xl)', background: 'var(--neutral-background-2)', borderBottom: '1px solid var(--neutral-stroke-2)' }}>
                                            <span style={{ fontSize: 'var(--font-size-200)', fontWeight: 'var(--font-weight-semibold)', color: 'var(--neutral-foreground-2)', fontFamily: 'var(--font-family-base)', letterSpacing: '.2px' }}>
                                                {g.scenario}
                                            </span>
                                            <span style={{ marginLeft: 'auto' }}>
                                                <Badge appearance="tint" color="informative" shape="rounded">
                                                    {g.cases} {g.cases === 1 ? 'case' : 'cases'}
                                                </Badge>
                                            </span>
                                        </div>
                                    )}
                                    {g.rows.map((m) => (
                                        <div
                                            key={`${g.scenario}-${m.name}`}
                                            className="eval-grid3 eval-cmp-row"
                                            style={{ display: 'grid', gridTemplateColumns: CMP_COLS, columnGap: 'var(--spacing-l)', alignItems: 'center', padding: 'var(--spacing-m) var(--spacing-xl)', borderBottom: '1px solid var(--neutral-stroke-3)', fontSize: 'var(--font-size-300)' }}
                                        >
                                            <span style={{ minWidth: 0, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', color: 'var(--neutral-foreground-1)' }}>
                                                {m.name}
                                            </span>
                                            <span style={{ display: 'grid', gridTemplateColumns: '1fr auto 1fr', alignItems: 'baseline', gap: 'var(--spacing-s)', minWidth: 0, whiteSpace: 'nowrap' }}>
                                                <span style={{ textAlign: 'right', color: 'var(--neutral-foreground-2)', fontVariantNumeric: 'tabular-nums' }}>{m.a}</span>
                                                <span aria-hidden="true" style={{ textAlign: 'center', color: 'var(--neutral-foreground-4)' }}>→</span>
                                                <span style={{ textAlign: 'left', fontWeight: 'var(--font-weight-bold)', color: m.bColor, fontVariantNumeric: 'tabular-nums' }}>{m.b}</span>
                                            </span>
                                            <span style={{ display: 'flex', alignItems: 'center', gap: 'var(--spacing-m)' }}>
                                                <span
                                                    role="img"
                                                    aria-label={`${m.name}: baseline ${effectiveA} ${m.a} → current ${effectiveB} ${m.b}`}
                                                    title={`baseline ${effectiveA} (${m.a}) → current ${effectiveB} (${m.b})`}
                                                    style={{ flex: 1, minWidth: '60px', position: 'relative', height: '16px' }}
                                                >
                                                    <span aria-hidden="true" style={{ position: 'absolute', left: 0, right: 0, top: '50%', height: '1.5px', transform: 'translateY(-50%)', borderRadius: 'var(--radius-circular)', background: 'var(--neutral-stroke-2)' }} />
                                                    <span aria-hidden="true" style={cssText(m.connectorStyle)} />
                                                    <span aria-hidden="true" style={cssText(m.dotBStyle)} />
                                                    <span aria-hidden="true" style={cssText(m.dotAStyle)} />
                                                </span>
                                                <span
                                                    style={{
                                                        width: '64px',
                                                        textAlign: 'right',
                                                        fontSize: 'var(--font-size-300)',
                                                        fontWeight: 'var(--font-weight-semibold)',
                                                        fontVariantNumeric: 'tabular-nums',
                                                        color:
                                                            m.deltaColor === 'success'
                                                                ? 'var(--status-success-foreground-1)'
                                                                : m.deltaColor === 'danger'
                                                                    ? 'var(--status-danger-foreground-1)'
                                                                    : 'var(--neutral-foreground-3)',
                                                    }}
                                                >
                                                    {m.delta}
                                                </span>
                                            </span>
                                        </div>
                                    ))}
                                </div>
                            ))}
                        </div>
                    </div>
                </Card>
            )}

            {effectiveA !== effectiveB && allRows.length === 0 && (
                <div className={s.card}>
                    <div className={local.emptyCard}>
                        <span className={local.emptyTitle}>No comparable numeric metrics</span>
                        <span className={local.emptyReason}>
                            The selected executions share no numeric metrics that can be compared.
                        </span>
                    </div>
                </div>
            )}
        </div>
    );
};

// Parse a `prop:value;` string (from dumbbellStyles) into a React style object.
const cssText = (text: string): React.CSSProperties => {
    const out: Record<string, string> = {};
    for (const decl of text.split(';')) {
        const i = decl.indexOf(':');
        if (i < 0) continue;
        const prop = decl.slice(0, i).trim();
        const value = decl.slice(i + 1).trim();
        if (!prop || !value) continue;
        const camel = prop.replace(/-([a-z])/g, (_, c) => c.toUpperCase());
        out[camel] = value;
    }
    return out as React.CSSProperties;
};
