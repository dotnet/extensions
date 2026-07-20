// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { useId, useState } from 'react';
import { makeStyles, mergeClasses } from '@fluentui/react-components';
import { ChevronRight16Regular } from '@fluentui/react-icons';
import { useReportStyles, statusSolidVar, statusTextVar, type ReportStatus } from '../styles/reportStyles';
import { formatValue } from '../core/metricModel';
import { DiagnosticsContent } from './DiagnosticsContent';
import { MetadataContent } from './MetadataContent';
import { type MetricType } from './metricTypes';

// The 5-segment meter fills to the rating ordinal.
const RATING_PIP: Partial<Record<EvaluationRating, number>> = {
    exceptional: 5,
    good: 4,
    average: 3,
    poor: 2,
    unacceptable: 1,
};

const statusKeyOf = (rating: EvaluationRating | undefined): ReportStatus => {
    switch (rating) {
        case 'exceptional':
        case 'good':
            return 'success';
        case 'average':
            return 'caution';
        case 'poor':
        case 'unacceptable':
            return 'danger';
        default:
            return 'neutral';
    }
};

const ratingWord = (rating: EvaluationRating | undefined): string => {
    switch (rating) {
        case 'exceptional':
            return 'Exceptional';
        case 'good':
            return 'Good';
        case 'average':
            return 'Fair';
        case 'poor':
            return 'Poor';
        case 'unacceptable':
            return 'Weak';
        case 'inconclusive':
            return 'Inconclusive';
        default:
            return 'Unknown';
    }
};

const metricFailed = (metric: MetricType): boolean =>
    metric.interpretation?.failed === true ||
    (metric.diagnostics?.some((d) => d.severity === 'error') ?? false);

const useStyles = makeStyles({
    headerRow: {
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        gap: 'var(--spacing-s)',
        padding: 'var(--spacing-m) var(--spacing-l)',
        borderBottom: '1px solid var(--neutral-stroke-3)',
    },
    headerCount: {
        fontSize: 'var(--font-size-100)',
        color: 'var(--neutral-foreground-4)',
        whiteSpace: 'nowrap',
    },

    rowWrap: { borderTop: '1px solid var(--neutral-stroke-3)' },
    row: {
        appearance: 'none',
        border: 'none',
        margin: 0,
        width: '100%',
        font: 'inherit',
        color: 'inherit',
        textAlign: 'left',
        display: 'flex',
        alignItems: 'center',
        gap: 'var(--spacing-m-nudge)',
        minHeight: '44px',
        padding: '12px var(--spacing-l)',
        lineHeight: '20px',
        cursor: 'pointer',
        userSelect: 'none',
        backgroundColor: 'transparent',
    },
    rowInteractive: {
        transition: 'background-color var(--duration-faster) var(--curve-easy-ease)',
        ':hover': { background: 'var(--subtle-background-hover)' },
        ':focus-visible': {
            boxShadow:
                '0 0 0 2px var(--focus-stroke-inner) inset, ' +
                '0 0 0 4px var(--focus-stroke-outer) inset',
            borderRadius: 'inherit',
            outline: 'none',
        },
    },
    caret: {
        flexShrink: 0,
        color: 'var(--neutral-foreground-3)',
        transition: 'transform var(--duration-fast) var(--curve-easy-ease)',
    },
    caretOpen: { transform: 'rotate(90deg)' },
    dotWrap: { flex: 'none', display: 'inline-flex', alignItems: 'center' },
    dot: {
        width: '8px',
        height: '8px',
        borderRadius: 'var(--radius-circular)',
        flex: 'none',
        boxSizing: 'border-box',
    },
    rowName: {
        flex: '1 1 auto',
        minWidth: 0,
        whiteSpace: 'nowrap',
        overflow: 'hidden',
        textOverflow: 'ellipsis',
        fontSize: 'var(--font-size-300)',
        color: 'var(--neutral-foreground-1)',
    },
    track: {
        flex: 'none',
        display: 'flex',
        alignItems: 'stretch',
        width: '96px',
        height: '16px',
        gap: '4px',
    },
    seg: { flex: '1 1 0', minWidth: 0, borderRadius: '2px' },
    segCenter: { display: 'flex', alignItems: 'center', justifyContent: 'center' },
    segIcon: { fontSize: '11px', lineHeight: 1, fontWeight: 700 },

    panel: {
        padding: '0 var(--spacing-l) var(--spacing-l) var(--spacing-xxxl)',
        display: 'flex',
        flexDirection: 'column',
        containerType: 'inline-size',
    },
    hero: {
        display: 'flex',
        flexDirection: 'column',
        gap: 'var(--spacing-s)',
        padding: 'var(--spacing-m) 0 var(--spacing-l)',
    },
    heroLine: { display: 'flex', alignItems: 'baseline', gap: 'var(--spacing-s-nudge)' },
    heroWord: {
        fontSize: 'var(--font-size-500)',
        fontWeight: 'var(--font-weight-semibold)',
        lineHeight: 1.2,
    },
    heroNum: {
        fontSize: 'var(--font-size-400)',
        color: 'var(--neutral-foreground-3)',
        fontVariantNumeric: 'tabular-nums',
    },
    heroIcon: { fontSize: '28px', lineHeight: 1, fontWeight: 700 },
    heroTrack: {
        height: '6px',
        width: '100%',
        borderRadius: 'var(--radius-circular)',
        backgroundColor: 'var(--eval-seg-empty)',
        overflow: 'hidden',
    },
    heroFill: { height: '100%', borderRadius: 'var(--radius-circular)' },

    subSection: {
        borderTop: '1px solid var(--neutral-stroke-2)',
        padding: 'var(--spacing-l) 0',
    },
    subHeader: {
        fontSize: 'var(--font-size-200)',
        fontWeight: 'var(--font-weight-semibold)',
        color: 'var(--neutral-foreground-2)',
        marginBottom: 'var(--spacing-s)',
    },
    subBody: {
        fontSize: 'var(--font-size-300)',
        lineHeight: 1.55,
        whiteSpace: 'pre-wrap',
        wordBreak: 'break-word',
    },
    empty: {
        fontSize: 'var(--font-size-200)',
        color: 'var(--neutral-foreground-3)',
        fontStyle: 'italic',
        padding: 'var(--spacing-l)',
    },
});

const SegmentTrack = ({ metric, sk }: { metric: MetricType; sk: ReportStatus }) => {
    const classes = useStyles();
    const solid = statusSolidVar(sk);
    const aura = `0 0 0 3px color-mix(in srgb, ${solid} 18%, transparent)`;

    const neutralTrack = (
        <span className={classes.track} aria-hidden="true">
            <span className={mergeClasses(classes.seg, classes.segCenter)} style={{ backgroundColor: 'var(--eval-seg-empty)' }}>
                <span className={classes.segIcon} style={{ color: 'var(--neutral-foreground-3)' }}>?</span>
            </span>
        </span>
    );

    if (sk === 'neutral') {
        return neutralTrack;
    }

    if (metric.$type === 'boolean') {
        const good = sk === 'success';
        return (
            <span className={classes.track} aria-hidden="true">
                <span className={mergeClasses(classes.seg, classes.segCenter)} style={{ backgroundColor: solid, boxShadow: aura }}>
                    <span className={classes.segIcon} style={{ color: 'var(--neutral-foreground-on-brand)' }}>{good ? '✓' : '✗'}</span>
                </span>
            </span>
        );
    }

    // string/none metrics carry no scale to plot — no meter, matching real-data rendering.
    if (metric.$type !== 'numeric') {
        return null;
    }

    const pip = RATING_PIP[metric.interpretation?.rating as EvaluationRating];
    if (pip === undefined) {
        return neutralTrack;
    }

    return (
        <span className={classes.track} aria-hidden="true">
            {Array.from({ length: 5 }, (_, i) => {
                const on = i < pip;
                return (
                    <span
                        key={i}
                        className={classes.seg}
                        style={{ backgroundColor: on ? solid : 'var(--eval-seg-empty)', boxShadow: on ? aura : undefined }}
                    />
                );
            })}
        </span>
    );
};

const MetricRow = ({ metric }: { metric: MetricType }) => {
    const classes = useStyles();
    const [open, setOpen] = useState(false);
    const panelId = useId();

    const rating = metric.interpretation?.rating;
    const failed = metricFailed(metric);
    const sk: ReportStatus = failed ? 'danger' : statusKeyOf(rating);
    const solid = statusSolidVar(sk);
    const textColor = statusTextVar(sk);

    const dotStyle =
        sk === 'neutral'
            ? { backgroundColor: 'transparent', boxShadow: 'inset 0 0 0 1.5px var(--neutral-foreground-4)' }
            : { backgroundColor: solid, boxShadow: `0 0 0 3px color-mix(in srgb, ${solid} 18%, transparent)` };

    const reason = metric.reason;
    const interpretationReason = metric.interpretation?.reason;
    const diagnostics = metric.diagnostics ?? [];
    const metadata = metric.metadata ?? {};
    const hasMetadata = Object.keys(metadata).length > 0;

    const heroNum = formatValue(metric);
    const pip = metric.$type === 'numeric' ? RATING_PIP[rating as EvaluationRating] : undefined;
    const heroFillW = pip !== undefined ? `${(pip / 5) * 100}%` : '100%';
    const showHeroBar = pip !== undefined;

    return (
        <div className={classes.rowWrap}>
            <button
                type="button"
                className={mergeClasses(classes.row, classes.rowInteractive)}
                aria-expanded={open}
                aria-controls={open ? panelId : undefined}
                aria-label={`${metric.name}${failed ? ', failed' : ''}, ${ratingWord(rating)}${heroNum !== undefined ? `, ${heroNum}` : ''}`}
                onClick={() => setOpen((v) => !v)}
            >
                <ChevronRight16Regular className={mergeClasses(classes.caret, open && classes.caretOpen)} />
                <span className={classes.dotWrap} aria-hidden="true">
                    <span className={classes.dot} style={dotStyle} />
                </span>
                <span className={classes.rowName}>{metric.name}</span>
                <SegmentTrack metric={metric} sk={sk} />
            </button>

            {open && (
                <div id={panelId} role="region" aria-label={`${metric.name} detail`} className={classes.panel}>
                    <div className={classes.hero}>
                        <div className={classes.heroLine}>
                            <span className={classes.heroWord} style={{ color: textColor }}>{ratingWord(rating)}</span>
                            {heroNum !== undefined && <span className={classes.heroNum}>{heroNum}</span>}
                        </div>
                        {showHeroBar && (
                            <div className={classes.heroTrack}>
                                <div className={classes.heroFill} style={{ width: heroFillW, backgroundColor: solid }} />
                            </div>
                        )}
                    </div>

                    {(reason != null || interpretationReason != null) && (
                        <div className={classes.subSection}>
                            <div className={classes.subHeader}>{failed ? 'Why this failed?' : 'Why this score?'}</div>
                            <div className={classes.subBody} style={{ color: 'var(--neutral-foreground-2)' }}>
                                {interpretationReason ?? reason}
                            </div>
                        </div>
                    )}

                    {reason != null && interpretationReason != null && (
                        <div className={classes.subSection}>
                            <div className={classes.subHeader}>What this measures?</div>
                            <div className={classes.subBody} style={{ color: 'var(--neutral-foreground-3)' }}>{reason}</div>
                        </div>
                    )}

                    {diagnostics.length > 0 && (
                        <div className={classes.subSection}>
                            <div className={classes.subHeader}>Diagnostics</div>
                            <DiagnosticsContent diagnostics={diagnostics} metricName={metric.name} />
                        </div>
                    )}

                    {hasMetadata && (
                        <div className={classes.subSection}>
                            <div className={classes.subHeader}>Metadata</div>
                            <MetadataContent metadata={metadata} />
                        </div>
                    )}
                </div>
            )}
        </div>
    );
};

export const MetricPanel = ({ scenario }: { scenario: ScenarioRunResult }) => {
    const classes = useStyles();
    const s = useReportStyles();
    const metrics = Object.values(scenario.evaluationResult?.metrics ?? {});

    return (
        <div className={s.cardNested}>
            <div className={classes.headerRow}>
                <h2 className={s.eyebrow} style={{ margin: 0 }}>Metrics</h2>
                {metrics.length > 0 && (
                    <span className={classes.headerCount}>
                        {metrics.length} {metrics.length === 1 ? 'metric' : 'metrics'} · tap to open
                    </span>
                )}
            </div>
            {metrics.length === 0 ? (
                <div className={classes.empty}>No metrics for this case.</div>
            ) : (
                metrics.map((metric, index) => <MetricRow key={`${metric.name}-${index}`} metric={metric} />)
            )}
        </div>
    );
};
