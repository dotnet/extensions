// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { describe, it, expect } from 'vitest';
import { formatNumber, formatValue } from '../components/core/metricModel';

const numericMetric = (value?: number): NumericMetric =>
    ({
        $type: 'numeric',
        name: 'm',
        value,
        metadata: {},
    }) as NumericMetric;

const booleanMetric = (value?: boolean): BooleanMetric =>
    ({
        $type: 'boolean',
        name: 'm',
        value,
        metadata: {},
    }) as BooleanMetric;

const stringMetric = (value?: string): StringMetric =>
    ({
        $type: 'string',
        name: 'm',
        value,
        metadata: {},
    }) as StringMetric;

const noneMetric = (): MetricWithNoValue =>
    ({
        $type: 'none',
        name: 'm',
        value: undefined,
        metadata: {},
    }) as MetricWithNoValue;

describe('formatNumber — pinned precision policy (round to <=3dp, strip trailing zeros)', () => {
    it('pins the worked examples from the plan', () => {
        expect(formatNumber(0.87)).toBe('0.87');
        expect(formatNumber(0.5)).toBe('0.5');
        expect(formatNumber(842)).toBe('842');
        expect(formatNumber(4)).toBe('4');
        expect(formatNumber(4.5)).toBe('4.5');
        expect(formatNumber(6)).toBe('6');
        expect(formatNumber(4.333333333)).toBe('4.333');
    });

    it('normalizes -0 to "0"', () => {
        expect(formatNumber(-0)).toBe('0');
    });
});

describe('formatValue — classification by $type', () => {
    it('numeric: formats via formatNumber', () => {
        expect(formatValue(numericMetric(0.87))).toBe('0.87');
        expect(formatValue(numericMetric(842))).toBe('842');
    });

    it('numeric: absent value renders the em-dash placeholder, not a crash', () => {
        expect(formatValue(numericMetric(undefined))).toBe('—');
    });

    it('boolean: value -> Yes/No', () => {
        expect(formatValue(booleanMetric(true))).toBe('Yes');
        expect(formatValue(booleanMetric(false))).toBe('No');
    });

    it('boolean: absent value -> undefined (no fabricated default)', () => {
        expect(formatValue(booleanMetric(undefined))).toBeUndefined();
    });

    it('string: raw text passthrough', () => {
        expect(formatValue(stringMetric('PASS'))).toBe('PASS');
    });

    it('none: undefined', () => {
        expect(formatValue(noneMetric())).toBeUndefined();
    });
});
