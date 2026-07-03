// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { useMemo, useState, type KeyboardEvent, type MouseEvent } from 'react';
import { Badge, makeStyles, mergeClasses } from '@fluentui/react-components';
import { ChevronRight16Regular } from '@fluentui/react-icons';
import { MoverDirections, getTabsterAttribute } from 'tabster';
import { useReportContext } from './ReportContext';
import { ScoreNode } from './Summary';
import { useReportStyles } from './reportStyles';

const useLocalStyles = makeStyles({
    caretButton: {
        appearance: 'none',
        padding: 0,
        margin: 0,
        border: 'none',
        background: 'transparent',
        cursor: 'pointer',
        color: 'inherit',
        outlineStyle: 'none',
    },
    caretOpen: { transform: 'rotate(90deg)' },
    labelGroup: { textTransform: 'uppercase', letterSpacing: '0.5px' },
    labelStrong: { fontWeight: 'var(--font-weight-semibold)' },
    labelSelected: { color: 'var(--neutral-foreground-1)' },
    labelDefault: { color: 'var(--neutral-foreground-2)' },
    labelGroupColor: { color: 'var(--neutral-foreground-3)' },
    pillSlot: {
        flex: 'none',
        display: 'inline-flex',
        justifyContent: 'flex-end',
        minWidth: '48px',
        fontVariantNumeric: 'tabular-nums',
    },
    empty: {
        fontSize: 'var(--font-size-200)',
        color: 'var(--neutral-foreground-3)',
        padding: 'var(--spacing-xxs) var(--spacing-m-nudge)',
    },
});

type SidebarRowVM = {
    key: string;
    label: string;
    depth: number;
    hasChildren: boolean;
    expanded: boolean;
    selected: boolean;
    isTopGroup: boolean;
    passing: number;
    total: number;
    onSelect: () => void;
    onToggle: () => void;
};

const DEPTH_PAD = [
    'var(--spacing-m-nudge)',
    'var(--spacing-xxl)',
    'var(--spacing-xxxl)',
    'calc(var(--spacing-xxxl) + var(--spacing-s))',
    'calc(var(--spacing-xxxl) + var(--spacing-l))',
] as const;
const padForDepth = (depth: number) => DEPTH_PAD[Math.min(depth, DEPTH_PAD.length - 1)];

const pillProps = (passing: number, total: number) => {
    if (total === 0 || passing >= total) {
        return { appearance: 'ghost' as const, color: 'informative' as const };
    }
    return { appearance: 'tint' as const, color: passing / total < 0.5 ? ('danger' as const) : ('warning' as const) };
};

export const SidebarTree = () => {
    const local = useLocalStyles();
    const { activeNode, selectedScenarioLevel, selectScenarioLevel } = useReportContext();

    const topGroupKeys = useMemo(
        () => activeNode.childNodes.filter((n) => n.childNodes.length > 0).map((n) => n.nodeKey),
        [activeNode],
    );
    const [expanded, setExpanded] = useState<Set<string>>(() => new Set(topGroupKeys));

    const toggle = (key: string) =>
        setExpanded((prev) => {
            const next = new Set(prev);
            if (next.has(key)) next.delete(key); else next.add(key);
            return next;
        });

    const scopeTo = (nodeKey: string | undefined) => {
        const target = nodeKey ?? '';
        if (target !== (selectedScenarioLevel ?? '')) {
            selectScenarioLevel(target);
        }
        // Selecting a scenario re-scopes the CURRENT tab — it does not jump to Cases.
    };

    const rows = useMemo<SidebarRowVM[]>(() => {
        const out: SidebarRowVM[] = [];
        const walk = (nodes: ScoreNode[], depth: number) => {
            // The sidebar shows the Group > Scenario hierarchy only; it never descends into
            // iteration leaves. A node is expandable only if it has non-leaf children.
            const branches = nodes.filter((n) => !n.isLeafNode);
            const sorted = [...branches].sort((a, b) => a.name.localeCompare(b.name));
            for (const node of sorted) {
                const hasChildren = node.childNodes.some((c) => !c.isLeafNode);
                const isExpanded = expanded.has(node.nodeKey);
                out.push({
                    key: node.nodeKey,
                    label: node.name,
                    depth,
                    hasChildren,
                    expanded: isExpanded,
                    selected: selectedScenarioLevel === node.nodeKey,
                    isTopGroup: depth === 0 && hasChildren,
                    passing: node.numPassingIterations,
                    total: node.numPassingIterations + node.numFailingIterations,
                    onSelect: () => scopeTo(node.nodeKey),
                    onToggle: () => toggle(node.nodeKey),
                });
                if (hasChildren && isExpanded) {
                    walk(node.childNodes, depth + 1);
                }
            }
        };
        walk(activeNode.childNodes, 0);
        return out;
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [activeNode, expanded, selectedScenarioLevel]);

    return (
        <div
            role="tree"
            aria-label="Scenarios"
            {...getTabsterAttribute({ mover: { direction: MoverDirections.Vertical, cyclic: true } })}
        >
            <SidebarRow
                label="All scenarios"
                depth={0}
                hasChildren={false}
                expanded={false}
                selected={!selectedScenarioLevel}
                isTopGroup={false}
                onSelect={() => scopeTo(undefined)}
            />

            {rows.length === 0 ? (
                <div className={local.empty}>No scenarios</div>
            ) : (
                rows.map((row) => (
                    <SidebarRow
                        key={row.key}
                        label={row.label}
                        depth={row.depth}
                        hasChildren={row.hasChildren}
                        expanded={row.expanded}
                        selected={row.selected}
                        isTopGroup={row.isTopGroup}
                        passing={row.passing}
                        total={row.total}
                        onSelect={row.onSelect}
                        onToggle={row.onToggle}
                    />
                ))
            )}
        </div>
    );
};

const SidebarRow = ({
    label,
    depth,
    hasChildren,
    expanded,
    selected,
    isTopGroup,
    passing,
    total,
    onSelect,
    onToggle,
}: {
    label: string;
    depth: number;
    hasChildren: boolean;
    expanded: boolean;
    selected: boolean;
    isTopGroup: boolean;
    passing?: number;
    total?: number;
    onSelect: () => void;
    onToggle?: () => void;
}) => {
    const s = useReportStyles();
    const local = useLocalStyles();

    // The treeitem is a div, not a button, so the caret can be a real nested
    // button — nesting a button inside a button is invalid HTML and swallows clicks.
    const onRowKeyDown = (e: KeyboardEvent<HTMLDivElement>) => {
        if (e.key === 'Enter' || e.key === ' ' || e.key === 'Spacebar') {
            e.preventDefault();
            onSelect();
            return;
        }
        if (!hasChildren || !onToggle) return;
        if (e.key === 'ArrowRight' && !expanded) { e.preventDefault(); onToggle(); }
        else if (e.key === 'ArrowLeft' && expanded) { e.preventDefault(); onToggle(); }
    };

    const onCaretClick = (e: MouseEvent<HTMLButtonElement>) => {
        e.stopPropagation();
        onToggle?.();
    };

    const labelColor = selected
        ? local.labelSelected
        : isTopGroup
            ? local.labelGroupColor
            : local.labelDefault;

    return (
        <div
            role="treeitem"
            tabIndex={0}
            aria-level={depth + 1}
            aria-selected={selected}
            {...(hasChildren ? { 'aria-expanded': expanded } : {})}
            title={label}
            className={mergeClasses(s.sidebarItem, 'eval-toc-row', selected && 'is-selected')}
            style={{ paddingLeft: padForDepth(depth) }}
            onClick={onSelect}
            onKeyDown={onRowKeyDown}
        >
            {hasChildren ? (
                <button
                    type="button"
                    tabIndex={-1}
                    className={mergeClasses(s.sidebarCaret, local.caretButton, expanded && local.caretOpen)}
                    onClick={onCaretClick}
                    aria-label={expanded ? `Collapse ${label}` : `Expand ${label}`}
                >
                    <ChevronRight16Regular />
                </button>
            ) : (
                <span className={s.sidebarCaret} aria-hidden="true" />
            )}
            <span
                className={mergeClasses(
                    s.sidebarItemLabel,
                    isTopGroup && local.labelGroup,
                    (selected || isTopGroup) && local.labelStrong,
                    labelColor,
                )}
            >
                {label}
            </span>
            {total !== undefined && total > 0 && (
                <span className={local.pillSlot}>
                    <Badge {...pillProps(passing ?? 0, total)} shape="circular">
                        {`${passing ?? 0}/${total}`}
                    </Badge>
                </span>
            )}
        </div>
    );
};
