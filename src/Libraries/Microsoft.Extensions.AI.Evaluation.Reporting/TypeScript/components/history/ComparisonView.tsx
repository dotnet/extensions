// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { useMemo, useEffect, useState, useCallback } from 'react';
import { makeStyles, mergeClasses, Badge, Card, Dropdown, Option } from '@fluentui/react-components';
import { ChevronRight20Regular } from '@fluentui/react-icons';
import { useReportContext } from '../core/ReportContext';
import { useAnnounce } from '../core/Announcer';
import { useReportStyles, srOnlyStyle } from '../styles/reportStyles';
import { chronologicalExecutions } from '../core/viewModels';
import { formatNumber } from '../core/metricModel';
import {
    inferBetterDirections,
    judgeValueDelta,
    judgmentWord,
    ratingGoodness,
    type BetterDirection,
    type DeltaJudgment,
} from '../core/metricDirection';
import { posOn, dumbbellStyles, STATUS_TEXT } from './dumbbellGeometry';
import { axisDomain } from './axisDomain';

type MetricAgg = { sum: number; n: number; goodnessSum: number; goodnessN: number };
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
        const scenAgg = byScen[sn];
        scenAgg.cases++;
        for (const [k, m] of Object.entries(r.evaluationResult?.metrics ?? {})) {
            if (!m || m.$type !== 'numeric') continue;
            const metric = m as NumericMetric;
            if (typeof metric.value !== 'number') continue;
            if (!scenAgg.metricAgg[k]) {
                scenAgg.metricAgg[k] = { sum: 0, n: 0, goodnessSum: 0, goodnessN: 0 };
                scenAgg.metricOrder.push(k);
            }
            const entry = scenAgg.metricAgg[k];
            entry.sum += metric.value;
            entry.n++;
            const goodness = ratingGoodness(metric.interpretation?.rating);
            if (goodness !== undefined) {
                entry.goodnessSum += goodness;
                entry.goodnessN += 1;
            }
        }
    }
    return { byScen, order };
};

type CmpDelta = { txt: string; magText: string };

const deltaStyle = (rawDelta: number): CmpDelta => {
    const flat = Math.abs(rawDelta) < 0.0005;
    const magText = formatNumber(Math.abs(rawDelta));
    const txt = flat ? '—' : (rawDelta > 0 ? '▲ ' : '▼ ') + magText;
    return { txt, magText };
};

type CmpRow = {
    name: string;
    a: string;
    b: string;
    bColor: string;
    delta: string;
    deltaAriaLabel: string;
    status: DeltaJudgment;
    connector: React.CSSProperties;
    dotB: React.CSSProperties;
    dotA: React.CSSProperties;
    baselineAvg: number;
    currentAvg: number;
    rawDelta: number;
    dir: number;
    mag: number;
};

const buildCmpRow = (
    k: string,
    baselineAgg: MetricAgg | undefined,
    currentAgg: MetricAgg | undefined,
    direction: BetterDirection,
): CmpRow => {
    const hasA = !!(baselineAgg && baselineAgg.n);
    const hasB = !!(currentAgg && currentAgg.n);
    const baselineAvg = hasA ? baselineAgg!.sum / baselineAgg!.n : 0;
    const currentAvg = hasB ? currentAgg!.sum / currentAgg!.n : 0;
    const rawDelta = currentAvg - baselineAvg;
    const deltaFmt = deltaStyle(rawDelta);
    const domain = axisDomain(hasA ? [baselineAvg, currentAvg] : [currentAvg]);
    const isFlat = deltaFmt.txt === '—';
    const dir = isFlat || !hasA ? 0 : rawDelta > 0 ? 1 : -1;
    const baselineGoodness = hasA && baselineAgg!.goodnessN > 0 ? baselineAgg!.goodnessSum / baselineAgg!.goodnessN : undefined;
    const currentGoodness = hasB && currentAgg!.goodnessN > 0 ? currentAgg!.goodnessSum / currentAgg!.goodnessN : undefined;
    const goodnessDelta =
        baselineGoodness !== undefined && currentGoodness !== undefined ? currentGoodness - baselineGoodness : undefined;
    const status: DeltaJudgment = dir === 0 ? 'neutral' : judgeValueDelta(direction, rawDelta, goodnessDelta);
    const prevPos = hasA ? posOn(baselineAvg, domain.min, domain.max) : null;
    const curPos = posOn(currentAvg, domain.min, domain.max);
    const dumbbell = dumbbellStyles(prevPos, curPos, !isFlat, status, 0);
    const aStr = hasA ? formatNumber(baselineAvg) : '—';
    const bStr = hasB ? formatNumber(currentAvg) : '—';
    const word = judgmentWord(status);
    const deltaAriaLabel =
        dir === 0
            ? 'no change'
            : `${dir > 0 ? 'increased' : 'decreased'} by ${deltaFmt.magText}${word ? `, ${word}` : ''}`;
    return {
        name: k,
        a: aStr,
        b: bStr,
        bColor: STATUS_TEXT[status],
        delta: deltaFmt.txt,
        deltaAriaLabel,
        status,
        connector: dumbbell.connector,
        dotB: dumbbell.dotB,
        dotA: dumbbell.dotA,
        baselineAvg,
        currentAvg,
        rawDelta,
        dir,
        mag: Math.abs(rawDelta) / (domain.max - domain.min || 1),
    };
};

type SortKey = 'name' | 'a' | 'b' | 'change';

const SORT_LABEL: Record<SortKey, string> = {
    name: 'Metric',
    a: 'Baseline',
    b: 'Current',
    change: 'Change',
};

const CMP_COLS = '1.6fr 1.4fr 2fr';

const useLocalStyles = makeStyles({
    cmpHeaderCard: {
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
    },
    cmpSelectorRow: {
        display: 'flex',
        alignItems: 'stretch',
        gap: 0,
        flexWrap: 'wrap',
        padding: 'var(--spacing-m) var(--spacing-l)',
    },
    cmpColumn: {
        display: 'flex',
        flexDirection: 'column',
        gap: 'var(--spacing-s-nudge)',
        minWidth: 0,
        flex: 1,
    },
    cmpColumnLabel: {
        display: 'flex',
        alignItems: 'center',
        gap: 'var(--spacing-s-nudge)',
        fontSize: 'var(--font-size-100)',
        fontWeight: 'var(--font-weight-semibold)',
        textTransform: 'uppercase',
        letterSpacing: '.5px',
    },
    cmpColumnLabelBaseline: { color: 'var(--neutral-foreground-3)' },
    cmpColumnLabelCurrent: { color: 'var(--brand-foreground-1)' },
    cmpColumnDot: {
        width: '8px',
        height: '8px',
        borderRadius: '50%',
        flex: 'none',
    },
    cmpColumnDotBaseline: { background: 'var(--neutral-foreground-4)' },
    cmpColumnDotCurrent: { background: 'var(--brand-background)' },
    cmpDropdownWrap: { width: '100%' },
    cmpArrowWrap: {
        display: 'flex',
        alignItems: 'flex-end',
        padding: '0 var(--spacing-l)',
    },
    cmpArrowIcon: {
        display: 'inline-flex',
        alignItems: 'center',
        justifyContent: 'center',
        width: '24px',
        height: '32px',
        color: 'var(--neutral-foreground-3)',
    },
    cmpHeadlineRow: {
        display: 'flex',
        alignItems: 'stretch',
        borderTop: '1px solid var(--neutral-stroke-2)',
    },
    cmpHeadlineStat: {
        flex: 1,
        minWidth: 0,
        padding: 'var(--spacing-m-nudge) var(--spacing-l)',
    },
    cmpHeadlineLabel: {
        fontSize: 'var(--font-size-200)',
        color: 'var(--neutral-foreground-3)',
        fontWeight: 'var(--font-weight-semibold)',
        whiteSpace: 'nowrap',
        overflow: 'hidden',
        textOverflow: 'ellipsis',
    },
    cmpHeadlineValue: {
        fontSize: 'var(--font-size-600)',
        fontWeight: 'var(--font-weight-semibold)',
        lineHeight: 1.1,
        whiteSpace: 'nowrap',
        overflow: 'hidden',
        textOverflow: 'ellipsis',
        marginTop: 'var(--spacing-xs)',
        fontVariantNumeric: 'tabular-nums',
    },
    cmpHeadlineSub: {
        fontSize: 'var(--font-size-200)',
        color: 'var(--neutral-foreground-4)',
        marginTop: 'var(--spacing-xxs)',
        whiteSpace: 'nowrap',
        overflow: 'hidden',
        textOverflow: 'ellipsis',
    },
    cmpCardInner: { margin: '-12px' },
    cmpSectionHeader: {
        display: 'flex',
        alignItems: 'baseline',
        justifyContent: 'space-between',
        gap: 'var(--spacing-l)',
        padding: 'var(--spacing-l) var(--spacing-xl) var(--spacing-m)',
        flexWrap: 'wrap',
    },
    cmpSectionTitle: {
        margin: 0,
        fontSize: 'var(--font-size-400)',
        fontWeight: 'var(--font-weight-semibold)',
        color: 'var(--neutral-foreground-1)',
    },
    cmpLegend: {
        display: 'inline-flex',
        alignItems: 'center',
        gap: 'var(--spacing-m)',
        fontSize: 'var(--font-size-200)',
        color: 'var(--neutral-foreground-3)',
        whiteSpace: 'nowrap',
        flexWrap: 'wrap',
    },
    cmpLegendItem: {
        display: 'inline-flex',
        alignItems: 'center',
        gap: 'var(--spacing-xs)',
    },
    cmpLegendDot: {
        width: '8px',
        height: '8px',
        boxSizing: 'border-box',
        borderRadius: '50%',
        display: 'inline-block',
        flex: 'none',
    },
    cmpLegendDotBaseline: {
        background: 'var(--neutral-background-1)',
        border: '1.5px solid var(--neutral-foreground-3)',
    },
    cmpLegendDotCurrent: { background: 'var(--status-success-background-3)' },
    cmpLegendDivider: {
        width: '1px',
        height: 'var(--spacing-m)',
        background: 'var(--neutral-stroke-2)',
    },
    cmpLegendDirection: { color: 'var(--neutral-foreground-3)' },
    cmpLegendSep: { color: 'var(--neutral-foreground-4)' },
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
    cmpTableHeaderRow: {
        display: 'grid',
        gridTemplateColumns: CMP_COLS,
        columnGap: 'var(--spacing-l)',
        alignItems: 'center',
        padding: 'var(--spacing-m-nudge) var(--spacing-xl)',
        fontSize: 'var(--font-size-100)',
        fontWeight: 'var(--font-weight-semibold)',
        color: 'var(--neutral-foreground-4)',
        textTransform: 'uppercase',
        letterSpacing: '.5px',
        borderBottom: '1px solid var(--neutral-stroke-2)',
    },
    cmpColHeaderName: { display: 'flex' },
    cmpColHeaderNameBtn: { whiteSpace: 'nowrap' },
    cmpColHeaderValue: {
        display: 'grid',
        gridTemplateColumns: '1fr auto 1fr',
        alignItems: 'center',
        gap: 'var(--spacing-xs)',
        whiteSpace: 'nowrap',
    },
    cmpColHeaderValueBtnRight: { justifyContent: 'flex-end', textAlign: 'right' },
    cmpColHeaderValueBtnLeft: { justifyContent: 'flex-start', textAlign: 'left' },
    cmpColHeaderValueArrow: {
        color: 'var(--neutral-foreground-4)',
        fontWeight: 'var(--font-weight-regular)',
        textAlign: 'center',
    },
    cmpColHeaderChange: { display: 'flex', justifyContent: 'flex-end' },
    cmpColHeaderChangeBtn: { whiteSpace: 'nowrap', justifyContent: 'flex-end' },
    cmpScenarioRow: {
        padding: 'var(--spacing-s) var(--spacing-xl)',
        background: 'var(--neutral-background-2)',
        borderBottom: '1px solid var(--neutral-stroke-2)',
    },
    cmpScenarioCell: {
        display: 'flex',
        alignItems: 'center',
        gap: 'var(--spacing-s)',
    },
    cmpScenarioName: {
        fontSize: 'var(--font-size-200)',
        fontWeight: 'var(--font-weight-semibold)',
        color: 'var(--neutral-foreground-2)',
        fontFamily: 'var(--font-family-base)',
        letterSpacing: '.2px',
    },
    cmpScenarioBadgeWrap: { marginLeft: 'auto' },
    cmpRow: {
        display: 'grid',
        gridTemplateColumns: CMP_COLS,
        columnGap: 'var(--spacing-l)',
        alignItems: 'center',
        padding: 'var(--spacing-m) var(--spacing-xl)',
        borderBottom: '1px solid var(--neutral-stroke-3)',
        fontSize: 'var(--font-size-300)',
        transition: 'background-color var(--duration-faster) var(--curve-easy-ease)',
        ':hover': { background: 'var(--subtle-background-hover)' },
    },
    cmpNameCell: {
        minWidth: 0,
        overflow: 'hidden',
        textOverflow: 'ellipsis',
        whiteSpace: 'nowrap',
        color: 'var(--neutral-foreground-1)',
    },
    cmpValueCell: {
        display: 'grid',
        gridTemplateColumns: '1fr auto 1fr',
        alignItems: 'baseline',
        gap: 'var(--spacing-s)',
        minWidth: 0,
        whiteSpace: 'nowrap',
    },
    cmpValueA: {
        textAlign: 'right',
        color: 'var(--neutral-foreground-2)',
        fontVariantNumeric: 'tabular-nums',
    },
    cmpValueArrow: { textAlign: 'center', color: 'var(--neutral-foreground-4)' },
    cmpValueB: {
        textAlign: 'left',
        fontWeight: 'var(--font-weight-bold)',
        fontVariantNumeric: 'tabular-nums',
    },
    cmpDumbbellCell: {
        display: 'flex',
        alignItems: 'center',
        gap: 'var(--spacing-m)',
    },
    cmpDumbbellTrack: {
        flex: 1,
        minWidth: '60px',
        position: 'relative',
        height: '16px',
    },
    cmpDumbbellLine: {
        position: 'absolute',
        left: 0,
        right: 0,
        top: '50%',
        height: '1.5px',
        transform: 'translateY(-50%)',
        borderRadius: 'var(--radius-circular)',
        background: 'var(--neutral-stroke-2)',
    },
    cmpDeltaCell: {
        width: '64px',
        textAlign: 'right',
        fontSize: 'var(--font-size-300)',
        fontWeight: 'var(--font-weight-semibold)',
        fontVariantNumeric: 'tabular-nums',
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

export const ComparisonView = () => {
    const local = useLocalStyles();
    const s = useReportStyles();
    const announce = useAnnounce();
    const { dataset, scoreSummary, cmpA, setCmpA, cmpB, setCmpB, selectedScenarioLevel, scopedNode } = useReportContext();

    const emptyState = (title: string, reason: string) => (
        <div className={s.card}>
            <div className={local.emptyCard}>
                <span className={local.emptyTitle}>{title}</span>
                <span className={local.emptyReason}>{reason}</span>
            </div>
        </div>
    );

    const [sortKey, setSortKey] = useState<SortKey>('name');
    const [sortDir, setSortDir] = useState<'asc' | 'desc'>('asc');

    const executions = useMemo(
        () => [...scoreSummary.executionHistory.keys()],
        [scoreSummary],
    );

    const chrono = useMemo(() => chronologicalExecutions(dataset), [dataset]);
    const directions = useMemo(() => inferBetterDirections(dataset.scenarioRunResults ?? []), [dataset]);
    const defaultB = chrono.length >= 1 ? chrono[chrono.length - 1] : undefined;
    const defaultA = chrono.length >= 2 ? chrono[chrono.length - 2] : undefined;

    useEffect(() => {
        if (!cmpA && defaultA) setCmpA(defaultA);
        if (!cmpB && defaultB) setCmpB(defaultB);
    }, [defaultA, defaultB, cmpA, cmpB, setCmpA, setCmpB]);

    const effectiveA = cmpA ?? defaultA;
    const effectiveB = cmpB ?? defaultB;

    const hasTwoExecs = executions.length >= 2;

    const scopedScenarioNames = useMemo(() => {
        if (!selectedScenarioLevel) return undefined;
        return new Set(
            scopedNode.flattenedNodes
                .filter((n) => n.isLeafNode && n.scenario)
                .map((n) => n.scenario!.scenarioName),
        );
    }, [scopedNode, selectedScenarioLevel]);

    const resultsFor = useCallback((execName: string | undefined): ScenarioRunResult[] => {
        if (!execName) return [];
        return (dataset.scenarioRunResults ?? []).filter(
            (r) =>
                r.executionName === execName &&
                (!scopedScenarioNames || scopedScenarioNames.has(r.scenarioName)),
        );
    }, [dataset, scopedScenarioNames]);

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
                else if (sortKey === 'a') r = p.baselineAvg - q.baselineAvg;
                else if (sortKey === 'b') r = p.currentAvg - q.currentAvg;
                else if (sortKey === 'change') r = p.rawDelta - q.rawDelta;
                return r * sdir;
            });
        };

        return scenNames
            .map((sn) => {
                const scenarioBaseline = scnA.byScen[sn];
                const scenarioCurrent = scnB.byScen[sn];
                const order = scenarioCurrent && scenarioCurrent.metricOrder.length
                    ? scenarioCurrent.metricOrder
                    : scenarioBaseline
                        ? scenarioBaseline.metricOrder
                        : [];
                const rows = sortRows(order.map((k) =>
                    buildCmpRow(k, scenarioBaseline?.metricAgg[k], scenarioCurrent?.metricAgg[k], directions.get(k) ?? 'none')));
                return { scenario: sn, rows, cases: (scenarioCurrent ?? scenarioBaseline ?? { cases: 0 }).cases };
            })
            .filter((g) => g.rows.length > 0);
    }, [resultsFor, effectiveA, effectiveB, hasTwoExecs, sortKey, sortDir, directions]);

    const allRows = useMemo(() => groups.reduce<CmpRow[]>((acc, g) => acc.concat(g.rows), []), [groups]);
    const multiScenario = groups.length > 1;

    const increased = allRows.filter((r) => r.dir > 0).length;
    const decreased = allRows.filter((r) => r.dir < 0).length;
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
            label: 'Metrics increased',
            value: '' + increased,
            valueColor: 'var(--neutral-foreground-1)',
            sub: 'of ' + allRows.length + ' metrics',
            borderRight: DIV,
        },
        {
            label: 'Metrics decreased',
            value: '' + decreased,
            valueColor: 'var(--neutral-foreground-1)',
            sub: 'of ' + allRows.length + ' metrics',
            borderRight: DIV,
        },
        biggest
            ? {
                  label: 'Biggest change',
                  value: biggest.delta,
                  valueColor: STATUS_TEXT[biggest.status],
                  sub: biggest.name,
                  borderRight: 'none',
              }
            : {
                  label: 'Biggest change',
                  value: 'stable',
                  valueColor: 'var(--neutral-foreground-1)',
                  sub: 'no significant change',
                  borderRight: 'none',
              },
    ];

    const onSort = (key: SortKey) => {
        const dir: 'asc' | 'desc' = sortKey === key ? (sortDir === 'asc' ? 'desc' : 'asc') : 'asc';
        setSortKey(key);
        setSortDir(dir);
        announce(`Sorted by ${SORT_LABEL[key]}, ${dir === 'asc' ? 'ascending' : 'descending'}`);
    };
    const sortArrow = (key: SortKey) => (sortKey === key ? (sortDir === 'asc' ? ' ▲' : ' ▼') : '');
    const ariaSort = (key: SortKey): 'ascending' | 'descending' | 'none' =>
        sortKey === key ? (sortDir === 'asc' ? 'ascending' : 'descending') : 'none';
    const ariaLabel = (key: SortKey, label: string) =>
        sortKey === key
            ? `${label}, sorted ${sortDir === 'asc' ? 'ascending' : 'descending'}. Activate to sort ${sortDir === 'asc' ? 'descending' : 'ascending'}.`
            : `Sort by ${label}`;

    if (!hasTwoExecs) {
        return emptyState(
            'Needs at least 2 executions',
            'Run the evaluation suite across multiple executions to compare results side by side.',
        );
    }

    return (
        <div>
            <div className={local.cmpHeaderCard}>
                <div
                    className={mergeClasses('eval-cmp-selector', local.cmpSelectorRow)}
                >
                    <div className={local.cmpColumn}>
                        <span className={mergeClasses(local.cmpColumnLabel, local.cmpColumnLabelBaseline)}>
                            <span aria-hidden="true" className={mergeClasses(local.cmpColumnDot, local.cmpColumnDotBaseline)} />
                            Baseline
                        </span>
                        <div className={local.cmpDropdownWrap}>
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

                    <div className={mergeClasses('eval-cmp-arrowwrap', local.cmpArrowWrap)}>
                        <span aria-hidden="true" className={mergeClasses('eval-cmp-arrow', local.cmpArrowIcon)}>
                            <ChevronRight20Regular />
                        </span>
                    </div>

                    <div className={local.cmpColumn}>
                        <span className={mergeClasses(local.cmpColumnLabel, local.cmpColumnLabelCurrent)}>
                            <span aria-hidden="true" className={mergeClasses(local.cmpColumnDot, local.cmpColumnDotCurrent)} />
                            Current
                        </span>
                        <div className={local.cmpDropdownWrap}>
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
                    <div className={local.cmpHeadlineRow}>
                        {headline.map((c) => (
                            <div key={c.label} className={local.cmpHeadlineStat} style={{ borderRight: c.borderRight }}>
                                <div className={local.cmpHeadlineLabel}>
                                    {c.label}
                                </div>
                                <div className={local.cmpHeadlineValue} style={{ color: c.valueColor }}>
                                    {c.value}
                                </div>
                                <div className={local.cmpHeadlineSub}>
                                    {c.sub}
                                </div>
                            </div>
                        ))}
                    </div>
                )}
            </div>

            {effectiveA === effectiveB && emptyState(
                'Select two different executions',
                'The baseline and current executions are the same. Choose different executions to see the delta.',
            )}

            {effectiveA !== effectiveB && allRows.length > 0 && (
                <Card appearance="outline">
                    <div className={local.cmpCardInner}>
                        <div className={local.cmpSectionHeader}>
                            <h2 className={local.cmpSectionTitle}>
                                Per-metric change
                            </h2>
                            <span className={local.cmpLegend}>
                                <span className={local.cmpLegendItem}>
                                    <span aria-hidden="true" className={mergeClasses(local.cmpLegendDot, local.cmpLegendDotBaseline)} /> baseline
                                </span>
                                <span className={local.cmpLegendItem}>
                                    <span aria-hidden="true" className={mergeClasses(local.cmpLegendDot, local.cmpLegendDotCurrent)} /> current
                                </span>
                                <span className={local.cmpLegendDivider} />
                                <span className={local.cmpLegendItem}>
                                    <span aria-hidden="true" className={local.cmpLegendDirection}>▲</span> increased <span aria-hidden="true" className={local.cmpLegendSep}>·</span> <span aria-hidden="true" className={local.cmpLegendDirection}>▼</span> decreased
                                </span>
                            </span>
                        </div>

                        <div className={s.tscroll} role="table" aria-label="Per-metric comparison" tabIndex={0}>
                            <div
                                role="row"
                                className={mergeClasses('eval-grid3', local.cmpTableHeaderRow)}
                            >
                                <span role="columnheader" aria-sort={ariaSort('name')} className={local.cmpColHeaderName}>
                                    <button type="button" className={mergeClasses(local.sortBtn, local.cmpColHeaderNameBtn)} onClick={() => onSort('name')} aria-label={ariaLabel('name', 'Metric')}>
                                        Metric{sortArrow('name')}
                                    </button>
                                </span>
                                <span
                                    role="columnheader"
                                    aria-sort={sortKey === 'a' || sortKey === 'b' ? ariaSort(sortKey) : 'none'}
                                    className={local.cmpColHeaderValue}
                                >
                                    <button type="button" className={mergeClasses(local.sortBtn, local.cmpColHeaderValueBtnRight)} onClick={() => onSort('a')} aria-label={ariaLabel('a', 'Baseline')}>
                                        Baseline{sortArrow('a')}
                                    </button>
                                    <span aria-hidden="true" className={local.cmpColHeaderValueArrow}>→</span>
                                    <button type="button" className={mergeClasses(local.sortBtn, local.cmpColHeaderValueBtnLeft)} onClick={() => onSort('b')} aria-label={ariaLabel('b', 'Current')}>
                                        Current{sortArrow('b')}
                                    </button>
                                </span>
                                <span role="columnheader" aria-sort={ariaSort('change')} className={local.cmpColHeaderChange}>
                                    <button type="button" className={mergeClasses(local.sortBtn, local.cmpColHeaderChangeBtn)} onClick={() => onSort('change')} aria-label={ariaLabel('change', 'Δ run')}>
                                        Δ run{sortArrow('change')}
                                    </button>
                                </span>
                            </div>

                            {groups.map((g) => (
                                <div key={g.scenario} role="rowgroup">
                                    {multiScenario && (
                                        <div role="row" className={local.cmpScenarioRow}>
                                            <span role="cell" aria-colspan={3} className={local.cmpScenarioCell}>
                                                <span className={local.cmpScenarioName}>
                                                    {g.scenario}
                                                </span>
                                                <span className={local.cmpScenarioBadgeWrap}>
                                                    <Badge appearance="tint" color="informative" shape="rounded">
                                                        {g.cases} {g.cases === 1 ? 'case' : 'cases'}
                                                    </Badge>
                                                </span>
                                            </span>
                                        </div>
                                    )}
                                    {g.rows.map((m) => (
                                        <div
                                            key={`${g.scenario}-${m.name}`}
                                            role="row"
                                            className={mergeClasses('eval-grid3', local.cmpRow)}
                                        >
                                            <span role="cell" className={local.cmpNameCell}>
                                                {m.name}
                                            </span>
                                            <span role="cell" className={local.cmpValueCell}>
                                                <span className={local.cmpValueA}>{m.a}</span>
                                                <span aria-hidden="true" className={local.cmpValueArrow}>→</span>
                                                <span
                                                    className={local.cmpValueB}
                                                    style={{ color: m.bColor }}
                                                >
                                                    {m.b}
                                                </span>
                                            </span>
                                            <span role="cell" className={local.cmpDumbbellCell}>
                                                <span
                                                    role="img"
                                                    aria-label={`${m.name}: baseline ${effectiveA} ${m.a} → current ${effectiveB} ${m.b}`}
                                                    title={`baseline ${effectiveA} (${m.a}) → current ${effectiveB} (${m.b})`}
                                                    className={local.cmpDumbbellTrack}
                                                >
                                                    <span aria-hidden="true" className={local.cmpDumbbellLine} />
                                                    <span aria-hidden="true" style={m.connector} />
                                                    <span aria-hidden="true" style={m.dotB} />
                                                    <span aria-hidden="true" style={m.dotA} />
                                                </span>
                                                <span
                                                    className={local.cmpDeltaCell}
                                                    style={{ color: STATUS_TEXT[m.status] }}
                                                >
                                                    <span aria-hidden="true">{m.delta}</span>
                                                    <span style={srOnlyStyle}>{m.deltaAriaLabel}</span>
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

            {effectiveA !== effectiveB && allRows.length === 0 && emptyState(
                'No comparable numeric metrics',
                'The selected executions share no numeric metrics that can be compared.',
            )}
        </div>
    );
};
