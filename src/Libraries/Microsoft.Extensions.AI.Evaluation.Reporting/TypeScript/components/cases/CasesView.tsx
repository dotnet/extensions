// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { useEffect, useMemo, useRef, useState } from 'react';
import {
    makeStyles,
    mergeClasses,
    Switch,
    SearchBox,
    MenuButton,
    Button,
    Link,
} from '@fluentui/react-components';
import { ChevronRight16Regular } from '@fluentui/react-icons';
import { MoverDirections, getTabsterAttribute } from 'tabster';
import { useReportStyles, statusSolidVar } from '../styles/reportStyles';
import { useReportContext } from '../core/ReportContext';
import { ScoreNode, getConversationDisplay } from '../core/Summary';
import { isLeafFailed, scenariosForExecution } from '../core/viewModels';
import { TranscriptBlock } from './TranscriptBlock';
import { MetricPanel } from './MetricPanel';

const PAGE_SIZE = 25;

const useStyles = makeStyles({
    root: { display: 'flex', flexDirection: 'column', gap: 'var(--spacing-l)' },

    controls: {
        display: 'flex',
        alignItems: 'center',
        gap: 'var(--spacing-s)',
        flexWrap: 'wrap',
        justifyContent: 'flex-end',
    },
    searchWrap: {
        flex: '1 1 auto',
        minWidth: '180px',
        display: 'flex',
    },
    search: { width: '100%' },
    tagWrap: { position: 'relative' },
    tagOverlay: { position: 'fixed', inset: 0, zIndex: 40 },
    tagPopover: {
        position: 'absolute',
        top: 'calc(100% + var(--spacing-xs))',
        right: 0,
        zIndex: 41,
        backgroundColor: 'var(--neutral-background-1)',
        border: '1px solid var(--neutral-stroke-1)',
        borderRadius: 'var(--radius-large)',
        boxShadow: 'var(--shadow-16)',
        padding: 'var(--spacing-l)',
        width: '312px',
        maxHeight: '360px',
        overflow: 'auto',
    },
    tagMenuHead: {
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        marginBottom: 'var(--spacing-m)',
    },
    tagMenuTitle: {
        fontSize: 'var(--font-size-100)',
        fontWeight: 'var(--font-weight-bold)',
        textTransform: 'uppercase',
        letterSpacing: '0.5px',
        color: 'var(--neutral-foreground-3)',
    },
    tagGrid: {
        display: 'flex',
        flexWrap: 'wrap',
        gap: 'var(--spacing-s-nudge)',
    },
    // The base look of a tag pill is set inline (see below); these hover slots use
    // !important to override that inline background on hover.
    tagOpt: {
        ':hover': { background: 'var(--neutral-background-3) !important' },
    },
    tagOptActive: {
        ':hover': { background: 'var(--brand-background-2-hover) !important' },
    },

    rowlist: { overflow: 'hidden' },
    rowWrap: { borderTop: '1px solid var(--neutral-stroke-3)', '&:first-child': { borderTop: 'none' } },
    // Animated left accent bar that slides in when the case is expanded.
    caseWrap: {
        position: 'relative',
        '&::before': {
            content: '""',
            position: 'absolute',
            left: 0,
            top: 0,
            bottom: 0,
            width: '2px',
            zIndex: 3,
            background: 'var(--brand-stroke-1)',
            pointerEvents: 'none',
            opacity: 0,
            transform: 'scaleY(0)',
            transformOrigin: 'top',
            transition:
                'transform var(--duration-normal) var(--curve-decelerate-mid), ' +
                'opacity var(--duration-faster) var(--curve-easy-ease)',
        },
    },
    caseWrapOpen: {
        '&::before': { opacity: 1, transform: 'scaleY(1)' },
    },
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
        padding: 'var(--spacing-m) var(--spacing-l)',
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
    dotWrap: { display: 'inline-flex', flex: 'none' },
    statusDot: {
        width: '8px',
        height: '8px',
        borderRadius: 'var(--radius-circular)',
        flex: 'none',
        boxSizing: 'border-box',
    },
    label: {
        flex: '1 1 auto',
        minWidth: 0,
        marginRight: 'auto',
        fontFamily: 'var(--font-family-base)',
        fontSize: 'var(--font-size-300)',
        fontWeight: 'var(--font-weight-medium)',
        color: 'var(--neutral-foreground-2)',
        whiteSpace: 'nowrap',
        overflow: 'hidden',
        textOverflow: 'ellipsis',
    },

    detail: {
        padding: '0 var(--spacing-xxl) var(--spacing-xxl) var(--spacing-xxxl)',
        outline: 'none',
        backgroundColor: 'transparent',
    },
    metaLine: {
        padding: 'var(--spacing-l) 0',
        maxWidth: '75rem',
    },
    metaText: {
        fontSize: 'var(--font-size-200)',
        color: 'var(--neutral-foreground-3)',
        letterSpacing: '0.2px',
    },
    twoPane: {
        display: 'grid',
        gridTemplateColumns: 'minmax(0, 3fr) minmax(0, 2fr)',
        gap: 'var(--spacing-l)',
        alignItems: 'start',
    },

    empty: {
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
    clearLink: {
        appearance: 'none',
        border: 'none',
        cursor: 'pointer',
        fontFamily: 'inherit',
        fontSize: 'var(--font-size-300)',
        fontWeight: 'var(--font-weight-semibold)',
        color: 'var(--brand-foreground-1)',
        backgroundColor: 'transparent',
        padding: 'var(--spacing-xs) var(--spacing-s)',
        borderRadius: 'var(--radius-medium)',
        marginTop: 'var(--spacing-xs)',
        '&:hover': { backgroundColor: 'var(--brand-background-2)', textDecoration: 'underline' },
    },

    pager: {
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        gap: 'var(--spacing-m)',
        marginTop: 'var(--spacing-xs)',
    },
    pagerBtn: {
        appearance: 'none',
        cursor: 'pointer',
        fontFamily: 'inherit',
        fontSize: 'var(--font-size-300)',
        lineHeight: 1,
        padding: 'var(--spacing-s) var(--spacing-m)',
        borderRadius: 'var(--radius-medium)',
        border: '1px solid var(--neutral-stroke-2)',
        backgroundColor: 'var(--neutral-background-1)',
        color: 'var(--neutral-foreground-2)',
        transition: 'background-color var(--duration-faster) var(--curve-easy-ease)',
        '&:hover:not(:disabled)': { backgroundColor: 'var(--subtle-background-hover)', color: 'var(--neutral-foreground-1)' },
        '&:disabled': { color: 'var(--neutral-foreground-disabled)', cursor: 'default', opacity: 0.6 },
    },
    pagerLabel: {
        fontSize: 'var(--font-size-300)',
        color: 'var(--neutral-foreground-3)',
        fontVariantNumeric: 'tabular-nums',
        padding: '0 var(--spacing-s)',
    },
});

type CaseRowVM = {
    key: string;
    label: string;
    group?: string;
    scenarioName: string;
    failed: boolean;
    isNew: boolean;
    scenOrder: number;
    index: number;
    scenario: ScenarioRunResult;
};

// prevKeys holds `scenarioName#iterationName` for the previous execution; a case is
// New when its key isn't there. Undefined = earliest run.
const buildRows = (root: ScoreNode, prevKeys: Set<string> | undefined): CaseRowVM[] => {
    const rows: CaseRowVM[] = [];
    const scenOrder = new Map<string, number>();
    for (const node of root.flattenedNodes) {
        if (!node.isLeafNode || !node.scenario) {
            continue;
        }
        const segments = node.name.split(' / ');
        const label = segments[segments.length - 1];
        const scenarioName = node.scenario.scenarioName;
        const group = scenarioName || (segments.length > 1 ? segments.slice(0, -1).join(' · ') : undefined);
        if (!scenOrder.has(scenarioName)) {
            scenOrder.set(scenarioName, scenOrder.size);
        }
        const isNew = prevKeys ? !prevKeys.has(`${scenarioName}#${node.scenario.iterationName}`) : false;
        rows.push({
            key: node.nodeKey,
            label,
            group,
            scenarioName,
            failed: isLeafFailed(node.scenario),
            isNew,
            scenOrder: scenOrder.get(scenarioName)!,
            index: rows.length,
            scenario: node.scenario,
        });
    }
    return rows;
};

const metaLineFor = (scenario: ScenarioRunResult): string | undefined => {
    const tags = scenario.tags ?? [];
    if (tags.length === 0) {
        return undefined;
    }
    return tags
        .map((t) => {
            const i = t.indexOf(':');
            return i > 0 ? t.slice(i + 1).trim() : t;
        })
        .join('  ·  ');
};

const CaseRow = ({
    vm,
    open,
    showTag,
    onToggle,
    registerRowRef,
}: {
    vm: CaseRowVM;
    open: boolean;
    showTag: boolean;
    onToggle: () => void;
    registerRowRef: (key: string, el: HTMLButtonElement | null) => void;
}) => {
    const classes = useStyles();
    const s = useReportStyles();
    const detailRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        if (open && detailRef.current) {
            detailRef.current.focus();
        }
    }, [open]);

    const dotSolid = vm.failed ? statusSolidVar('danger') : statusSolidVar('success');
    const conversation = open ? getConversationDisplay(vm.scenario.messages, vm.scenario.modelResponse) : null;
    const metaLine = open ? metaLineFor(vm.scenario) : undefined;

    return (
        <div className={mergeClasses(classes.rowWrap, classes.caseWrap, open && classes.caseWrapOpen)}>
            <button
                type="button"
                ref={(el) => registerRowRef(vm.key, el)}
                className={mergeClasses(classes.row, classes.rowInteractive)}
                aria-expanded={open}
                aria-label={`${vm.label}${vm.failed ? ' (failed)' : ' (passed)'}`}
                onClick={onToggle}
            >
                <ChevronRight16Regular className={mergeClasses(classes.caret, open && classes.caretOpen)} />
                <span className={classes.dotWrap} title={vm.failed ? 'Failed' : 'Passed'}>
                    <span
                        className={classes.statusDot}
                        style={{ backgroundColor: dotSolid, boxShadow: `0 0 0 3px color-mix(in srgb, ${dotSolid} 18%, transparent)` }}
                        aria-hidden="true"
                    />
                </span>
                <span className={classes.label}>{vm.label}</span>
                {vm.isNew && <span className={mergeClasses(s.badge, 'eval-new-badge')}>New</span>}
                {showTag && vm.group && <span className={s.badge}>{vm.group}</span>}
            </button>

            {open && conversation && (
                <div
                    ref={detailRef}
                    className={classes.detail}
                    tabIndex={-1}
                    role="region"
                    aria-label={`${vm.label} detail`}
                    {...getTabsterAttribute({ mover: { direction: MoverDirections.Both } })}
                >
                    {metaLine && (
                        <div className={classes.metaLine}>
                            <span className={classes.metaText}>{metaLine}</span>
                        </div>
                    )}
                    <div className={mergeClasses(classes.twoPane, 'eval-twopane')}>
                        <TranscriptBlock messages={conversation.messages} model={conversation.model} />
                        <MetricPanel scenario={vm.scenario} />
                    </div>
                </div>
            )}
        </div>
    );
};

export const CasesView = () => {
    const classes = useStyles();
    const s = useReportStyles();
    const {
        dataset,
        activeExecution,
        scopedNode,
        selectedScenarioLevel,
        filterTree,
        failedOnly,
        setFailedOnly,
        setCasePage,
        casePage,
        selectedTags,
        handleTagClick,
        searchValue,
        setSearchValue,
        clearFilters,
    } = useReportContext();

    const scopedScenarioNames = useMemo(() => {
        if (!selectedScenarioLevel) return undefined;
        return new Set(
            scopedNode.flattenedNodes
                .filter((n) => n.isLeafNode && n.scenario)
                .map((n) => n.scenario!.scenarioName),
        );
    }, [scopedNode, selectedScenarioLevel]);

    const filterableTags = useMemo(() => {
        const results = (dataset.scenarioRunResults ?? []).filter(
            (r) => r.executionName === activeExecution &&
                (!scopedScenarioNames || scopedScenarioNames.has(r.scenarioName)),
        );
        const total = results.length;
        const counts = new Map<string, number>();
        for (const r of results) for (const tag of r.tags ?? []) counts.set(tag, (counts.get(tag) ?? 0) + 1);
        return [...counts.entries()]
            .filter(([, c]) => c !== total)
            .map(([tag, count]) => ({ tag, count }))
            .sort((a, b) => b.count - a.count);
    }, [dataset, activeExecution, scopedScenarioNames]);

    const [openKey, setOpenKey] = useState<string | null>(null);
    const [tagMenuOpen, setTagMenuOpen] = useState(false);
    const rowRefs = useRef(new Map<string, HTMLButtonElement | null>());
    const registerRowRef = (key: string, el: HTMLButtonElement | null) => {
        if (el) {
            rowRefs.current.set(key, el);
        } else {
            rowRefs.current.delete(key);
        }
    };

    const closeOpen = (returnFocus: boolean) => {
        const key = openKey;
        setOpenKey(null);
        if (returnFocus && key) {
            requestAnimationFrame(() => rowRefs.current.get(key)?.focus());
        }
    };

    // Previous execution = the run immediately before the active one (same baseline Overview uses).
    const prevKeys = useMemo(() => {
        const execs: string[] = [];
        const seen = new Set<string>();
        for (const r of dataset.scenarioRunResults ?? []) {
            if (!seen.has(r.executionName)) {
                seen.add(r.executionName);
                execs.push(r.executionName);
            }
        }
        const activeIdx = execs.indexOf(activeExecution);
        const prevExec = activeIdx <= 0 ? execs[1] : execs[activeIdx - 1];
        if (!prevExec) {
            return undefined;
        }
        return new Set(
            scenariosForExecution(dataset, prevExec).map((r) => `${r.scenarioName}#${r.iterationName}`),
        );
    }, [dataset, activeExecution]);

    const allRows = useMemo(() => {
        const filtered = filterTree(scopedNode);
        return filtered ? buildRows(filtered, prevKeys) : [];
    }, [filterTree, scopedNode, prevKeys]);

    const rows = useMemo(() => {
        const filtered = failedOnly ? allRows.filter((r) => r.failed) : allRows;
        const sorted = [...filtered];
        sorted.sort((a, b) => {
            if (a.scenOrder !== b.scenOrder) return a.scenOrder - b.scenOrder;
            if (a.isNew !== b.isNew) return a.isNew ? -1 : 1;
            return a.index - b.index;
        });
        return sorted;
    }, [allRows, failedOnly]);

    // A row hides its scenario tag only when the scope is a single scenario (its tag would be
    // redundant); broader scopes keep tags.
    const selectedScenarioName = useMemo(() => {
        if (!selectedScenarioLevel) return undefined;
        const names = new Set(
            scopedNode.flattenedNodes.filter((n) => n.isLeafNode && n.scenario).map((n) => n.scenario!.scenarioName),
        );
        return names.size === 1 ? [...names][0] : undefined;
    }, [selectedScenarioLevel, scopedNode]);

    const pageCount = Math.max(1, Math.ceil(rows.length / PAGE_SIZE));
    useEffect(() => {
        if (casePage > pageCount) {
            setCasePage(pageCount);
        }
    }, [casePage, pageCount, setCasePage]);
    const page = Math.min(casePage, pageCount);
    const pageRows = rows.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

    useEffect(() => {
        if (openKey && !pageRows.some((r) => r.key === openKey)) {
            setOpenKey(null);
        }
    }, [openKey, pageRows]);

    const hasActiveFilter = selectedTags.length > 0 || !!searchValue || failedOnly;
    const searchCount = allRows.length;
    const searchPlaceholder = `Search across ${searchCount} ${searchCount === 1 ? 'case' : 'cases'}`;
    const tagLabel = selectedTags.length > 0 ? `${selectedTags.length} selected` : 'All tags';

    return (
        <div className={classes.root} data-screen-label="cases">
            <div className={classes.controls}>
                <div className={mergeClasses(classes.searchWrap, 'eval-search-wrap')}>
                    <SearchBox
                        className={classes.search}
                        aria-label="Search cases"
                        placeholder={searchPlaceholder}
                        value={searchValue}
                        onChange={(_ev, data) => setSearchValue(data.value)}
                    />
                </div>

                {filterableTags.length > 0 && (
                    <div className={mergeClasses(classes.tagWrap, 'eval-fitbtn')}>
                        <MenuButton
                            appearance="secondary"
                            onClick={() => setTagMenuOpen((v) => !v)}
                        >
                            {tagLabel}
                        </MenuButton>
                        {tagMenuOpen && (
                            <>
                                <div className={classes.tagOverlay} onClick={() => setTagMenuOpen(false)} />
                                <div className={classes.tagPopover}>
                                    <div className={classes.tagMenuHead}>
                                        <span className={classes.tagMenuTitle}>Filter by tag</span>
                                        <Link
                                            onClick={() => {
                                                selectedTags.forEach((t) => handleTagClick(t));
                                                setCasePage(1);
                                            }}
                                        >
                                            Clear
                                        </Link>
                                    </div>
                                    <div className={classes.tagGrid}>
                                        {filterableTags.map(({ tag }) => {
                                            const active = selectedTags.includes(tag);
                                            return (
                                                <button
                                                    key={tag}
                                                    type="button"
                                                    className={active ? classes.tagOptActive : classes.tagOpt}
                                                    aria-pressed={active}
                                                    onClick={() => {
                                                        handleTagClick(tag);
                                                        setCasePage(1);
                                                    }}
                                                    style={{
                                                        flex: '1 1 auto',
                                                        boxSizing: 'border-box',
                                                        border: active ? '1px solid var(--brand-stroke-1)' : '1px solid var(--neutral-stroke-1)',
                                                        background: active ? 'var(--brand-background-2)' : 'transparent',
                                                        color: active ? 'var(--brand-foreground-1)' : 'var(--neutral-foreground-2)',
                                                        fontWeight: active ? 'var(--font-weight-semibold)' : 'var(--font-weight-regular)',
                                                        borderRadius: 'var(--radius-circular)',
                                                        padding: 'var(--spacing-xs) var(--spacing-l)',
                                                        cursor: 'pointer',
                                                        fontFamily: 'inherit',
                                                        fontSize: 'var(--font-size-200)',
                                                        lineHeight: 1.3,
                                                        whiteSpace: 'nowrap',
                                                        textAlign: 'center',
                                                        transition:
                                                            'background-color var(--duration-faster) var(--curve-easy-ease), border-color var(--duration-faster) var(--curve-easy-ease)',
                                                    }}
                                                >
                                                    {tag}
                                                </button>
                                            );
                                        })}
                                    </div>
                                </div>
                            </>
                        )}
                    </div>
                )}

                <Button
                    appearance="secondary"
                    disabled={openKey === null}
                    onClick={() => closeOpen(false)}
                    className="eval-fitbtn"
                >
                    Collapse open
                </Button>

                <Switch
                    checked={failedOnly}
                    onChange={(_ev, data) => {
                        setFailedOnly(data.checked);
                        setCasePage(1);
                    }}
                    label="Show failed"
                />
            </div>

            {pageRows.length === 0 ? (
                <div className={s.card}>
                    <div className={classes.empty}>
                        <span className={classes.emptyTitle}>No matching cases</span>
                        <span className={classes.emptyReason}>
                            {hasActiveFilter
                                ? 'No scenarios match the current search, tags, or failing-only filter.'
                                : 'There are no scenario results to display.'}
                        </span>
                        {hasActiveFilter && (
                            <button type="button" className={classes.clearLink} onClick={clearFilters}>Clear filters</button>
                        )}
                    </div>
                </div>
            ) : (
                <div className={s.card}>
                    <div className={mergeClasses(classes.rowlist, 'eval-rowlist')}>
                        {pageRows.map((vm) => (
                            <CaseRow
                                key={vm.key}
                                vm={vm}
                                open={openKey === vm.key}
                                showTag={vm.scenarioName !== selectedScenarioName}
                                onToggle={() => (openKey === vm.key ? closeOpen(true) : setOpenKey(vm.key))}
                                registerRowRef={registerRowRef}
                            />
                        ))}
                    </div>
                </div>
            )}

            {pageCount > 1 && (
                <div className={classes.pager}>
                    <button
                        type="button"
                        className={classes.pagerBtn}
                        disabled={page <= 1}
                        onClick={() => setCasePage(Math.max(1, page - 1))}
                    >
                        ‹ Prev
                    </button>
                    <span className={classes.pagerLabel}>Page {page} of {pageCount}</span>
                    <button
                        type="button"
                        className={classes.pagerBtn}
                        disabled={page >= pageCount}
                        onClick={() => setCasePage(Math.min(pageCount, page + 1))}
                    >
                        Next ›
                    </button>
                </div>
            )}
        </div>
    );
};
