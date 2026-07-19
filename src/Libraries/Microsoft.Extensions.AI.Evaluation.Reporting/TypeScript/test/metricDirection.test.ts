// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { describe, it, expect } from 'vitest';
import {
    inferBetterDirections,
    judgeValueDelta,
    judgmentWord,
    ratingGoodness,
} from '../components/core/metricDirection';

type VR = [value: number, rating: EvaluationRating];

const metric = (name: string, value: number, rating: EvaluationRating): NumericMetric => ({
    $type: 'numeric',
    name,
    value,
    interpretation: { rating, failed: rating === 'poor' || rating === 'unacceptable' },
});

const results = (name: string, samples: VR[]): ScenarioRunResult[] =>
    samples.map(([value, rating], i) => ({
        scenarioName: `S.${name}`,
        iterationName: String(i),
        executionName: 'Run',
        evaluationResult: { metrics: { [name]: metric(name, value, rating) } },
    }) as unknown as ScenarioRunResult);

describe('inferBetterDirections — direction learned from interpretation.rating', () => {
    it('higher value tracking better ratings ⇒ higher', () => {
        const d = inferBetterDirections(results('Coherence', [
            [5, 'exceptional'], [4, 'good'], [3, 'average'], [2, 'poor'], [1, 'unacceptable'],
        ]));
        expect(d.get('Coherence')).toBe('higher');
    });

    it('higher value tracking worse ratings ⇒ lower (Safety-style inverted metric)', () => {
        const d = inferBetterDirections(results('Violence', [
            [6, 'unacceptable'], [5, 'poor'], [3, 'average'], [1, 'good'], [0, 'exceptional'],
        ]));
        expect(d.get('Violence')).toBe('lower');
    });

    it('no rating variance ⇒ none (cannot infer a direction)', () => {
        const d = inferBetterDirections(results('Flat', [
            [4, 'good'], [4, 'good'], [4, 'good'], [5, 'good'],
        ]));
        expect(d.get('Flat')).toBe('none');
    });

    it('fewer than three rated samples ⇒ none', () => {
        const d = inferBetterDirections(results('Sparse', [[5, 'exceptional'], [1, 'unacceptable']]));
        expect(d.get('Sparse')).toBe('none');
    });

    it('only unrated/inconclusive samples ⇒ metric absent from the map', () => {
        const d = inferBetterDirections(results('NoSignal', [
            [1, 'unknown'], [2, 'inconclusive'], [3, 'unknown'],
        ]));
        expect(d.has('NoSignal')).toBe(false);
    });

    it('mixes samples across scenarios for the same metric name', () => {
        const a = results('Fluency', [[5, 'exceptional'], [4, 'good']]);
        const b = results('Fluency', [[2, 'poor']]).map((r) => ({ ...r, scenarioName: 'S.other' }));
        expect(inferBetterDirections([...a, ...b]).get('Fluency')).toBe('higher');
    });
});

describe('judgeValueDelta — combines direction with the value delta, with a rating fallback', () => {
    it('higher-better: up ⇒ success, down ⇒ danger', () => {
        expect(judgeValueDelta('higher', 0.9)).toBe('success');
        expect(judgeValueDelta('higher', -0.4)).toBe('danger');
    });

    it('lower-better: down ⇒ success, up ⇒ danger', () => {
        expect(judgeValueDelta('lower', -0.5)).toBe('success');
        expect(judgeValueDelta('lower', 2)).toBe('danger');
    });

    it('indeterminate direction falls back to the rating-goodness delta', () => {
        expect(judgeValueDelta('none', 1, 0.5)).toBe('success');
        expect(judgeValueDelta('none', 1, -0.5)).toBe('danger');
    });

    it('indeterminate with no/zero rating signal ⇒ neutral', () => {
        expect(judgeValueDelta('none', 1)).toBe('neutral');
        expect(judgeValueDelta('none', 1, 0)).toBe('neutral');
    });

    it('an inferred direction takes precedence over a supplied rating-goodness delta', () => {
        expect(judgeValueDelta('higher', -1, 5)).toBe('danger');
        expect(judgeValueDelta('lower', -1, -5)).toBe('success');
    });
});

describe('ratingGoodness / judgmentWord', () => {
    it('maps only the ordinal ratings, excluding unknown/inconclusive', () => {
        expect(ratingGoodness('exceptional')).toBe(5);
        expect(ratingGoodness('unacceptable')).toBe(1);
        expect(ratingGoodness('unknown')).toBeUndefined();
        expect(ratingGoodness('inconclusive')).toBeUndefined();
        expect(ratingGoodness(undefined)).toBeUndefined();
    });

    it('words only the judged states', () => {
        expect(judgmentWord('success')).toBe('improved');
        expect(judgmentWord('danger')).toBe('regressed');
        expect(judgmentWord('neutral')).toBeUndefined();
    });
});
