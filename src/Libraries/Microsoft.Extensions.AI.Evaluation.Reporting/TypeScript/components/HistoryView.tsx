// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { useMemo, useState } from 'react';
import {
    makeStyles,
    tokens,
    Tab,
    TabList,
    Text,
    Badge,
    type SelectTabEventHandler,
} from '@fluentui/react-components';
import { useReportContext } from './ReportContext';
import { metricHistoryForScenario } from './viewModels';
import { TrendChart } from './TrendChart';

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
    metricTabsLabel: {
        fontSize: tokens.fontSizeBase200,
        fontWeight: tokens.fontWeightSemibold,
        color: tokens.colorNeutralForeground3,
        textTransform: 'uppercase',
        letterSpacing: '0.5px',
        marginBottom: '0.25rem',
    },
    chartCard: {
        border: `1px solid ${tokens.colorNeutralStroke2}`,
        borderRadius: tokens.borderRadiusMedium,
        padding: '1.25rem 1.25rem 1rem',
        backgroundColor: tokens.colorNeutralBackground2,
        display: 'flex',
        flexDirection: 'column',
        gap: '1rem',
    },
    chartCardHeader: {
        display: 'flex',
        alignItems: 'baseline',
        gap: '0.5rem',
        flexWrap: 'wrap',
    },
    chartCardTitle: {
        fontWeight: tokens.fontWeightSemibold,
        fontSize: tokens.fontSizeBase400,
    },
    chartCardSubtitle: {
        fontSize: tokens.fontSizeBase200,
        color: tokens.colorNeutralForeground3,
    },
    statsRow: {
        display: 'flex',
        flexWrap: 'wrap',
        gap: '0.75rem',
    },
    statCard: {
        display: 'flex',
        flexDirection: 'column',
        gap: '0.125rem',
        padding: '0.625rem 0.875rem',
        border: `1px solid ${tokens.colorNeutralStroke2}`,
        borderRadius: tokens.borderRadiusMedium,
        backgroundColor: tokens.colorNeutralBackground1,
        minWidth: '110px',
    },
    statLabel: {
        fontSize: tokens.fontSizeBase100,
        color: tokens.colorNeutralForeground3,
        fontWeight: tokens.fontWeightSemibold,
        textTransform: 'uppercase',
        letterSpacing: '0.4px',
    },
    statValue: {
        fontSize: tokens.fontSizeBase500,
        fontWeight: tokens.fontWeightSemibold,
        lineHeight: '1.2',
    },
    statValuePositive: {
        color: tokens.colorStatusSuccessForeground1,
    },
    statValueNegative: {
        color: tokens.colorStatusDangerForeground1,
    },
    statValueNeutral: {
        color: tokens.colorNeutralForeground1,
    },
    runHistorySection: {
        display: 'flex',
        flexDirection: 'column',
        gap: '0.5rem',
    },
    runHistoryTitle: {
        fontWeight: tokens.fontWeightSemibold,
        fontSize: tokens.fontSizeBase300,
    },
    runTable: {
        width: '100%',
        borderCollapse: 'collapse' as const,
        fontSize: tokens.fontSizeBase200,
    },
    runTableHead: {
        borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
    },
    runTableHeadCell: {
        fontSize: tokens.fontSizeBase100,
        fontWeight: tokens.fontWeightSemibold,
        color: tokens.colorNeutralForeground3,
        textTransform: 'uppercase' as const,
        letterSpacing: '0.4px',
        padding: '0.25rem 0.5rem',
        textAlign: 'left' as const,
    },
    runTableHeadCellRight: {
        textAlign: 'right' as const,
    },
    runTableRow: {
        borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
    },
    runTableCell: {
        padding: '0.5rem 0.5rem',
        color: tokens.colorNeutralForeground2,
    },
    runTableCellRight: {
        textAlign: 'right' as const,
        padding: '0.5rem 0.5rem',
    },
    deltaPositive: {
        color: tokens.colorStatusSuccessForeground1,
        fontWeight: tokens.fontWeightSemibold,
    },
    deltaNegative: {
        color: tokens.colorStatusDangerForeground1,
        fontWeight: tokens.fontWeightSemibold,
    },
    deltaBaseline: {
        color: tokens.colorNeutralForeground3,
        fontStyle: 'italic',
    },
    scenarioPickerLabel: {
        fontSize: tokens.fontSizeBase200,
        fontWeight: tokens.fontWeightSemibold,
        color: tokens.colorNeutralForeground3,
        textTransform: 'uppercase',
        letterSpacing: '0.5px',
    },
    scenarioPicker: {
        display: 'flex',
        flexWrap: 'wrap',
        gap: '0.375rem',
    },
    scenarioChip: {
        padding: '0.2rem 0.625rem',
        borderRadius: '9999px',
        border: `1px solid ${tokens.colorNeutralStroke2}`,
        cursor: 'pointer',
        fontSize: tokens.fontSizeBase200,
        background: 'transparent',
        color: tokens.colorNeutralForeground2,
        fontFamily: 'inherit',
        '&:hover': {
            backgroundColor: tokens.colorSubtleBackgroundHover,
        },
    },
    scenarioChipSelected: {
        backgroundColor: tokens.colorBrandBackground,
        color: tokens.colorNeutralForegroundOnBrand,
        border: `1px solid ${tokens.colorBrandBackground}`,
        '&:hover': {
            backgroundColor: tokens.colorBrandBackgroundHover,
        },
    },
});

const formatValue = (v: number): string =>
    Number.isInteger(v) ? v.toFixed(0) : v.toFixed(1);

const formatDelta = (d: number): string => {
    const sign = d > 0 ? '+' : '';
    return `${sign}${Number.isInteger(d) ? d.toFixed(0) : d.toFixed(2)}`;
};

type StatCardProps = {
    label: string;
    value: string;
    sentiment?: 'positive' | 'negative' | 'neutral';
};

const StatCard = ({ label, value, sentiment = 'neutral' }: StatCardProps) => {
    const classes = useStyles();
    const valueClass =
        sentiment === 'positive'
            ? classes.statValuePositive
            : sentiment === 'negative'
            ? classes.statValueNegative
            : classes.statValueNeutral;

    return (
        <div className={classes.statCard}>
            <span className={classes.statLabel}>{label}</span>
            <span className={`${classes.statValue} ${valueClass}`}>{value}</span>
        </div>
    );
};

export const HistoryView = () => {
    const classes = useStyles();
    const { scoreSummary, dataset } = useReportContext();

    const leafScenarios = useMemo(() => {
        const primaryRoot = [...scoreSummary.executionHistory.values()][0];
        return primaryRoot
            ? primaryRoot.flattenedNodes
                  .filter((n) => n.isLeafNode && n.scenario != null)
                  .map((n) => n.scenario!)
            : [];
    }, [scoreSummary]);

    const [selectedScenarioKey, setSelectedScenarioKey] = useState<string | undefined>(undefined);

    const selectedScenario = useMemo(() => {
        if (!selectedScenarioKey) return leafScenarios[0] ?? undefined;
        return (
            leafScenarios.find(
                (s) =>
                    `${s.scenarioName}::${s.iterationName}::${s.executionName}` ===
                    selectedScenarioKey,
            ) ?? leafScenarios[0] ?? undefined
        );
    }, [leafScenarios, selectedScenarioKey]);

    const allSeries = useMemo(
        () => (selectedScenario ? metricHistoryForScenario(scoreSummary, selectedScenario) : []),
        [scoreSummary, selectedScenario],
    );

    const [selectedMetric, setSelectedMetric] = useState<string | undefined>(undefined);
    const activeMetric = selectedMetric ?? allSeries[0]?.metricName;

    const onTabSelect: SelectTabEventHandler = (_ev, data) => {
        setSelectedMetric(data.value as string);
    };

    const hasTrend = allSeries.length > 0;

    const scenarioPicker = useMemo(() => {
        const seen = new Map<string, { key: string; label: string }>();
        for (const s of leafScenarios) {
            const k = `${s.scenarioName}::${s.iterationName}::${s.executionName}`;
            const label =
                s.iterationName && s.iterationName !== 'default'
                    ? `${s.scenarioName} · ${s.iterationName}`
                    : s.scenarioName;
            if (!seen.has(k)) seen.set(k, { key: k, label });
        }
        return [...seen.values()];
    }, [leafScenarios]);

    if (leafScenarios.length === 0) {
        return (
            <div className={classes.emptyState}>
                <Text className={classes.emptyTitle}>No scenario data</Text>
                <Text size={300} style={{ color: tokens.colorNeutralForeground3 }}>
                    No scenarios are available in this report.
                </Text>
            </div>
        );
    }

    const chartSeries = hasTrend
        ? allSeries.filter((s) => s.metricName === activeMetric)
        : [];

    const activeSeriesPoints = hasTrend
        ? allSeries.find((s) => s.metricName === activeMetric)?.points ?? []
        : [];

    const spreadByExec = useMemo(() => {
        if (!hasTrend || !activeMetric || activeSeriesPoints.length === 0) return new Map<string, { min: number; max: number }>();
        const map = new Map<string, { min: number; max: number }>();
        for (const r of dataset.scenarioRunResults ?? []) {
            const m = r.evaluationResult?.metrics?.[activeMetric];
            if (!m || m.$type !== 'numeric' || typeof (m as NumericMetric).value !== 'number') continue;
            const v = (m as NumericMetric).value!;
            const existing = map.get(r.executionName);
            if (existing) {
                if (v < existing.min) existing.min = v;
                if (v > existing.max) existing.max = v;
            } else {
                map.set(r.executionName, { min: v, max: v });
            }
        }
        return map;
    }, [dataset, activeMetric, activeSeriesPoints, hasTrend]);

    const firstPoint = activeSeriesPoints[0];
    const lastPoint = activeSeriesPoints[activeSeriesPoints.length - 1];
    const netDelta = hasTrend && firstPoint && lastPoint ? lastPoint.value - firstPoint.value : 0;
    const peakValue = hasTrend
        ? Math.max(...activeSeriesPoints.map((p) => p.value))
        : undefined;

    const scaleMax = peakValue !== undefined ? (peakValue <= 5 ? 5 : peakValue <= 10 ? 10 : undefined) : undefined;
    const scaleLabel = scaleMax != null ? `SCORE · 1–${scaleMax}` : 'METRIC VALUE';

    const metricNames = allSeries.map((s) => s.metricName);

    return (
        <div className={classes.root}>
            {scenarioPicker.length > 1 && (
                <div style={{ display: 'flex', flexDirection: 'column', gap: '0.375rem' }}>
                    <span className={classes.scenarioPickerLabel}>Scenario</span>
                    <div className={classes.scenarioPicker} role="listbox" aria-label="Select scenario">
                        {scenarioPicker.map(({ key, label }) => {
                            const isActive =
                                key ===
                                (selectedScenarioKey ??
                                    (leafScenarios[0]
                                        ? `${leafScenarios[0].scenarioName}::${leafScenarios[0].iterationName}::${leafScenarios[0].executionName}`
                                        : undefined));
                            return (
                                <button
                                    key={key}
                                    role="option"
                                    aria-selected={isActive}
                                    className={`${classes.scenarioChip} ${isActive ? classes.scenarioChipSelected : ''}`}
                                    onClick={() => {
                                        setSelectedScenarioKey(key);
                                        setSelectedMetric(undefined);
                                    }}
                                    title={label}
                                >
                                    {label}
                                </button>
                            );
                        })}
                    </div>
                </div>
            )}

            {!hasTrend && (
                <div className={classes.emptyState}>
                    <Text className={classes.emptyTitle}>Needs at least 2 executions</Text>
                    <Text size={300} style={{ color: tokens.colorNeutralForeground3 }}>
                        Run this scenario across multiple executions to see metric trends over time.
                    </Text>
                </div>
            )}

            {hasTrend && metricNames.length > 0 && (
                <div style={{ display: 'flex', flexDirection: 'column', gap: '0.25rem' }}>
                    <span className={classes.metricTabsLabel}>Metric</span>
                    <TabList
                        selectedValue={activeMetric}
                        onTabSelect={onTabSelect}
                        size="medium"
                        appearance="subtle"
                    >
                        {metricNames.map((name) => (
                            <Tab key={name} value={name}>{name}</Tab>
                        ))}
                    </TabList>
                </div>
            )}

            {hasTrend && activeMetric && chartSeries.length > 0 && (
                <div className={classes.chartCard}>
                    <div className={classes.chartCardHeader}>
                        <Text className={classes.chartCardTitle}>{activeMetric}</Text>
                        <Text className={classes.chartCardSubtitle}>{scaleLabel}</Text>
                    </div>

                    {firstPoint && lastPoint && (
                        <div className={classes.statsRow}>
                            <StatCard
                                label="First run score"
                                value={scaleMax != null ? `${formatValue(firstPoint.value)}/${scaleMax}` : formatValue(firstPoint.value)}
                                sentiment="neutral"
                            />
                            <StatCard
                                label="Last run score"
                                value={scaleMax != null ? `${formatValue(lastPoint.value)}/${scaleMax}` : formatValue(lastPoint.value)}
                                sentiment="neutral"
                            />
                            <StatCard
                                label="Net change"
                                value={formatDelta(netDelta)}
                                sentiment={netDelta > 0 ? 'positive' : netDelta < 0 ? 'negative' : 'neutral'}
                            />
                            {peakValue !== undefined && (
                                <StatCard
                                    label="Peak"
                                    value={scaleMax != null ? `${formatValue(peakValue)}/${scaleMax}` : formatValue(peakValue)}
                                    sentiment="neutral"
                                />
                            )}
                        </div>
                    )}

                    <TrendChart
                        series={chartSeries}
                        ariaLabel={`${activeMetric} score trend across executions${selectedScenario ? ` for ${selectedScenario.scenarioName}` : ''}`}
                        showLegend={true}
                    />

                    {activeSeriesPoints.length > 0 && (
                        <div className={classes.runHistorySection}>
                            <Text className={classes.runHistoryTitle}>Run history</Text>
                            <table className={classes.runTable} aria-label={`Run history for ${activeMetric}`}>
                                <thead className={classes.runTableHead}>
                                    <tr>
                                        <th className={classes.runTableHeadCell}>Execution</th>
                                        <th className={classes.runTableHeadCell}>Metric score</th>
                                        <th className={classes.runTableHeadCell}>Spread</th>
                                        <th className={`${classes.runTableHeadCell} ${classes.runTableHeadCellRight}`}>
                                            Change vs previous
                                        </th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {activeSeriesPoints.map((pt, i) => {
                                        const prev = i > 0 ? activeSeriesPoints[i - 1] : undefined;
                                        const delta = prev != null ? pt.value - prev.value : undefined;
                                        const spread = spreadByExec.get(pt.executionName);
                                        return (
                                            <tr key={pt.executionName} className={classes.runTableRow}>
                                                <td className={classes.runTableCell}>{pt.executionName}</td>
                                                <td className={classes.runTableCell}>
                                                    {scaleMax != null
                                                        ? `${formatValue(pt.value)}/${scaleMax}`
                                                        : formatValue(pt.value)}
                                                </td>
                                                <td className={classes.runTableCell}>
                                                    {spread && spread.min !== spread.max
                                                        ? `${formatValue(spread.min)}–${formatValue(spread.max)}${scaleMax != null ? `/${scaleMax}` : ''}`
                                                        : '—'}
                                                </td>
                                                <td className={classes.runTableCellRight}>
                                                    {delta == null ? (
                                                        <span className={classes.deltaBaseline}>baseline</span>
                                                    ) : delta === 0 ? (
                                                        <span className={classes.deltaBaseline}>—</span>
                                                    ) : (
                                                        <span
                                                            className={
                                                                delta > 0
                                                                    ? classes.deltaPositive
                                                                    : classes.deltaNegative
                                                            }
                                                        >
                                                            {delta > 0 ? '▲' : '▼'} {Math.abs(delta).toFixed(1)}
                                                        </span>
                                                    )}
                                                </td>
                                            </tr>
                                        );
                                    })}
                                </tbody>
                            </table>
                        </div>
                    )}
                </div>
            )}

            {hasTrend && allSeries.length > 1 && (
                <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
                    <Badge appearance="tint" color="informative" size="small">All metrics overview</Badge>
                    <TrendChart
                        series={allSeries}
                        ariaLabel={`All metric trends across executions${selectedScenario ? ` for ${selectedScenario.scenarioName}` : ''}`}
                        showLegend={true}
                    />
                </div>
            )}
        </div>
    );
};
