// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { makeStyles } from '@fluentui/react-components';

export const useReportStyles = makeStyles({
    card: {
        backgroundColor: 'var(--neutral-background-1)',
        border: '1px solid var(--neutral-stroke-1)',
        borderRadius: 'var(--radius-card)',
        overflow: 'hidden',
    },
    cardNested: {
        backgroundColor: 'var(--neutral-background-2)',
        border: '1px solid var(--neutral-stroke-1)',
        borderRadius: 'var(--radius-large)',
        overflow: 'hidden',
    },
    cardPad: {
        padding: 'var(--spacing-l) var(--spacing-xl)',
    },

    sectionHeader: {
        display: 'flex',
        alignItems: 'center',
        gap: 'var(--spacing-m-nudge)',
        padding: 'var(--spacing-l) var(--spacing-xl) var(--spacing-m)',
        borderBottom: '1px solid var(--neutral-stroke-2)',
    },
    sectionHeaderTitle: {
        margin: 0,
        fontSize: 'var(--font-size-400)',
        lineHeight: 'var(--line-height-400)',
        fontWeight: 'var(--font-weight-semibold)',
        color: 'var(--neutral-foreground-1)',
    },
    sectionHeaderSub: {
        fontSize: 'var(--font-size-200)',
        color: 'var(--neutral-foreground-4)',
        whiteSpace: 'nowrap',
    },
    eyebrow: {
        fontSize: 'var(--font-size-100)',
        fontWeight: 'var(--font-weight-semibold)',
        color: 'var(--neutral-foreground-3)',
        textTransform: 'uppercase',
        letterSpacing: '0.4px',
    },

    statusDot: {
        width: '8px',
        height: '8px',
        borderRadius: 'var(--radius-circular)',
        flex: 'none',
        display: 'inline-block',
        backgroundColor: 'var(--neutral-foreground-4)',
    },
    fillSuccess: { backgroundColor: 'var(--status-success-background-3)' },
    fillCaution: { backgroundColor: 'var(--status-warning-background-3)' },
    fillDanger: { backgroundColor: 'var(--status-danger-background-3)' },
    fillWarning: { backgroundColor: 'var(--palette-orange-background3)' },
    fillNeutral: { backgroundColor: 'var(--neutral-foreground-4)' },
    textSuccess: { color: 'var(--status-success-foreground-1)' },
    textCaution: { color: 'var(--status-warning-foreground-1)' },
    textDanger: { color: 'var(--status-danger-foreground-1)' },
    textWarning: { color: 'var(--status-warning-foreground-1)' },

    passRateBar: {
        display: 'flex',
        flexDirection: 'column',
        gap: 'var(--spacing-s-nudge)',
        minWidth: 0,
    },
    passRateValue: {
        textAlign: 'right',
        fontSize: 'var(--font-size-200)',
        fontWeight: 'var(--font-weight-semibold)',
        color: 'var(--neutral-foreground-1)',
        fontVariantNumeric: 'tabular-nums',
        lineHeight: 1,
    },
    deltaPill: {
        display: 'inline-flex',
        alignItems: 'center',
        gap: 'var(--spacing-xxs)',
        height: '20px',
        padding: '0 var(--spacing-s)',
        borderRadius: 'var(--radius-circular)',
        fontSize: 'var(--font-size-200)',
        fontWeight: 'var(--font-weight-semibold)',
        lineHeight: 1,
        whiteSpace: 'nowrap',
        fontVariantNumeric: 'tabular-nums',
        border: '1px solid transparent',
    },
    deltaUp: {
        backgroundColor: 'var(--status-success-background-1)',
        color: 'var(--status-success-foreground-1)',
    },
    deltaDown: {
        backgroundColor: 'var(--status-danger-background-1)',
        color: 'var(--status-danger-foreground-1)',
    },
    deltaFlat: {
        backgroundColor: 'var(--status-info-background)',
        color: 'var(--status-info-foreground)',
    },

    countPill: {
        display: 'inline-flex',
        alignItems: 'center',
        justifyContent: 'center',
        minWidth: '16px',
        height: '16px',
        padding: '0 var(--spacing-s-nudge)',
        borderRadius: 'var(--radius-circular)',
        fontSize: 'var(--font-size-100)',
        fontWeight: 'var(--font-weight-semibold)',
        lineHeight: 1,
        fontVariantNumeric: 'tabular-nums',
        whiteSpace: 'nowrap',
    },
    countSuccess: {
        backgroundColor: 'var(--status-success-background-1)',
        color: 'var(--status-success-foreground-1)',
    },
    countDanger: {
        backgroundColor: 'var(--status-danger-background-1)',
        color: 'var(--status-danger-foreground-1)',
    },
    countBrand: {
        backgroundColor: 'var(--brand-background-2)',
        color: 'var(--brand-foreground-1)',
    },
    countNeutral: {
        backgroundColor: 'var(--neutral-background-3)',
        color: 'var(--neutral-foreground-2)',
    },
    badge: {
        display: 'inline-flex',
        alignItems: 'center',
        height: '20px',
        padding: '0 var(--spacing-s)',
        borderRadius: 'var(--radius-circular)',
        backgroundColor: 'var(--neutral-background-3)',
        color: 'var(--neutral-foreground-2)',
        fontSize: 'var(--font-size-200)',
        fontWeight: 'var(--font-weight-semibold)',
        letterSpacing: '0.2px',
        whiteSpace: 'nowrap',
        flex: 'none',
    },

    tableHeader: {
        display: 'grid',
        alignItems: 'center',
        padding: 'var(--spacing-m-nudge) var(--spacing-xl)',
        fontSize: 'var(--font-size-100)',
        fontWeight: 'var(--font-weight-semibold)',
        color: 'var(--neutral-foreground-4)',
        textTransform: 'uppercase',
        letterSpacing: '0.5px',
        borderBottom: '1px solid var(--neutral-stroke-2)',
    },
    tableRow: {
        display: 'grid',
        alignItems: 'center',
        padding: 'var(--spacing-m) var(--spacing-xl)',
        fontSize: 'var(--font-size-300)',
        color: 'var(--neutral-foreground-1)',
        borderBottom: '1px solid var(--neutral-stroke-3)',
    },
    tableNum: {
        textAlign: 'right',
        fontVariantNumeric: 'tabular-nums',
        color: 'var(--neutral-foreground-1)',
    },

    sidebarItem: {
        display: 'flex',
        alignItems: 'center',
        gap: 'var(--spacing-m)',
        width: '100%',
        boxSizing: 'border-box',
        border: 'none',
        cursor: 'pointer',
        backgroundColor: 'transparent',
        color: 'var(--neutral-foreground-1)',
        fontFamily: 'inherit',
        textAlign: 'left',
        borderRadius: 'var(--radius-medium)',
        minHeight: '32px',
        padding: 'var(--spacing-s-nudge) var(--spacing-m-nudge)',
    },
    sidebarItemLabel: {
        flex: '1 1 auto',
        minWidth: 0,
        fontSize: 'var(--font-size-300)',
        whiteSpace: 'nowrap',
        overflow: 'hidden',
        textOverflow: 'ellipsis',
    },
    sidebarCaret: {
        width: '16px',
        flex: 'none',
        display: 'inline-flex',
        justifyContent: 'center',
        alignItems: 'center',
        color: 'var(--neutral-foreground-3)',
        transition: 'transform var(--duration-fast) var(--curve-easy-ease)',
    },
    sidebarSectionLabel: {
        fontSize: 'var(--font-size-200)',
        fontWeight: 'var(--font-weight-semibold)',
        color: 'var(--neutral-foreground-3)',
        textTransform: 'uppercase',
        letterSpacing: '0.5px',
    },

    segmentedTrack: {
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
        border: '1px solid var(--neutral-stroke-3)',
        borderRadius: 'var(--radius-large)',
        position: 'relative',
    },
    segmentedPill: {
        position: 'relative',
        zIndex: 1,
        flex: 'none',
        border: 'none',
        background: 'transparent',
        color: 'var(--neutral-foreground-2)',
        fontWeight: 'var(--font-weight-regular)',
        fontFamily: 'inherit',
        fontSize: 'var(--font-size-300)',
        lineHeight: 1,
        padding: 'var(--spacing-s) var(--spacing-l)',
        borderRadius: 'var(--radius-medium)',
        cursor: 'pointer',
        whiteSpace: 'nowrap',
        transition: 'color var(--duration-faster) var(--curve-easy-ease)',
    },
    segmentedPillActive: {
        color: 'var(--neutral-foreground-1)',
        fontWeight: 'var(--font-weight-semibold)',
    },
    slideIndicatorPill: {
        position: 'absolute',
        top: 0,
        left: 0,
        zIndex: 0,
        backgroundColor: 'var(--neutral-background-1)',
        boxShadow: 'var(--shadow-2)',
        borderRadius: 'var(--radius-medium)',
        opacity: 0,
        transition:
            'transform var(--duration-normal) var(--curve-decelerate-max), ' +
            'width var(--duration-normal) var(--curve-decelerate-max), ' +
            'height var(--duration-normal) var(--curve-decelerate-max)',
    },

    tab: {
        position: 'relative',
        display: 'inline-flex',
        alignItems: 'center',
        gap: 'var(--spacing-s)',
        background: 'none',
        border: 'none',
        cursor: 'pointer',
        fontFamily: 'inherit',
        fontSize: 'var(--font-size-300)',
        padding: 'var(--spacing-m) var(--spacing-s)',
        color: 'var(--neutral-foreground-2)',
    },
    slideIndicatorUnderline: {
        position: 'absolute',
        zIndex: 1,
        left: 0,
        bottom: 0,
        height: '2px',
        borderRadius: 'var(--radius-circular)',
        backgroundColor: 'var(--compound-brand-background)',
        opacity: 0,
        transition:
            'transform var(--duration-normal) var(--curve-decelerate-max), ' +
            'width var(--duration-normal) var(--curve-decelerate-max)',
    },

    viewLink: {
        appearance: 'none',
        border: 'none',
        cursor: 'pointer',
        fontFamily: 'var(--font-family-base)',
        fontSize: 'var(--font-size-200)',
        fontWeight: 'var(--font-weight-semibold)',
        lineHeight: 'var(--line-height-200)',
        color: 'var(--brand-foreground-1)',
        backgroundColor: 'transparent',
        padding: 'var(--spacing-xs) var(--spacing-s)',
        borderRadius: 'var(--radius-medium)',
        transition: 'background-color var(--duration-faster) var(--curve-easy-ease)',
        '&:hover': {
            backgroundColor: 'var(--brand-background-2)',
            textDecoration: 'underline',
        },
        '&:active': { backgroundColor: 'var(--brand-background-2-hover)' },
    },

    // Wraps a wide grid/table so it scrolls horizontally instead of overflowing on
    // narrow viewports. Shared by History, Overview, and Comparison run tables.
    tscroll: {
        '@media (max-width: 720px)': {
            overflowX: 'auto',
            overflowY: 'hidden',
            WebkitOverflowScrolling: 'touch',
        },
    },
});

export type ReportStatus = 'success' | 'caution' | 'warning' | 'danger' | 'neutral';

export const pickFill = (
    s: ReturnType<typeof useReportStyles>,
    status: ReportStatus,
): string =>
    status === 'success' ? s.fillSuccess
        : status === 'caution' ? s.fillCaution
            : status === 'warning' ? s.fillWarning
                : status === 'danger' ? s.fillDanger
                    : s.fillNeutral;

export const pickStatusText = (
    s: ReturnType<typeof useReportStyles>,
    status: ReportStatus,
): string | undefined =>
    status === 'success' ? s.textSuccess
        : status === 'caution' ? s.textCaution
            : status === 'warning' ? s.textWarning
                : status === 'danger' ? s.textDanger
                    : undefined;

export const statusSolidVar = (status: ReportStatus): string =>
    status === 'success' ? 'var(--status-success-background-3)'
        : status === 'caution' ? 'var(--status-warning-background-3)'
            : status === 'warning' ? 'var(--palette-orange-background3)'
                : status === 'danger' ? 'var(--status-danger-background-3)'
                    : 'var(--neutral-foreground-4)';
