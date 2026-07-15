// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import React from 'react';
import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ReportContextProvider, createScoreSummary, getConversationDisplay, TranscriptBlock } from '../components';
import { toolCallDataset, toolCallScenario } from './fixtures/richDataset';

const renderTranscript = (messages: ChatMessage[], modelResponse?: ChatResponse) => {
    const scoreSummary = createScoreSummary(toolCallDataset);
    const { messages: display } = getConversationDisplay(messages, modelResponse);
    return render(
        <ReportContextProvider dataset={toolCallDataset} scoreSummary={scoreSummary}>
            <TranscriptBlock messages={display} />
        </ReportContextProvider>,
    );
};

describe('TranscriptBlock — functionCall / functionResult discrimination', () => {
    it('renders a merged tool section with the function name, Input caption and arguments', () => {
        renderTranscript(toolCallScenario.messages, toolCallScenario.modelResponse);

        expect(screen.getByText('Tool call: get_current_weather')).toBeInTheDocument();
        // The arguments render as a JSON body (distinct from the title above): exactly one
        // <pre> carries both call parameters, so this asserts the body — not the fn name again.
        const argsBody = screen.getByText(
            (_content, el) =>
                el?.tagName === 'PRE' &&
                /"location": "Seattle, WA"/.test(el.textContent ?? '') &&
                /"unit": "celsius"/.test(el.textContent ?? ''),
        );
        expect(argsBody).toBeInTheDocument();
        expect(screen.getByText('Input')).toBeInTheDocument();
        expect(screen.getByText(/Seattle, WA/)).toBeInTheDocument();
    });

    it('renders the result payload under an Output caption in the same section', () => {
        renderTranscript(toolCallScenario.messages, toolCallScenario.modelResponse);

        expect(screen.getByText('Output')).toBeInTheDocument();
        expect(screen.getByText(/Partly cloudy/)).toBeInTheDocument();
    });

    it('still renders ordinary text content alongside tool blocks', () => {
        renderTranscript(toolCallScenario.messages, toolCallScenario.modelResponse);

        expect(screen.getByText(/Let me look that up for you\./)).toBeInTheDocument();
        expect(screen.getByText(/What is the weather in Seattle right now\?/)).toBeInTheDocument();
    });
});

describe('TranscriptBlock — unknown $type degrades gracefully', () => {
    const mysteryContent = {
        $type: 'mysteryWidget',
        payload: { sentinel: 'UNKNOWN_TYPE_SENTINEL', nested: [1, 2, 3] },
    } as unknown as AIContent;

    const mysteryMessages: ChatMessage[] = [
        { role: 'user', contents: [{ $type: 'text', text: 'Trigger the mystery.' } as unknown as AIContent] },
        { role: 'assistant', authorName: 'gpt-4o', contents: [mysteryContent] },
    ];

    it('does not throw when rendering an unknown content type', () => {
        expect(() => renderTranscript(mysteryMessages)).not.toThrow();
    });

    it('serializes the unknown content so its data is still visible', () => {
        renderTranscript(mysteryMessages);
        expect(screen.getByText(/UNKNOWN_TYPE_SENTINEL/)).toBeInTheDocument();
    });

    it('renders the transcript shell (header) even with only unknown content', () => {
        renderTranscript(mysteryMessages);
        expect(screen.getByText('Transcript')).toBeInTheDocument();
    });
});

describe('TranscriptBlock — image content renders as <img> with derived alt text', () => {
    it('renders a UriContent image (mediaType image/*) with the user-side alt text', () => {
        const uriImage = {
            $type: 'uri',
            uri: 'https://example.com/cat.png',
            mediaType: 'image/png',
        } as unknown as AIContent;
        const messages: ChatMessage[] = [{ role: 'user', contents: [uriImage] }];

        renderTranscript(messages);

        const img = screen.getByRole('img', { name: 'Image shared by the user' });
        expect(img).toHaveAttribute('src', 'https://example.com/cat.png');
    });

    it('renders a DataContent image (data:image/ URI) with the assistant-side alt text', () => {
        const dataUri = 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCA',
            dataImage = { $type: 'data', uri: dataUri } as unknown as AIContent;
        const messages: ChatMessage[] = [{ role: 'assistant', authorName: 'gpt-4o', contents: [dataImage] }];

        renderTranscript(messages);

        const img = screen.getByRole('img', { name: 'Image shared by the assistant' });
        expect(img).toHaveAttribute('src', dataUri);
    });
});
