// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* ==========================================================================
   Shared mockup primitives — the FIDELITY CONTRACT for the v3.1 re-skin.

   A single makeStyles module exporting the report's recurring visual patterns,
   built to pixel-match "AI Evaluation Report v3.1.dc.html". Every view body
   (OverviewView / CasesView / HistoryView / ComparisonView) imports `useMockupStyles`
   and composes these classes so the cards, headers, status dots, pass-rate bars,
   delta pills, badges, tables, sidebar rows, segmented controls and tabs are
   IDENTICAL across views.

   All values reference the inlined design tokens from theme.css via CSS `var()`
   (--neutral-*, --spacing-*, --radius-*, --font-*, --status-*, --shadow-*), so
   one `darkMode` boolean re-themes Fluent components AND these primitives in
   lockstep (light in :root, dark under .fluent-dark). NO hard-coded hex except
   where a token does not exist; NO external assets (CSP-clean).

   Mockup line references (v3.1.dc.html) are noted per primitive so the view
   workers can cross-check the source treatment.
   ========================================================================== */

import { makeStyles, mergeClasses } from '@fluentui/react-components';

/** Re-export so callers can compose primitives without a second import. */
export { mergeClasses };

export const useMockupStyles = makeStyles({
    /* ── card ──────────────────────────────────────────────────────────────
       Top-level section surface. Solid neutral-background-1 (so dense content
       reads cleanly over the tinted acrylic canvas) + a subtle neutral-stroke-2
       hairline + 8px (--radius-card) rounding. FLAT — the hairline alone
       delineates the card; no elevation shadow (mockup lines 382-387). Clips its
       own content to the rounded corner. */
    card: {
        backgroundColor: 'var(--neutral-background-1)',
        border: '1px solid var(--neutral-stroke-2)',
        borderRadius: 'var(--radius-card)',
        overflow: 'hidden',
    },
    /* Nested card (a card inside a card — e.g. the open-case transcript/metric
       panels). Recessed neutral-background-2 surface, hairline, 6px radius, NO
       shadow — kills the stacked "three white layers" effect (mockup 388-400). */
    cardNested: {
        backgroundColor: 'var(--neutral-background-2)',
        border: '1px solid var(--neutral-stroke-2)',
        borderRadius: 'var(--radius-large)',
        overflow: 'hidden',
    },
    /* Card body inset matching the mockup's internal padding rhythm (cards pad
       their content with 16px / 20px). Use on the immediate child of `card`. */
    cardPad: {
        padding: 'var(--spacing-l) var(--spacing-xl)',
    },

    /* ── sectionHeader ─────────────────────────────────────────────────────
       The card section header row: a 16px title flush-left, optional sub/meta to
       the right, a hairline divider beneath. Matches the "Biggest movers" /
       "Pass rate by scenario group" heads (mockup 661-665, 731). */
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
    /* The eyebrow caption — 10px uppercase, letter-spaced, tertiary. Used for
       "OVERALL PASS RATE", "TRANSCRIPT", "METRICS", section sub-labels (mockup
       627, 636, 812, 880). */
    eyebrow: {
        fontSize: 'var(--font-size-100)',
        fontWeight: 'var(--font-weight-semibold)',
        color: 'var(--neutral-foreground-3)',
        textTransform: 'uppercase',
        letterSpacing: '0.5px',
    },

    /* ── statusDot ─────────────────────────────────────────────────────────
       The 8px round status indicator preceding a scenario/metric name. The base
       class sets shape; callers set the fill via inline `backgroundColor` from a
       status solid token (--status-success-background-3 / --status-danger-background-3
       / --status-warning-background-3 / --neutral-foreground-4). Mockup movers/
       groups/attention dots (668, 738, 687). */
    statusDot: {
        width: '8px',
        height: '8px',
        borderRadius: 'var(--radius-circular)',
        flex: 'none',
        display: 'inline-block',
        backgroundColor: 'var(--neutral-foreground-4)',
    },
    /* Status fill helpers — compose with statusDot (or any dot/segment). */
    fillSuccess: { backgroundColor: 'var(--status-success-background-3)' },
    fillDanger: { backgroundColor: 'var(--status-danger-background-3)' },
    fillWarning: { backgroundColor: 'var(--status-warning-background-3)' },
    fillNeutral: { backgroundColor: 'var(--neutral-foreground-4)' },
    /* Status TEXT helpers (the foreground-1 step, contrast-safe per theme). */
    textSuccess: { color: 'var(--status-success-foreground-1)' },
    textDanger: { color: 'var(--status-danger-foreground-1)' },
    textWarning: { color: 'var(--status-warning-foreground-1)' },

    /* ── passRateBar ───────────────────────────────────────────────────────
       THE signature pass-rate treatment: a THIN underline-style bar with the %
       rendered ABOVE the line, right-aligned — NOT a filled pill (mockup 742;
       screenshots final-overview.png). Structure the view emits:
         <span class={passRateBar}>
           <span class={passRateValue}>79%</span>
           <span class={passRateTrack}><span class={passRateFill} style={{width,background}}/></span>
         </span>
       The fill width is the percentage; the fill background is the row's status
       solid token (green good / yellow-orange warn / red weak). Track is the
       --meter-track unfilled rail. */
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
    passRateTrack: {
        position: 'relative',
        width: '100%',
        height: '4px',
        borderRadius: 'var(--radius-circular)',
        backgroundColor: 'var(--meter-track)',
        overflow: 'hidden',
    },
    passRateFill: {
        position: 'absolute',
        left: 0,
        top: 0,
        bottom: 0,
        borderRadius: 'var(--radius-circular)',
        backgroundColor: 'var(--compound-brand-background)',
    },

    /* ── deltaPill ─────────────────────────────────────────────────────────
       The directional change chip: ▲ up (green) / ▼ down (red) / → flat (neutral).
       A tint background with the matching status foreground text, circular, tabular
       digits. Movers badges / Δ-run column (mockup 670, 743; screenshots show
       "▲ +0.5", "▼ -1%", "→ 0%"). Compose with deltaUp / deltaDown / deltaFlat. */
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

    /* ── badge / countPill ─────────────────────────────────────────────────
       The pass-count / tag chip — a tint fill carrying colored text only (the
       Fluent stroke is dropped so it reads as a calm tint, not a bordered chip).
       Circular, small, tabular. Sidebar pass counts ("12/12", "4/11") + Cases tab
       count (mockup 589, 608; screenshots). Compose with a count* color helper. */
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
    /* A neutral scenario tag chip (e.g. the per-row "RAG.Answer" pill in Cases) —
       grey background, secondary text, slightly more horizontal padding, 20px tall
       (mockup 794). */
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

    /* ── tableHeader + tableRow ────────────────────────────────────────────
       Grid-based "table" rows used by the Overview group table, History run table
       and Comparison table. The header is a 10px uppercase letter-spaced row with
       a hairline beneath; data rows are 14px with a faint stroke-3 separator. The
       view sets `gridTemplateColumns` inline (column proportions differ per table).
       Mockup 733-737. */
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
    /* A right-aligned numeric cell with tabular figures (Good/Fair/Weak/% columns). */
    tableNum: {
        textAlign: 'right',
        fontVariantNumeric: 'tabular-nums',
        color: 'var(--neutral-foreground-1)',
    },

    /* ── sidebarItem ───────────────────────────────────────────────────────
       The scenario-tree nav row primitive (also surfaced via AppShell). Transparent
       row, 32px min height, medium radius, flex layout with caret + label + pill.
       The transparent/backdrop-lift hover-and-select affordance lives in theme.css
       (.eval-toc-row). Apply BOTH `sidebarItem` and the `eval-toc-row` class to the
       button. Mockup 584-590. */
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
    /* The uppercase section label above a sidebar group ("SCENARIOS", "EXECUTION"). */
    sidebarSectionLabel: {
        fontSize: 'var(--font-size-200)',
        fontWeight: 'var(--font-weight-semibold)',
        color: 'var(--neutral-foreground-3)',
        textTransform: 'uppercase',
        letterSpacing: '0.5px',
    },

    /* ── segmentedPill ─────────────────────────────────────────────────────
       The History metric selector: an acrylic track holding equal-feel pills with
       a single white sliding indicator behind the active one. The view renders the
       track, an absolutely-positioned `.eval-slide-ind` span, and the pill buttons;
       JS drives the indicator transform. Hover lift + indicator come from theme.css
       (.eval-seg-track / .eval-seg-btn / .eval-slide-ind). Mockup 961-964. */
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
    /* Active pill: semibold + primary text (the white fill is the sliding indicator). */
    segmentedPillActive: {
        color: 'var(--neutral-foreground-1)',
        fontWeight: 'var(--font-weight-semibold)',
    },
    /* The sliding white indicator (one per track). Position/size driven by JS. */
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

    /* ── tab ───────────────────────────────────────────────────────────────
       The Foundry pivot (top-of-panel tab). foreground-2 rest, semibold active,
       grey hover underline, sliding 2px brand underline indicator behind the
       active tab. Apply `tab` + the `eval-pivot` class (and `eval-pivot is-active`
       on the active one); the underline behaviors live in theme.css. The sliding
       brand indicator uses `slideIndicatorUnderline`. Mockup 604-608. */
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
    /* The sliding brand underline (one per tablist). Position/width driven by JS. */
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

    /* ── viewLink ──────────────────────────────────────────────────────────
       The inline "View" / "View cases" action link in the Needs-attention rows —
       a borderless brand-foreground button with a brand-tint hover (mockup 690). */
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
});

/**
 * Convenience maps for picking a status fill / text / count / delta class from a
 * status string, so view code stays declarative. `status` is the report's rating
 * bucket reduced to success | warning | danger | neutral.
 */
export type MockupStatus = 'success' | 'warning' | 'danger' | 'neutral';

export const pickFill = (
    s: ReturnType<typeof useMockupStyles>,
    status: MockupStatus,
): string =>
    status === 'success' ? s.fillSuccess
        : status === 'warning' ? s.fillWarning
            : status === 'danger' ? s.fillDanger
                : s.fillNeutral;

export const pickStatusText = (
    s: ReturnType<typeof useMockupStyles>,
    status: MockupStatus,
): string | undefined =>
    status === 'success' ? s.textSuccess
        : status === 'warning' ? s.textWarning
            : status === 'danger' ? s.textDanger
                : undefined;

/** Solid status token (for inline fill of bars/dots/segments). */
export const statusSolidVar = (status: MockupStatus): string =>
    status === 'success' ? 'var(--status-success-background-3)'
        : status === 'warning' ? 'var(--status-warning-background-3)'
            : status === 'danger' ? 'var(--status-danger-background-3)'
                : 'var(--neutral-foreground-4)';
