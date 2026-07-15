// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import React from 'react';
import { describe, it, expect } from 'vitest';
import { render } from '@testing-library/react';
import { ReportContextProvider, createScoreSummary, getConversationDisplay, TranscriptBlock } from '../components';
import { toolCallDataset } from './fixtures/richDataset';

// withCitations()/CITE_RE live inside TranscriptBlock and only run through the markdown
// pipeline, so we exercise them by rendering assistant prose (renderMarkdown defaults to true).
const renderProse = (text: string) => {
    const scoreSummary = createScoreSummary(toolCallDataset);
    const messages: ChatMessage[] = [
        { role: 'assistant', authorName: 'gpt-4o', contents: [{ $type: 'text', text } as unknown as AIContent] },
    ];
    const { messages: display } = getConversationDisplay(messages);
    return render(
        <ReportContextProvider dataset={toolCallDataset} scoreSummary={scoreSummary}>
            <TranscriptBlock messages={display} />
        </ReportContextProvider>,
    );
};

const cites = (container: HTMLElement): HTMLElement[] =>
    Array.from(container.querySelectorAll('sup.eval-cite'));

describe('TranscriptBlock — [[n]] citation transform', () => {
    it('converts multiple [[n]] markers into numbered sup.eval-cite nodes', () => {
        const { container } = renderProse('See the docs [[1]] and the spec [[2]] for details.');
        const sups = cites(container);
        expect(sups).toHaveLength(2);
        expect(sups.map((s) => s.textContent)).toEqual(['1', '2']);
    });

    it('handles adjacent citations with no text between them', () => {
        const { container } = renderProse('Sources [[3]][[4]] agree.');
        const sups = cites(container);
        expect(sups).toHaveLength(2);
        expect(sups.map((s) => s.textContent)).toEqual(['3', '4']);
    });

    it('preserves multi-digit citation numbers', () => {
        const { container } = renderProse('As shown [[42]].');
        const sups = cites(container);
        expect(sups).toHaveLength(1);
        expect(sups[0].textContent).toBe('42');
    });

    it('leaves plain text without citations untouched', () => {
        const { container } = renderProse('This paragraph has no citations at all.');
        expect(cites(container)).toHaveLength(0);
        expect(container.textContent).toContain('This paragraph has no citations at all.');
    });
});
