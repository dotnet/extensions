// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import React, { useEffect } from 'react';
import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ReportContextProvider, createScoreSummary, getConversationDisplay, TranscriptBlock, useReportContext } from '../components';
import { singleExecutionDataset, toolCallDataset, toolCallScenario, twoExecutionDataset } from './fixtures/richDataset';

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

    it('serializes the unknown content so its data is still visible', () => {
        renderTranscript(mysteryMessages);
        expect(screen.getByText(/UNKNOWN_TYPE_SENTINEL/)).toBeInTheDocument();
    });

    it('renders the transcript shell (header) even with only unknown content', () => {
        renderTranscript(mysteryMessages);
        expect(screen.getByText('Transcript')).toBeInTheDocument();
    });
});

describe('TranscriptBlock — malformed content degrades gracefully', () => {
    it.each([
        ['null', null],
        ['primitive', 'MALFORMED_PRIMITIVE_SENTINEL'],
        ['non-string text', { $type: 'text', text: 42 }],
        ['non-string data URI', { $type: 'data', uri: 42 }],
        ['non-string media type', { $type: 'uri', uri: 'https://example.com/image.png', mediaType: 42 }],
    ])('serializes %s content without throwing', (_name, malformedContent) => {
        const messages: ChatMessage[] = [{
            role: 'assistant',
            contents: [malformedContent as unknown as AIContent],
        }];

        expect(() => renderTranscript(messages)).not.toThrow();
    });

    it('continues treating forward-compatible reasoning content as text', () => {
        const reasoningContent = {
            $type: 'reasoning',
            text: 'FORWARD_REASONING_SENTINEL',
        } as unknown as AIContent;
        const messages: ChatMessage[] = [{ role: 'assistant', contents: [reasoningContent] }];

        renderTranscript(messages);

        expect(screen.getByText('FORWARD_REASONING_SENTINEL')).toBeInTheDocument();
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

describe('TranscriptBlock — empty transcript message', () => {
    const NOT_KEPT = 'Transcript not kept for this execution. Reports only keep transcripts for the latest execution (exec-v1).';
    const NO_TRANSCRIPT = 'No transcript for this case.';

    const ExecSetter = ({ exec, children }: { exec?: string; children: React.ReactNode }) => {
        const { setExec } = useReportContext();
        useEffect(() => {
            if (exec !== undefined) setExec(exec);
        }, [exec, setExec]);
        return <>{children}</>;
    };

    const renderEmpty = (dataset: Dataset, exec?: string, messages: ChatMessage[] = []) => {
        const { messages: display } = getConversationDisplay(messages, { messages: [] });
        return render(
            <ReportContextProvider dataset={dataset} scoreSummary={createScoreSummary(dataset)}>
                <ExecSetter exec={exec}>
                    <TranscriptBlock messages={display} />
                </ExecSetter>
            </ReportContextProvider>,
        );
    };

    it('says the transcript was not kept when an older execution is selected', async () => {
        renderEmpty(twoExecutionDataset, 'exec-v2');

        expect(await screen.findByText(NOT_KEPT)).toBeInTheDocument();
        expect(screen.queryByText(NO_TRANSCRIPT)).not.toBeInTheDocument();
    });

    it('keeps the generic message for the latest execution', () => {
        renderEmpty(twoExecutionDataset);

        expect(screen.getByText(NO_TRANSCRIPT)).toBeInTheDocument();
        expect(screen.queryByText(/not kept/)).not.toBeInTheDocument();
    });

    it('keeps the generic message when the report has a single execution', () => {
        renderEmpty(singleExecutionDataset);

        expect(screen.getByText(NO_TRANSCRIPT)).toBeInTheDocument();
        expect(screen.queryByText(/not kept/)).not.toBeInTheDocument();
    });

    it('still renders the transcript of an older execution when it has one', async () => {
        renderEmpty(twoExecutionDataset, 'exec-v2', [
            { role: 'user', contents: [{ $type: 'text', text: 'Older execution question' } as unknown as AIContent] },
        ]);

        expect(await screen.findByText(/Older execution question/)).toBeInTheDocument();
        expect(screen.queryByText(/not kept/)).not.toBeInTheDocument();
        expect(screen.queryByText(NO_TRANSCRIPT)).not.toBeInTheDocument();
    });
});
