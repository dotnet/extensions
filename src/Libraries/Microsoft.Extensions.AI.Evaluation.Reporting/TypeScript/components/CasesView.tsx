// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { useEffect, useMemo, useRef, useState } from 'react';
import {
    makeStyles,
    mergeClasses,
    Switch,
    SearchBox,
    Menu,
    MenuTrigger,
    MenuButton,
    MenuPopover,
    MenuList,
    MenuItemCheckbox,
    MenuDivider,
    Button,
} from '@fluentui/react-components';
import { ChevronRight16Regular, TagMultipleRegular } from '@fluentui/react-icons';
import { MoverDirections, getTabsterAttribute } from 'tabster';
import { useReportStyles, statusSolidVar } from './reportStyles';
import { useReportContext } from './ReportContext';
import { ScoreNode, getConversationDisplay } from './Summary';
import { isLeafFailed } from './viewModels';
import { categorizeAndSortTags } from './TagsDisplay';
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
    tagMenuHead: {
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        gap: 'var(--spacing-m)',
        padding: 'var(--spacing-xs) var(--spacing-m) var(--spacing-s)',
    },
    tagMenuTitle: {
        fontSize: 'var(--font-size-100)',
        fontWeight: 'var(--font-weight-bold)',
        textTransform: 'uppercase',
        letterSpacing: '0.5px',
        color: 'var(--neutral-foreground-3)',
    },
    tagMenuList: { maxHeight: '320px', minWidth: '248px' },
    collapseBtn: {
        appearance: 'none',
        border: 'none',
        cursor: 'pointer',
        fontFamily: 'inherit',
        fontSize: 'var(--font-size-200)',
        fontWeight: 'var(--font-weight-semibold)',
        lineHeight: 'var(--line-height-200)',
        padding: 'var(--spacing-xs) var(--spacing-s)',
        borderRadius: 'var(--radius-medium)',
        backgroundColor: 'transparent',
        color: 'var(--neutral-foreground-2)',
        transition: 'background-color var(--duration-faster) var(--curve-easy-ease)',
        '&:hover:not(:disabled)': { backgroundColor: 'var(--subtle-background-hover)', color: 'var(--neutral-foreground-1)' },
        '&:disabled': { color: 'var(--neutral-foreground-disabled)', cursor: 'default' },
    },

    rowlist: { overflow: 'hidden' },
    rowWrap: { borderTop: '1px solid var(--neutral-stroke-3)', '&:first-child': { borderTop: 'none' } },
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
        fontFamily: 'var(--font-family-monospace)',
        fontSize: 'var(--font-size-200)',
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
        gridTemplateColumns: '1.12fr 1fr',
        gap: 'var(--spacing-l)',
        alignItems: 'start',
        maxWidth: '75rem',
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
    failed: boolean;
    scenario: ScenarioRunResult;
};

const buildRows = (root: ScoreNode): CaseRowVM[] => {
    const rows: CaseRowVM[] = [];
    for (const node of root.flattenedNodes) {
        if (!node.isLeafNode || !node.scenario) {
            continue;
        }
        const segments = node.name.split(' / ');
        const label = segments[segments.length - 1];
        const group = segments.length > 1 ? segments.slice(0, -1).join(' · ') : undefined;
        rows.push({
            key: node.nodeKey,
            label,
            group,
            failed: isLeafFailed(node.scenario),
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
    onToggle,
    registerRowRef,
}: {
    vm: CaseRowVM;
    open: boolean;
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
        <div className={mergeClasses(classes.rowWrap, 'eval-case-wrap', open && 'is-open')}>
            <button
                type="button"
                ref={(el) => registerRowRef(vm.key, el)}
                className={mergeClasses(classes.row, 'eval-case-row')}
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
                {vm.group && <span className={s.badge}>{vm.group}</span>}
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
                        <TranscriptBlock messages={conversation.messages} />
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
        activeNode,
        filterTree,
        failedOnly,
        setFailedOnly,
        scenSort,
        setCasePage,
        casePage,
        selectedTags,
        handleTagClick,
        searchValue,
        setSearchValue,
        clearFilters,
    } = useReportContext();

    const { filterableTags } = categorizeAndSortTags(dataset, activeExecution);

    const [openKey, setOpenKey] = useState<string | null>(null);
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

    const allRows = useMemo(() => {
        const filtered = filterTree(activeNode);
        return filtered ? buildRows(filtered) : [];
    }, [filterTree, activeNode]);

    const rows = useMemo(() => {
        const filtered = failedOnly ? allRows.filter((r) => r.failed) : allRows;
        const sorted = [...filtered];
        if (scenSort === 'passRate') {
            sorted.sort((a, b) => Number(b.failed) - Number(a.failed) || a.label.localeCompare(b.label));
        } else {
            sorted.sort((a, b) => a.label.localeCompare(b.label));
        }
        return sorted;
    }, [allRows, failedOnly, scenSort]);

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
                    <Menu checkedValues={{ tags: selectedTags }}>
                        <MenuTrigger disableButtonEnhancement>
                            <MenuButton
                                appearance="secondary"
                                icon={<TagMultipleRegular />}
                                className="eval-fitbtn"
                            >
                                {tagLabel}
                            </MenuButton>
                        </MenuTrigger>
                        <MenuPopover>
                            <div className={classes.tagMenuHead}>
                                <span className={classes.tagMenuTitle}>Filter by tag</span>
                                {selectedTags.length > 0 && (
                                    <Button
                                        appearance="transparent"
                                        size="small"
                                        onClick={() => {
                                            selectedTags.forEach((t) => handleTagClick(t));
                                            setCasePage(1);
                                        }}
                                    >
                                        Clear
                                    </Button>
                                )}
                            </div>
                            <MenuDivider />
                            <MenuList className={classes.tagMenuList}>
                                {filterableTags.map(({ tag, count }) => (
                                    <MenuItemCheckbox
                                        key={tag}
                                        name="tags"
                                        value={tag}
                                        onClick={() => {
                                            handleTagClick(tag);
                                            setCasePage(1);
                                        }}
                                        secondaryContent={String(count)}
                                    >
                                        {tag}
                                    </MenuItemCheckbox>
                                ))}
                            </MenuList>
                        </MenuPopover>
                    </Menu>
                )}

                <button
                    type="button"
                    className={classes.collapseBtn}
                    disabled={openKey === null}
                    onClick={() => closeOpen(false)}
                >
                    Collapse open
                </button>

                <Switch
                    checked={failedOnly}
                    onChange={(_ev, data) => {
                        setFailedOnly(data.checked);
                        setCasePage(1);
                    }}
                    label="Failing only"
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
