// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { useMemo } from 'react';
import { Badge, Card, ProgressBar } from '@fluentui/react-components';
import { useReportContext } from './ReportContext';
import {
    useReportStyles,
    statusSolidVar,
    type ReportStatus,
} from './reportStyles';
import {
    kpiCountsFromNode,
    bucketMetrics,
    ratingBucket,
    passRateByScenarioGroup,
    chronologicalExecutions,
    moversBetween,
    formatScore,
    scenariosForExecution,
    isLeafFailed,
    type MoverRow,
    type ScenarioGroupPassRate,
    type KpiCounts,
} from './viewModels';

const pctInt = (r: number) => Math.round(r * 100);

type DeltaChip = {
    show: boolean;
    label: string;
    status: 'success' | 'danger' | 'informative';
};

const chip = (
    delta: number | undefined,
    opts: { unit?: string; lowerBetter?: boolean } = {},
): DeltaChip => {
    if (delta === undefined || delta === 0) {
        return { show: delta !== undefined, label: '—', status: 'informative' };
    }
    const unit = opts.unit ?? '';
    const good = opts.lowerBetter ? delta < 0 : delta > 0;
    const arrow = delta > 0 ? '▲' : '▼';
    const sign = delta > 0 ? '+' : '−';
    return {
        show: true,
        label: `${arrow} ${sign}${Math.abs(delta)}${unit}`,
        status: good ? 'success' : 'danger',
    };
};

const numDeltaChip = (d: number): DeltaChip => {
    if (Math.abs(d) < 0.05) return { show: true, label: '—', status: 'informative' };
    const arrow = d > 0 ? '▲' : '▼';
    const sign = d > 0 ? '+' : '−';
    return { show: true, label: `${arrow} ${sign}${Math.abs(d).toFixed(1)}`, status: d > 0 ? 'success' : 'danger' };
};

const rateStatus = (rate: number): ReportStatus =>
    rate >= 0.9 ? 'success'
        : rate >= 0.75 ? 'caution'
            : rate >= 0.5 ? 'warning'
                : 'danger';

const auraDotStyle = (status: ReportStatus): React.CSSProperties => {
    const solid = statusSolidVar(status);
    return {
        width: '8px',
        height: '8px',
        borderRadius: '50%',
        flex: 'none',
        boxSizing: 'border-box',
        background: solid,
        boxShadow: `0 0 0 3px color-mix(in srgb, ${solid} 18%, transparent)`,
    };
};

const chipToStatus = (s: DeltaChip['status']): ReportStatus =>
    s === 'success' ? 'success' : s === 'danger' ? 'danger' : 'neutral';

type AttentionItem = {
    key: string;
    label: string;
    scenario: string;
    status: ReportStatus;
    statStr: string;
    failing: number;
    share: number;
};

const attentionItems = (scenarios: ScenarioRunResult[], limit = 3): AttentionItem[] => {
    const agg = new Map<string, { scenario: string; metric: string; danger: number; warning: number; total: number }>();
    for (const s of scenarios) {
        for (const [metricName, metric] of Object.entries(s.evaluationResult.metrics)) {
            const bucket = ratingBucket(metric?.interpretation?.rating);
            const id = `${s.scenarioName}${metricName}`;
            const entry = agg.get(id) ?? { scenario: s.scenarioName, metric: metricName, danger: 0, warning: 0, total: 0 };
            entry.total += 1;
            if (bucket === 'weak') entry.danger += 1;
            else if (bucket === 'fair') entry.warning += 1;
            agg.set(id, entry);
        }
    }

    return [...agg.entries()]
        .map(([key, a]) => {
            const bad = a.danger + a.warning;
            const status: ReportStatus = a.danger >= a.warning ? 'danger' : 'warning';
            const weakPct = a.total > 0 ? Math.round((a.warning / a.total) * 100) : 0;
            return {
                key,
                label: `${a.scenario} · ${a.metric}`,
                scenario: a.scenario,
                status,
                statStr: `${a.danger} failing · ${weakPct}% weak`,
                failing: a.danger,
                share: a.total > 0 ? bad / a.total : 0,
            };
        })
        .filter((w) => w.failing > 0 || w.share > 0)
        .sort((x, y) => y.share - x.share)
        .slice(0, limit);
};

const DeltaBadge = ({ chip, size = 'medium', shape = 'rounded', appearance = 'ghost' }: { chip: DeltaChip; size?: 'small' | 'medium'; shape?: 'rounded' | 'circular'; appearance?: 'ghost' | 'tint' }) =>
    chip.show ? (
        <Badge appearance={appearance} color={chip.status} size={size} shape={shape}>
            {chip.label}
        </Badge>
    ) : null;

const SummaryCard = ({
    passRate,
    passChip,
    kpis,
}: {
    passRate: number;
    passChip: DeltaChip;
    kpis: { label: string; value: string; sub: string; chip: DeltaChip; last?: boolean }[];
}) => {
    const s = useReportStyles();
    const passNow = pctInt(passRate);
    const fillStatus = rateStatus(passRate);

    return (
        <div className="eval-hero-acrylic" style={{ marginBottom: 'var(--spacing-l)' }}>
            <Card appearance="outline">
                <div style={{ margin: '-12px', overflow: 'hidden', borderRadius: 'var(--radius-card)', display: 'flex', flexDirection: 'column' }}>
                    <div style={{ flex: 1, display: 'flex', alignItems: 'center', gap: 'var(--spacing-xxl)', padding: 'var(--spacing-l) 0 var(--spacing-l) var(--spacing-xl)', flexWrap: 'wrap' }}>
                        <div style={{ flex: 'none' }}>
                            <div className={s.eyebrow}>Overall pass rate</div>
                            <div style={{ display: 'flex', alignItems: 'baseline', gap: 'var(--spacing-m)', marginTop: 'var(--spacing-xs)' }}>
                                <span className="eval-hero-passrate" style={{ fontSize: 'var(--font-size-800)', fontWeight: 'var(--font-weight-semibold)', lineHeight: 1, color: 'var(--neutral-foreground-1)', fontVariantNumeric: 'tabular-nums' }}>
                                    {passNow}
                                    <span style={{ fontSize: 'var(--font-size-500)', fontWeight: 'var(--font-weight-semibold)', color: 'var(--neutral-foreground-3)' }}>%</span>
                                </span>
                                <DeltaBadge chip={passChip} size="medium" shape="circular" />
                            </div>
                        </div>
                        <div className="eval-hero-kpis" style={{ marginLeft: 'auto', display: 'flex', alignItems: 'stretch', borderLeft: '1px solid var(--neutral-stroke-2)' }}>
                            {kpis.map((k) => (
                                <div key={k.label} className="eval-hero-kpi" style={{ padding: '0 var(--spacing-xl)', borderRight: k.last ? 'none' : '1px solid var(--neutral-stroke-2)' }}>
                                    <div className={s.eyebrow} style={{ whiteSpace: 'nowrap' }}>{k.label}</div>
                                    <div style={{ display: 'flex', alignItems: 'baseline', gap: 'var(--spacing-s)', marginTop: 'var(--spacing-xs)' }}>
                                        <span style={{ fontSize: 'var(--font-size-600)', fontWeight: 'var(--font-weight-semibold)', lineHeight: 1, whiteSpace: 'nowrap', color: 'var(--neutral-foreground-1)', fontVariantNumeric: 'tabular-nums' }}>
                                            {k.value}
                                        </span>
                                        <DeltaBadge chip={k.chip} />
                                        <span style={{ fontSize: 'var(--font-size-100)', color: 'var(--neutral-foreground-3)', whiteSpace: 'nowrap' }}>{k.sub}</span>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>
                    <ProgressBar
                        value={passNow / 100}
                        thickness="medium"
                        className="eval-grprate"
                        aria-label={`Overall pass rate ${passNow}%`}
                        style={{ ['--eval-bar']: statusSolidVar(fillStatus) } as React.CSSProperties}
                    />
                </div>
            </Card>
        </div>
    );
};

const MoversCard = ({ movers, compareLabel }: { movers: MoverRow[]; compareLabel?: string }) => {
    const s = useReportStyles();
    return (
        <Card appearance="outline">
            <div style={{ margin: '-12px' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 'var(--spacing-m-nudge)', padding: 'var(--spacing-l) var(--spacing-xl) var(--spacing-m)', borderBottom: '1px solid var(--neutral-stroke-2)' }}>
                    <span
                        aria-hidden="true"
                        style={{ flex: 'none', width: '26px', height: '26px', borderRadius: '50%', display: 'inline-flex', alignItems: 'center', justifyContent: 'center', background: 'color-mix(in srgb, var(--status-success-foreground-1) 15%, transparent)' }}
                    >
                        <svg width="15" height="15" viewBox="0 0 16 16" fill="none" stroke="var(--status-success-foreground-1)" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
                            <path d="M5 11 L11 5 M6.5 5 H11 V9.5" />
                        </svg>
                    </span>
                    <div style={{ display: 'flex', alignItems: 'baseline', gap: 'var(--spacing-s-nudge)', minWidth: 0 }}>
                        <h3 className={s.sectionHeaderTitle}>Biggest movers</h3>
                        {compareLabel && <span className={s.sectionHeaderSub}>vs {compareLabel}</span>}
                    </div>
                </div>
                <div style={{ display: 'grid', gridTemplateColumns: 'minmax(0,1fr) auto auto', alignItems: 'center', columnGap: 'var(--spacing-m)', gridAutoRows: '44px', padding: 'var(--spacing-s) var(--spacing-xl)' }}>
                    {movers.map((m) => {
                        const dc = numDeltaChip(m.delta);
                        const dotStatus = chipToStatus(dc.status);
                        const valStr = formatScore(m.value, m.kind);
                        return [
                            <span key={`${m.scenarioName}-${m.metricName}-n`} style={{ display: 'flex', alignItems: 'center', gap: 'var(--spacing-m)', minWidth: 0 }}>
                                <span style={auraDotStyle(dotStatus)} />
                                <span
                                    title={`${m.scenarioName} · ${m.metricName}`}
                                    style={{ fontSize: 'var(--font-size-300)', minWidth: 0, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis', color: 'var(--neutral-foreground-1)' }}
                                >
                                    {m.scenarioName} · {m.metricName}
                                </span>
                            </span>,
                            <span key={`${m.scenarioName}-${m.metricName}-v`} style={{ fontSize: 'var(--font-size-200)', color: 'var(--neutral-foreground-4)', fontVariantNumeric: 'tabular-nums', textAlign: 'right', justifySelf: 'end' }}>
                                {valStr}
                            </span>,
                            <span key={`${m.scenarioName}-${m.metricName}-d`} style={{ display: 'inline-flex', justifySelf: 'end' }}>
                                {dc.status === 'informative'
                                    ? <span style={{ fontSize: 'var(--font-size-200)', color: 'var(--neutral-foreground-3)', fontVariantNumeric: 'tabular-nums', justifySelf: 'end' }}>—</span>
                                    : <DeltaBadge chip={dc} shape="circular" appearance="tint" />}
                            </span>,
                        ];
                    })}
                </div>
            </div>
        </Card>
    );
};

const NeedsAttentionCard = ({ items, onView }: { items: AttentionItem[]; onView: (item: AttentionItem) => void }) => {
    const s = useReportStyles();
    return (
        <Card appearance="outline">
            <div style={{ margin: '-12px' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 'var(--spacing-m-nudge)', padding: 'var(--spacing-l) var(--spacing-xl) var(--spacing-m)', borderBottom: '1px solid var(--neutral-stroke-2)' }}>
                    <span
                        aria-hidden="true"
                        style={{ flex: 'none', width: '26px', height: '26px', borderRadius: '50%', display: 'inline-flex', alignItems: 'center', justifyContent: 'center', background: 'color-mix(in srgb, var(--status-danger-foreground-1) 15%, transparent)' }}
                    >
                        <svg width="15" height="15" viewBox="0 0 16 16" fill="none" stroke="var(--status-danger-foreground-1)" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
                            <path d="M5 5 L11 11 M11 6.5 V11 H6.5" />
                        </svg>
                    </span>
                    <h3 className={s.sectionHeaderTitle}>Needs attention</h3>
                </div>
                {items.length > 0 ? (
                    <div style={{ display: 'flex', flexDirection: 'column', padding: 'var(--spacing-s)' }}>
                        {items.map((a) => (
                            <div key={a.key} style={{ display: 'flex', alignItems: 'center', gap: 'var(--spacing-m)', padding: '0 var(--spacing-m)', height: '44px' }}>
                                <span style={auraDotStyle(a.status)} />
                                <span title={a.label} style={{ flex: 1, minWidth: 0, fontSize: 'var(--font-size-300)', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis', color: 'var(--neutral-foreground-1)' }}>
                                    {a.label}
                                </span>
                                <span style={{ flex: 'none', fontSize: 'var(--font-size-200)', color: 'var(--neutral-foreground-3)', fontVariantNumeric: 'tabular-nums', whiteSpace: 'nowrap' }}>
                                    {a.statStr}
                                </span>
                                <span style={{ flex: 'none', display: 'inline-flex' }}>
                                    <button className={s.viewLink} onClick={() => onView(a)} aria-label={`View cases for ${a.label}`}>
                                        View
                                    </button>
                                </span>
                            </div>
                        ))}
                    </div>
                ) : (
                    <div style={{ display: 'flex', alignItems: 'center', gap: 'var(--spacing-s-nudge)', fontSize: 'var(--font-size-300)', color: 'var(--status-success-foreground-1)', padding: 'var(--spacing-l) var(--spacing-xl)' }}>
                        No fair or weak ratings in scope
                    </div>
                )}
            </div>
        </Card>
    );
};

const GROUP_COLS = '1.6fr 0.7fr 0.7fr 0.7fr 2.4fr 64px';

const GroupTable = ({
    rows,
    activeScenarios,
    totalKpi,
    totalGoodPct,
    passChip,
}: {
    rows: ScenarioGroupPassRate[];
    activeScenarios: ScenarioRunResult[];
    totalKpi: KpiCounts;
    totalGoodPct: number;
    passChip: DeltaChip;
}) => {
    return (
        <Card appearance="outline">
            <div style={{ margin: '-12px' }}>
                <h3 style={{ margin: 0, padding: 'var(--spacing-l) var(--spacing-xl) var(--spacing-m)', fontSize: 'var(--font-size-400)', fontWeight: 'var(--font-weight-semibold)', color: 'var(--neutral-foreground-1)' }}>
                    Pass rate by scenario group
                </h3>
                <div className="eval-tscroll">
                    <div className="eval-grid6" style={{ display: 'grid', gridTemplateColumns: GROUP_COLS, padding: 'var(--spacing-m-nudge) var(--spacing-xl)', fontSize: 'var(--font-size-100)', fontWeight: 'var(--font-weight-semibold)', color: 'var(--neutral-foreground-4)', textTransform: 'uppercase', letterSpacing: '.5px', borderBottom: '1px solid var(--neutral-stroke-2)' }}>
                        <span>Scenario group</span>
                        <span style={{ textAlign: 'right' }}>Good</span>
                        <span style={{ textAlign: 'right' }}>Fair</span>
                        <span style={{ textAlign: 'right' }}>Weak</span>
                        <span style={{ textAlign: 'right', paddingRight: 'var(--spacing-l)' }}>Pass rate</span>
                        <span style={{ textAlign: 'right' }}>Δ run</span>
                    </div>
                    {rows.map((row) => {
                        const groupScenarios = activeScenarios.filter((sc) => sc.scenarioName.split('.')[0] === row.group);
                        const gb = bucketMetrics(groupScenarios);
                        const status = rateStatus(row.passRate);
                        const ratePct = pctInt(row.passRate);
                        const deltaBadgeChip = row.deltaRun !== undefined ? chip(Math.round(row.deltaRun * 100), { unit: '%' }) : { show: false, label: '', status: 'informative' as const };
                        return (
                            <div key={row.group} className="eval-grid6" style={{ display: 'grid', gridTemplateColumns: GROUP_COLS, alignItems: 'center', padding: 'var(--spacing-m) var(--spacing-xl)', borderBottom: '1px solid var(--neutral-stroke-3)' }}>
                                <span style={{ display: 'flex', alignItems: 'center', gap: 'var(--spacing-s)', fontSize: 'var(--font-size-300)', minWidth: 0 }}>
                                    <span style={auraDotStyle(status)} />
                                    <span style={{ whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>{row.group}</span>
                                </span>
                                <span style={{ textAlign: 'right', fontSize: 'var(--font-size-300)', color: 'var(--neutral-foreground-1)', fontVariantNumeric: 'tabular-nums' }}>{gb.good}</span>
                                <span style={{ textAlign: 'right', fontSize: 'var(--font-size-300)', color: 'var(--neutral-foreground-1)', fontVariantNumeric: 'tabular-nums' }}>{gb.fair}</span>
                                <span style={{ textAlign: 'right', fontSize: 'var(--font-size-300)', color: 'var(--neutral-foreground-1)', fontVariantNumeric: 'tabular-nums' }}>{gb.weak}</span>
                                <span style={{ display: 'flex', flexDirection: 'column', gap: 'var(--spacing-s-nudge)', paddingLeft: 'var(--spacing-xxl)', paddingRight: 'var(--spacing-l)' }}>
                                    <span style={{ textAlign: 'right', fontSize: 'var(--font-size-200)', fontWeight: 'var(--font-weight-semibold)', color: 'var(--neutral-foreground-1)', fontVariantNumeric: 'tabular-nums', lineHeight: 1 }}>{ratePct}%</span>
                                    <ProgressBar
                                        value={ratePct / 100}
                                        thickness="medium"
                                        className="eval-grprate"
                                        aria-label={`Pass rate ${ratePct}%`}
                                        style={{ ['--eval-bar']: statusSolidVar(status) } as React.CSSProperties}
                                    />
                                </span>
                                <span
                                    style={{
                                        textAlign: 'right',
                                        fontSize: 'var(--font-size-300)',
                                        fontWeight: 'var(--font-weight-semibold)',
                                        fontVariantNumeric: 'tabular-nums',
                                        color:
                                            deltaBadgeChip.status === 'success'
                                                ? 'var(--status-success-foreground-1)'
                                                : deltaBadgeChip.status === 'danger'
                                                    ? 'var(--status-danger-foreground-1)'
                                                    : 'var(--neutral-foreground-3)',
                                    }}
                                >
                                    {deltaBadgeChip.show ? deltaBadgeChip.label : '—'}
                                </span>
                            </div>
                        );
                    })}
                    <div className="eval-grid6" style={{ display: 'grid', gridTemplateColumns: GROUP_COLS, alignItems: 'center', padding: 'var(--spacing-m) var(--spacing-xl)', fontWeight: 'var(--font-weight-semibold)' }}>
                        <span style={{ fontSize: 'var(--font-size-300)' }}>All scenarios</span>
                        <span style={{ textAlign: 'right', fontSize: 'var(--font-size-300)', color: 'var(--neutral-foreground-1)' }}>{totalGoodPct}%</span>
                        <span />
                        <span />
                        <span style={{ display: 'flex', alignItems: 'center', paddingRight: 'var(--spacing-l)', justifyContent: 'flex-end' }}>
                            <span style={{ fontSize: 'var(--font-size-300)', textAlign: 'right' }}>{pctInt(totalKpi.passRate)}%</span>
                        </span>
                        <span
                            style={{
                                textAlign: 'right',
                                fontSize: 'var(--font-size-300)',
                                fontWeight: 'var(--font-weight-semibold)',
                                fontVariantNumeric: 'tabular-nums',
                                color:
                                    passChip.status === 'success'
                                        ? 'var(--status-success-foreground-1)'
                                        : passChip.status === 'danger'
                                            ? 'var(--status-danger-foreground-1)'
                                            : 'var(--neutral-foreground-3)',
                            }}
                        >
                            {passChip.show ? passChip.label : '—'}
                        </span>
                    </div>
                </div>
            </div>
        </Card>
    );
};

export const OverviewView = () => {
    const { dataset, scoreSummary, activeExecution, activeNode, scopedNode, selectedScenarioLevel, selectScenarioLevel, setView } = useReportContext();

    // Scenarios for the active execution, restricted to the current sidebar selection.
    // When "All scenarios" is selected scopedNode === activeNode, so this yields every scenario.
    const activeScenarios = useMemo(() => {
        const all = scenariosForExecution(dataset, activeExecution);
        if (!selectedScenarioLevel) return all;
        const scopedNames = new Set(
            scopedNode.flattenedNodes
                .filter((n) => n.isLeafNode && n.scenario)
                .map((n) => n.scenario!.scenarioName),
        );
        return all.filter((s) => scopedNames.has(s.scenarioName));
    }, [dataset, activeExecution, scopedNode, selectedScenarioLevel]);

    const kpi = useMemo(() => kpiCountsFromNode(scopedNode), [scopedNode]);
    const buckets = useMemo(() => bucketMetrics(activeScenarios), [activeScenarios]);

    const groupRows = useMemo(() => {
        const all = passRateByScenarioGroup(dataset, activeExecution);
        if (!selectedScenarioLevel) return all;
        const scopedGroups = new Set(activeScenarios.map((s) => s.scenarioName.split('.')[0]));
        return all.filter((r) => scopedGroups.has(r.group));
    }, [dataset, activeExecution, activeScenarios, selectedScenarioLevel]);

    const totalDeltaRun = useMemo(() => {
        const rowsWithDelta = groupRows.filter((r) => r.deltaRun !== undefined);
        if (rowsWithDelta.length === 0) return undefined;
        const totalTotal = rowsWithDelta.reduce((sum, r) => sum + r.total, 0);
        const prevPassing = rowsWithDelta.reduce((sum, r) => sum + (r.passing - (r.deltaRun ?? 0) * r.total), 0);
        if (totalTotal === 0) return undefined;
        return kpi.passRate - prevPassing / totalTotal;
    }, [groupRows, kpi.passRate]);

    // Movers compare the selected run against its chronologically-immediate
    // predecessor (creationTime order), not an insertion index — correct under
    // both newest-first dev data and oldest-first fixtures. When the selected run
    // is the earliest, there is no predecessor: no movers, no compare label.
    const compareLabel = useMemo(() => {
        const chrono = chronologicalExecutions(dataset);
        const idx = chrono.indexOf(activeExecution);
        return idx > 0 ? chrono[idx - 1] : undefined;
    }, [dataset, activeExecution]);

    const movers = useMemo(() => {
        const rows = moversBetween(
            dataset.scenarioRunResults,
            activeExecution,
            compareLabel,
            selectedScenarioLevel ? Infinity : 5,
        );
        if (!selectedScenarioLevel) return rows;
        const scopedNames = new Set(activeScenarios.map((s) => s.scenarioName));
        return rows.filter((m) => scopedNames.has(m.scenarioName)).slice(0, 5);
    }, [dataset, activeExecution, compareLabel, activeScenarios, selectedScenarioLevel]);

    const attention = useMemo(() => attentionItems(activeScenarios), [activeScenarios]);

    const groupsFullyPassing = groupRows.filter((r) => r.passRate >= 1 && r.total > 0).length;
    const totalGroups = groupRows.length;

    const totalEvals = buckets.good + buckets.fair + buckets.weak + buckets.unknown;
    const goodPct = Math.round((buckets.good / Math.max(1, totalEvals)) * 100);

    const prev = useMemo(() => {
        if (!compareLabel) return undefined;
        const prevNode = scoreSummary.executionHistory.get(compareLabel);
        if (!prevNode) return undefined;

        // Scope the comparison baseline to the same sidebar selection so the delta
        // chips compare scoped-current against scoped-previous (apples to apples).
        const scopedNames = selectedScenarioLevel
            ? new Set(activeScenarios.map((s) => s.scenarioName))
            : undefined;
        const prevScenarios = scopedNames
            ? scenariosForExecution(dataset, compareLabel).filter((s) => scopedNames.has(s.scenarioName))
            : scenariosForExecution(dataset, compareLabel);

        const prevFailing = scopedNames
            ? prevScenarios.filter((s) => isLeafFailed(s)).length
            : kpiCountsFromNode(prevNode).failing;
        const prevBuckets = bucketMetrics(prevScenarios);
        const prevEvals = prevBuckets.good + prevBuckets.fair + prevBuckets.weak + prevBuckets.unknown;
        const prevGroups = passRateByScenarioGroup(dataset, compareLabel).filter(
            (r) => !scopedNames || activeScenarios.some((s) => s.scenarioName.split('.')[0] === r.group),
        );
        const prevScenPass = prevGroups.filter((r) => r.passRate >= 1 && r.total > 0).length;
        return {
            failing: prevFailing,
            scenPass: prevScenPass,
            goodPct: Math.round((prevBuckets.good / Math.max(1, prevEvals)) * 100),
        };
    }, [compareLabel, dataset, scoreSummary, activeScenarios, selectedScenarioLevel]);

    const passChip = chip(totalDeltaRun !== undefined ? Math.round(totalDeltaRun * 100) : undefined, { unit: '%' });
    const failChip = chip(prev ? kpi.failing - prev.failing : undefined, { lowerBetter: true });
    const scenChip = chip(prev ? groupsFullyPassing - prev.scenPass : undefined);
    const goodChip = chip(prev ? goodPct - prev.goodPct : undefined, { unit: '%' });

    const hasMovers = movers.length > 0;

    const openCasesForScenario = (item: AttentionItem) => {
        const leaf = activeNode.flattenedNodes.find(
            (n) => n.isLeafNode && n.scenario?.scenarioName === item.scenario,
        );
        const nodeKey = leaf ? leaf.nodeKey.slice(0, leaf.nodeKey.lastIndexOf('.')) : `root.${item.scenario}`;
        if (nodeKey !== selectedScenarioLevel) selectScenarioLevel(nodeKey);
        setView('cases');
    };

    const kpis = [
        { label: 'Cases failing', value: String(kpi.failing), sub: `of ${kpi.total} cases`, chip: failChip },
        { label: 'Scenarios fully passing', value: `${groupsFullyPassing} / ${totalGroups}`, sub: groupsFullyPassing === totalGroups ? 'no failing case' : `${totalGroups - groupsFullyPassing} with failures`, chip: scenChip },
        { label: 'Good ratings', value: `${goodPct}%`, sub: `${buckets.good} of ${totalEvals} evals`, chip: goodChip, last: true },
    ];

    return (
        <div>
            <SummaryCard passRate={kpi.passRate} passChip={passChip} kpis={kpis} />

            {hasMovers ? (
                <div
                    className="eval-twopane"
                    style={{ display: 'grid', gridTemplateColumns: '1.12fr 1fr', gap: 'var(--spacing-l)', alignItems: 'stretch', marginBottom: 'var(--spacing-l)' }}
                >
                    <MoversCard movers={movers} compareLabel={compareLabel} />
                    <NeedsAttentionCard items={attention} onView={openCasesForScenario} />
                </div>
            ) : (
                <div style={{ marginBottom: 'var(--spacing-l)' }}>
                    <NeedsAttentionCard items={attention} onView={openCasesForScenario} />
                </div>
            )}

            {groupRows.length > 0 && (
                <GroupTable
                    rows={groupRows}
                    activeScenarios={activeScenarios}
                    totalKpi={kpi}
                    totalGoodPct={goodPct}
                    passChip={passChip}
                />
            )}
        </div>
    );
};
