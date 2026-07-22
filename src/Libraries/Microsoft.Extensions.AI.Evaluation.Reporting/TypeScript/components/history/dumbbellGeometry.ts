// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { CSSProperties } from 'react';
import type { DeltaJudgment } from '../core/metricDirection';
import { statusSolidVar } from '../styles/reportStyles';

const DUMBBELL_D = 8;
const DUMBBELL_RING = 1.5;
const DUMBBELL_CONN = 1.5;

export const posOn = (v: number, min: number, max: number): number =>
    max > min ? Math.max(0, Math.min(100, ((v - min) / (max - min)) * 100)) : 50;

type DumbbellStyles = {
    sk: DeltaJudgment;
    connector: CSSProperties;
    dotB: CSSProperties;
    dotA: CSSProperties;
};

export const dumbbellStyles = (
    prevPos: number | null,
    currPos: number,
    hasDelta: boolean,
    status: DeltaJudgment = 'neutral',
    connEpsilon = 0.01,
): DumbbellStyles => {
    const sk: DeltaJudgment = status;
    const color = statusSolidVar(sk);
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
