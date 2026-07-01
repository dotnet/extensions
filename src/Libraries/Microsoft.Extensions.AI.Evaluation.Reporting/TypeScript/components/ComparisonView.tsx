// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { useMemo, useEffect } from 'react';
import {
    makeStyles,
    tokens,
    Dropdown,
    Option,
    Text,
} from '@fluentui/react-components';
import { useReportContext } from './ReportContext';
import type { MetricDelta } from './viewModels';

const useStyles = makeStyles({
    root: {
        display: 'flex',
        flexDirection: 'column',
        gap: '1.5rem',
    },
    emptyState: {
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        padding: '3rem 2rem',
        gap: '0.75rem',
        color: tokens.colorNeutralForeground3,
        textAlign: 'center',
        border: `1px dashed ${tokens.colorNeutralStroke2}`,
        borderRadius: tokens.borderRadiusMedium,
    },
    emptyTitle: {
        fontWeight: tokens.fontWeightSemibold,
        fontSize: tokens.fontSizeBase400,
        color: tokens.colorNeutralForeground2,
    },
    selectorRow: {
        display: 'flex',
        alignItems: 'center',
        gap: '0.75rem',
        flexWrap: 'wrap',
    },
    selectorGroup: {
        display: 'flex',
        flexDirection: 'column',
        gap: '0.25rem',
        minWidth: '200px',
        flex: '1 1 200px',
        maxWidth: '340px',
    },
    selectorLabel: {
        display: 'flex',
        alignItems: 'center',
        gap: '0.375rem',
        fontSize: tokens.fontSizeBase200,
        fontWeight: tokens.fontWeightSemibold,
        color: tokens.colorNeutralForeground3,
        textTransform: 'uppercase',
        letterSpacing: '0.5px',
    },
    selectorDot: {
        width: '8px',
        height: '8px',
        borderRadius: '50%',
        flex: 'none',
    },
    selectorArrow: {
        alignSelf: 'flex-end',
        paddingBottom: '0.5rem',
        color: tokens.colorNeutralForeground3,
        fontSize: tokens.fontSizeBase400,
    },
    kpiStrip: {
        display: 'flex',
        flexWrap: 'wrap',
        gap: '0.75rem',
    },
    kpiCard: {
        display: 'flex',
        flexDirection: 'column',
        gap: '0.25rem',
        padding: '0.75rem 1rem',
        border: `1px solid ${tokens.colorNeutralStroke2}`,
        borderRadius: tokens.borderRadiusMedium,
        backgroundColor: tokens.colorNeutralBackground2,
        minWidth: '140px',
        flex: '1 1 140px',
    },
    kpiLabel: {
        fontSize: tokens.fontSizeBase100,
        color: tokens.colorNeutralForeground3,
        fontWeight: tokens.fontWeightSemibold,
        textTransform: 'uppercase',
        letterSpacing: '0.4px',
    },
    kpiValue: {
        fontSize: tokens.fontSizeBase600,
        fontWeight: tokens.fontWeightSemibold,
        lineHeight: '1.15',
    },
    kpiSub: {
        fontSize: tokens.fontSizeBase200,
        color: tokens.colorNeutralForeground3,
    },
    kpiPositive: { color: tokens.colorStatusSuccessForeground1 },
    kpiNegative: { color: tokens.colorStatusDangerForeground1 },
    kpiNeutral: { color: tokens.colorNeutralForeground1 },
    tableSection: {
        display: 'flex',
        flexDirection: 'column',
        gap: '0.5rem',
    },
    tableSectionHeader: {
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        flexWrap: 'wrap',
        gap: '0.5rem',
    },
    tableSectionTitle: {
        fontWeight: tokens.fontWeightSemibold,
        fontSize: tokens.fontSizeBase300,
    },
    legendRow: {
        display: 'flex',
        alignItems: 'center',
        gap: '0.5rem',
        fontSize: tokens.fontSizeBase200,
        color: tokens.colorNeutralForeground3,
    },
    legendSwatch: {
        width: '8px',
        height: '8px',
        borderRadius: '50%',
        flex: 'none',
    },
    deltaTable: {
        width: '100%',
        borderCollapse: 'collapse' as const,
        fontSize: tokens.fontSizeBase200,
    },
    deltaTableHead: {
        borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
    },
    deltaTableHeadCell: {
        fontSize: tokens.fontSizeBase100,
        fontWeight: tokens.fontWeightSemibold,
        color: tokens.colorNeutralForeground3,
        textTransform: 'uppercase' as const,
        letterSpacing: '0.4px',
        padding: '0.25rem 0.5rem',
        textAlign: 'left' as const,
    },
    deltaTableRow: {
        borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
        '&:last-child': { borderBottom: 'none' },
    },
    deltaTableCell: {
        padding: '0.5rem 0.5rem',
        color: tokens.colorNeutralForeground1,
        verticalAlign: 'middle' as const,
    },
    fromTo: {
        display: 'flex',
        alignItems: 'center',
        gap: '0.375rem',
        fontSize: tokens.fontSizeBase200,
        color: tokens.colorNeutralForeground2,
    },
    fromValue: {
        color: tokens.colorNeutralForeground3,
    },
    toValue: {
        fontWeight: tokens.fontWeightSemibold,
        color: tokens.colorNeutralForeground1,
    },
    arrow: {
        color: tokens.colorNeutralForeground3,
        fontSize: tokens.fontSizeBase100,
    },
    sparkBar: {
        display: 'flex',
        alignItems: 'center',
        gap: '0.375rem',
        minWidth: '100px',
    },
    deltaPositive: {
        color: tokens.colorStatusSuccessForeground1,
        fontWeight: tokens.fontWeightSemibold,
    },
    deltaNegative: {
        color: tokens.colorStatusDangerForeground1,
        fontWeight: tokens.fontWeightSemibold,
    },
    deltaNeutral: {
        color: tokens.colorNeutralForeground3,
    },
    metricName: {
        fontWeight: tokens.fontWeightRegular,
        color: tokens.colorNeutralForeground1,
    },
    scenarioName: {
        fontSize: tokens.fontSizeBase100,
        color: tokens.colorNeutralForeground3,
        marginTop: '0.1rem',
    },
});

const formatVal = (v: number): string => (Number.isInteger(v) ? v.toFixed(0) : v.toFixed(2));
const formatDelta = (d: number): string => {
    const s = d > 0 ? '+' : '';
    return `${s}${Number.isInteger(d) ? d.toFixed(0) : d.toFixed(2)}`;
};

const normalise = (delta: number, maxAbs: number) =>
    maxAbs > 0 ? Math.max(-1, Math.min(1, delta / maxAbs)) : 0;

type SparkBarProps = { normalised: number };
const SparkBar = ({ normalised }: SparkBarProps) => {
    const BAR_W = 80;
    const BAR_H = 8;
    const mid = BAR_W / 2;
    const fillW = Math.abs(normalised) * (BAR_W / 2);
    const fillX = normalised >= 0 ? mid : mid - fillW;
    const fillColor =
        normalised > 0 ? '#16a34a' : normalised < 0 ? '#dc2626' : '#94a3b8';

    return (
        <svg width={BAR_W} height={BAR_H} aria-hidden="true" style={{ display: 'block' }}>
            <rect x={0} y={2} width={BAR_W} height={BAR_H - 4} rx={2} fill="currentColor" fillOpacity={0.1} />
            {fillW > 0 && (
                <rect x={fillX} y={2} width={fillW} height={BAR_H - 4} rx={2} fill={fillColor} fillOpacity={0.8} />
            )}
            <rect x={mid - 0.5} y={0} width={1} height={BAR_H} fill="currentColor" fillOpacity={0.25} />
            <circle cx={normalised >= 0 ? mid + fillW : mid} cy={BAR_H / 2} r={3} fill="white" stroke={fillColor} strokeWidth={1.5} />
            <circle cx={normalised >= 0 ? mid : mid - fillW} cy={BAR_H / 2} r={2} fill={fillColor} fillOpacity={0.5} />
        </svg>
    );
};

export const ComparisonView = () => {
    const classes = useStyles();
    const { scoreSummary, cmpA, setCmpA, cmpB, setCmpB } = useReportContext();

    const executions = useMemo(
        () => [...scoreSummary.executionHistory.keys()],
        [scoreSummary],
    );

    useEffect(() => {
        if (executions.length >= 2) {
            if (!cmpA) setCmpA(executions[executions.length - 2]);
            if (!cmpB) setCmpB(executions[executions.length - 1]);
        }
    }, [executions, cmpA, cmpB, setCmpA, setCmpB]);

    const effectiveA = cmpA ?? (executions.length >= 2 ? executions[executions.length - 2] : undefined);
    const effectiveB = cmpB ?? (executions.length >= 1 ? executions[executions.length - 1] : undefined);

    const hasTwoExecs = executions.length >= 2;

    const allDeltas: MetricDelta[] = useMemo(() => {
        if (!hasTwoExecs || !effectiveA || !effectiveB || effectiveA === effectiveB) return [];

        const execARoot = scoreSummary.executionHistory.get(effectiveA);
        if (!execARoot) return [];

        const deltas: MetricDelta[] = [];
        for (const node of execARoot.flattenedNodes) {
            if (!node.isLeafNode || !node.scenario) continue;
            const scenA = node.scenario;

            const bRoot = scoreSummary.executionHistory.get(effectiveB);
            if (!bRoot) continue;
            const bLeaf = bRoot.flattenedNodes.find(
                (n) =>
                    n.isLeafNode &&
                    n.scenario?.scenarioName === scenA.scenarioName &&
                    n.scenario?.iterationName === scenA.iterationName,
            );
            if (!bLeaf?.scenario) continue;
            const scenB = bLeaf.scenario;

            for (const [metricName, metricA] of Object.entries(scenA.evaluationResult?.metrics ?? {})) {
                if (!metricA || metricA.$type !== 'numeric') continue;
                const vA = (metricA as NumericMetric).value;
                if (typeof vA !== 'number') continue;

                const metricB = scenB.evaluationResult?.metrics?.[metricName];
                if (!metricB || metricB.$type !== 'numeric') continue;
                const vB = (metricB as NumericMetric).value;
                if (typeof vB !== 'number') continue;

                deltas.push({
                    scenarioName: scenA.scenarioName,
                    iterationName: scenA.iterationName,
                    metricName,
                    fromExecution: effectiveA,
                    toExecution: effectiveB,
                    fromValue: vA,
                    toValue: vB,
                    delta: vB - vA,
                });
            }
        }
        return deltas;
    }, [scoreSummary, effectiveA, effectiveB, hasTwoExecs]);

    const sortedDeltas = useMemo(
        () => [...allDeltas].sort((a, b) => a.metricName.localeCompare(b.metricName) || a.scenarioName.localeCompare(b.scenarioName)),
        [allDeltas],
    );

    const improvedCount = allDeltas.filter((d) => d.delta > 0).length;
    const regressedCount = allDeltas.filter((d) => d.delta < 0).length;
    const totalCount = allDeltas.length;

    const biggestMover = useMemo(
        () =>
            allDeltas.length > 0
                ? allDeltas.reduce((a, b) => (Math.abs(b.delta) > Math.abs(a.delta) ? b : a))
                : undefined,
        [allDeltas],
    );

    const maxAbsDelta = useMemo(
        () => (allDeltas.length > 0 ? Math.max(...allDeltas.map((d) => Math.abs(d.delta))) : 1),
        [allDeltas],
    );

    if (!hasTwoExecs) {
        return (
            <div className={classes.emptyState}>
                <Text className={classes.emptyTitle}>Needs at least 2 executions</Text>
                <Text size={300} style={{ color: tokens.colorNeutralForeground3 }}>
                    Run the evaluation suite across multiple executions to compare results side by side.
                </Text>
            </div>
        );
    }

    return (
        <div className={classes.root}>
            <div className={classes.selectorRow}>
                <div className={classes.selectorGroup}>
                    <span className={classes.selectorLabel}>
                        <span
                            className={classes.selectorDot}
                            style={{ backgroundColor: '#64748b' }}
                            aria-hidden="true"
                        />
                        Baseline
                    </span>
                    <Dropdown
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

                <span className={classes.selectorArrow} aria-hidden="true">›</span>

                <div className={classes.selectorGroup}>
                    <span className={classes.selectorLabel}>
                        <span
                            className={classes.selectorDot}
                            style={{ backgroundColor: '#2563eb' }}
                            aria-hidden="true"
                        />
                        Current
                    </span>
                    <Dropdown
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

            {effectiveA === effectiveB && (
                <div className={classes.emptyState}>
                    <Text className={classes.emptyTitle}>Select two different executions</Text>
                    <Text size={300} style={{ color: tokens.colorNeutralForeground3 }}>
                        The baseline and current executions are the same. Choose different executions to see the delta.
                    </Text>
                </div>
            )}

            {effectiveA !== effectiveB && allDeltas.length > 0 && (
                <div className={classes.kpiStrip}>
                    <div className={classes.kpiCard}>
                        <span className={classes.kpiLabel}>Metrics improved</span>
                        <span className={`${classes.kpiValue} ${classes.kpiPositive}`}>{improvedCount}</span>
                        <span className={classes.kpiSub}>of {totalCount} metrics</span>
                    </div>
                    <div className={classes.kpiCard}>
                        <span className={classes.kpiLabel}>Metrics regressed</span>
                        <span className={`${classes.kpiValue} ${classes.kpiNegative}`}>{regressedCount}</span>
                        <span className={classes.kpiSub}>of {totalCount} metrics</span>
                    </div>
                    {biggestMover && (
                        <div className={classes.kpiCard}>
                            <span className={classes.kpiLabel}>Biggest mover</span>
                            <span
                                className={`${classes.kpiValue} ${
                                    biggestMover.delta > 0 ? classes.kpiPositive : classes.kpiNegative
                                }`}
                            >
                                {biggestMover.delta > 0 ? '▲' : '▼'} {Math.abs(biggestMover.delta).toFixed(3)}
                            </span>
                            <span className={classes.kpiSub}>{biggestMover.metricName}</span>
                        </div>
                    )}
                </div>
            )}

            {effectiveA !== effectiveB && sortedDeltas.length > 0 && (
                <div className={classes.tableSection}>
                    <div className={classes.tableSectionHeader}>
                        <Text className={classes.tableSectionTitle}>Per-metric change</Text>
                        <div className={classes.legendRow}>
                            <span
                                className={classes.legendSwatch}
                                style={{ backgroundColor: '#dc2626' }}
                                aria-hidden="true"
                            />
                            <span>regressed</span>
                            <span style={{ marginLeft: '0.25rem' }}>|</span>
                            <span
                                className={classes.legendSwatch}
                                style={{ backgroundColor: '#16a34a', marginLeft: '0.25rem' }}
                                aria-hidden="true"
                            />
                            <span>improved</span>
                        </div>
                    </div>

                    <table
                        className={classes.deltaTable}
                        aria-label={`Per-metric changes from ${effectiveA} to ${effectiveB}`}
                    >
                        <thead className={classes.deltaTableHead}>
                            <tr>
                                <th className={classes.deltaTableHeadCell}>Metric ▲</th>
                                <th className={classes.deltaTableHeadCell}>
                                    Baseline → Current
                                </th>
                                <th className={classes.deltaTableHeadCell}>Change</th>
                            </tr>
                        </thead>
                        <tbody>
                            {sortedDeltas.map((d, i) => {
                                const norm = normalise(d.delta, maxAbsDelta);
                                const isPos = d.delta > 0;
                                const isNeg = d.delta < 0;
                                return (
                                    <tr key={i} className={classes.deltaTableRow}>
                                        <td className={classes.deltaTableCell}>
                                            <div className={classes.metricName}>{d.metricName}</div>
                                            <div className={classes.scenarioName}>{d.scenarioName}</div>
                                        </td>
                                        <td className={classes.deltaTableCell}>
                                            <div className={classes.fromTo}>
                                                <span className={classes.fromValue}>{formatVal(d.fromValue)}</span>
                                                <span className={classes.arrow}>→</span>
                                                <span
                                                    className={classes.toValue}
                                                    style={{
                                                        color: isPos
                                                            ? tokens.colorStatusSuccessForeground1
                                                            : isNeg
                                                            ? tokens.colorStatusDangerForeground1
                                                            : tokens.colorNeutralForeground1,
                                                    }}
                                                >
                                                    {formatVal(d.toValue)}
                                                </span>
                                            </div>
                                        </td>
                                        <td className={classes.deltaTableCell}>
                                            <div className={classes.sparkBar}>
                                                <SparkBar normalised={norm} />
                                                <span
                                                    className={
                                                        isPos
                                                            ? classes.deltaPositive
                                                            : isNeg
                                                            ? classes.deltaNegative
                                                            : classes.deltaNeutral
                                                    }
                                                >
                                                    {isPos ? '▲' : isNeg ? '▼' : ''}{' '}
                                                    {formatDelta(d.delta)}
                                                </span>
                                            </div>
                                        </td>
                                    </tr>
                                );
                            })}
                        </tbody>
                    </table>
                </div>
            )}

            {effectiveA !== effectiveB && sortedDeltas.length === 0 && allDeltas.length === 0 && (
                <div className={classes.emptyState}>
                    <Text className={classes.emptyTitle}>No comparable numeric metrics</Text>
                    <Text size={300} style={{ color: tokens.colorNeutralForeground3 }}>
                        The selected executions share no numeric metrics that can be compared.
                    </Text>
                </div>
            )}
        </div>
    );
};
