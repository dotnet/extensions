// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { CSSProperties } from 'react';

export const DUMBBELL_D = 8;
export const DUMBBELL_RING = 1.5;
export const DUMBBELL_CONN = 1.5;

export type MetricScaleKind = 'fraction' | 'score' | 'severity' | 'count';

export const metricScale = (kind: MetricScaleKind, peak: number): [number, number] =>
    kind === 'fraction' ? [0, 1] : kind === 'score' ? [1, 5] : [0, Math.max(1, peak || 1)];

export const posOn = (v: number, min: number, max: number): number =>
    max > min ? Math.max(0, Math.min(100, ((v - min) / (max - min)) * 100)) : 50;

export const formatRaw = (v: number, kind: MetricScaleKind): string => {
    if (kind === 'fraction') return v.toFixed(3);
    if (kind === 'score') return (v % 1 === 0 ? `${v}` : v.toFixed(1)) + '/5';
    return v % 1 === 0 ? `${v}` : v.toFixed(1);
};

export type StatusKey = 'success' | 'warning' | 'danger' | 'caution' | 'neutral';

const STATUS_SOLID: Record<StatusKey, string> = {
    success: 'var(--status-success-background-3)',
    warning: 'var(--status-warning-foreground-2)',
    danger: 'var(--status-danger-foreground-2)',
    caution: 'var(--palette-yellow-foreground1)',
    neutral: 'var(--neutral-foreground-4)',
};

export const STATUS_TEXT: Record<StatusKey, string> = {
    success: 'var(--status-success-foreground-1)',
    warning: 'var(--status-warning-foreground-1)',
    danger: 'var(--status-danger-foreground-1)',
    caution: 'var(--palette-yellow-foreground1)',
    neutral: 'var(--neutral-foreground-3)',
};

export type DumbbellStyles = {
    sk: StatusKey;
    connector: CSSProperties;
    dotB: CSSProperties;
    dotA: CSSProperties;
};

export const dumbbellStyles = (
    prevPos: number | null,
    currPos: number,
    dir: number,
    hasDelta: boolean,
    connEpsilon = 0.01,
): DumbbellStyles => {
    const sk: StatusKey = dir > 0 ? 'success' : dir < 0 ? 'danger' : 'neutral';
    const color = STATUS_SOLID[sk];
    const halo = '0 0 0 2px var(--neutral-background-1)';
    const hasPrev = prevPos !== null && prevPos !== undefined && Number.isFinite(prevPos);
    const cur = Math.max(0, Math.min(100, currPos));
    const prv = hasPrev ? Math.max(0, Math.min(100, prevPos!)) : cur;
    const lo = Math.min(prv, cur);
    const hi = Math.max(prv, cur);
    const connVisible = hasPrev && hasDelta && hi - lo > connEpsilon;
    return {
        sk,
        connector: connVisible
            ? { position: 'absolute', top: '50%', left: `${lo}%`, width: `${hi - lo}%`, height: `${DUMBBELL_CONN}px`, transform: 'translateY(-50%)', borderRadius: 'var(--radius-circular)', background: color }
            : { display: 'none' },
        dotB: hasPrev
            ? { position: 'absolute', top: '50%', left: `${prv}%`, width: `${DUMBBELL_D}px`, height: `${DUMBBELL_D}px`, boxSizing: 'border-box', transform: 'translate(-50%,-50%)', borderRadius: '50%', background: 'var(--neutral-background-1)', border: `${DUMBBELL_RING}px solid var(--neutral-foreground-3)`, boxShadow: halo }
            : { display: 'none' },
        dotA: { position: 'absolute', top: '50%', left: `${cur}%`, width: `${DUMBBELL_D}px`, height: `${DUMBBELL_D}px`, boxSizing: 'border-box', transform: 'translate(-50%,-50%)', borderRadius: '50%', background: color, boxShadow: halo },
    };
};
