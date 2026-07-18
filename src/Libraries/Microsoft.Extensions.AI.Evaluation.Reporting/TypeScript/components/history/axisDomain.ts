// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { formatNumber } from '../core/metricModel';

export type AxisDomain = {
    min: number;
    max: number;
    ticks: number;
    fmt: (v: number) => string;
};

export const axisDomain = (values: readonly number[]): AxisDomain => {
    let dataLo = Infinity;
    let dataHi = -Infinity;
    for (const v of values) {
        if (!Number.isFinite(v)) continue;
        if (v < dataLo) dataLo = v;
        if (v > dataHi) dataHi = v;
    }
    if (!Number.isFinite(dataLo)) {
        dataLo = 0;
        dataHi = 1;
    }

    const [min, max] = dataHi <= 1 && dataLo >= 0
        ? [0, 1]
        : [Math.min(1, Math.floor(dataLo)), Math.max(5, Math.ceil(dataHi))];

    const ticks = min === 0 && max === 1 ? 5 : Math.max(1, Math.round(max - min));
    return { min, max, ticks, fmt: formatNumber };
};
