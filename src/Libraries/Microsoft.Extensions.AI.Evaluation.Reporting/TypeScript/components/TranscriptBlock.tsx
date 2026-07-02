// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { CSSProperties } from 'react';
import { Card } from '@fluentui/react-components';
import ReactMarkdown from 'react-markdown';
import { useReportContext } from './ReportContext';
import { type ChatMessageDisplay, isTextContent, isImageContent } from './Summary';

type FunctionCallLike = AIContent & {
    $type: 'functionCall';
    callId?: string;
    name?: string;
    arguments?: unknown;
};

type FunctionResultLike = AIContent & {
    $type: 'functionResult';
    callId?: string;
    result?: unknown;
};

const isFunctionCall = (content: AIContent): content is FunctionCallLike =>
    content?.$type === 'functionCall';

const isFunctionResult = (content: AIContent): content is FunctionResultLike =>
    content?.$type === 'functionResult';

const KNOWN_CONTENT_TYPES = new Set(['text', 'uri', 'data', 'functionCall', 'functionResult']);

const isToolish = (content: AIContent): boolean => {
    const t = content?.$type ?? '';
    return !KNOWN_CONTENT_TYPES.has(t) && /call|result|tool|function/i.test(t);
};

// Teal tool-section surface (1:1 with the mockup VM color-mix formulas). The
// accent TEXT — title, icon, Input/Output captions — uses the AA-safe
// --tool-accent-text token; the raw teal fails AA on the tinted head.
const T_ACCENT = 'var(--tool-accent-text)';
const T_TINT = 'color-mix(in srgb, var(--palette-teal-foreground) 7%, var(--neutral-background-1))';
const T_HEAD = 'color-mix(in srgb, var(--palette-teal-foreground) 14%, var(--neutral-background-1))';
const T_BORDER = 'color-mix(in srgb, var(--palette-teal-foreground) 30%, var(--neutral-background-1))';

const isUserSide = (role: string) => role.toLowerCase() === 'user';
const isSystemSide = (role: string) => role.toLowerCase() === 'system';
const isToolSide = (role: string) => role.toLowerCase() === 'tool';

const safeJson = (value: unknown, pretty: boolean): string => {
    try {
        return JSON.stringify(value, null, pretty ? 2 : 0) ?? String(value);
    } catch {
        return String(value);
    }
};

// Derive a human display model id from the assistant group's participant name.
// getConversationDisplay formats it as "<authorName> (<role>)" when an author
// exists; strip the trailing " (role)" to recover the model id.
const modelFromParticipant = (participantName: string): string => {
    const m = participantName.match(/^(.*)\s+\([^)]*\)\s*$/);
    const name = (m ? m[1] : participantName).trim();
    return name && name.toLowerCase() !== 'assistant' ? name : '—';
};

// Mirror the mockup chatClock(): time + long-form date parsed from createdAt.
const chatClock = (createdAt: string | undefined): { time: string; date: string } => {
    const s = createdAt ?? '';
    const time = (s.match(/(\d{1,2}:\d{2})/) ?? [])[1] ?? '';
    const dm = s.match(/(\d{4})-(\d{2})-(\d{2})/);
    let date = 'Today';
    if (dm) {
        const months = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
        date = `${months[(+dm[2]) - 1]} ${+dm[3]}, ${dm[1]}`;
    }
    return { time, date };
};

// ── View-model (reconstructs the mockup's convo → convoM → groups pipeline
//    from the flat ChatMessageDisplay stream) ───────────────────────────────

type ToolSectionVM = {
    name: string;
    callText: string;
    hasResult: boolean;
    resultText: string;
};

type BubbleVM = {
    tools: ToolSectionVM[];
    text: AIContent | null;
};

type GroupVM =
    | { kind: 'group'; role: 'user' | 'assistant'; bubbles: BubbleVM[]; participantName: string }
    | { kind: 'system'; content: AIContent };

const buildGroups = (messages: ChatMessageDisplay[]): GroupVM[] => {
    const groups: GroupVM[] = [];
    // Assistant turns buffer tool calls in `pending`; they fold into the top of
    // the next reply bubble as sections. A turn can run tool → reply → tool.
    let pending: ToolSectionVM[] = [];
    let openAssistant: Extract<GroupVM, { kind: 'group' }> | null = null;

    const flushPending = () => {
        if (openAssistant && pending.length) {
            openAssistant.bubbles.push({ tools: pending, text: null });
        }
        pending = [];
    };

    for (const m of messages) {
        const content = m.content;

        if (isFunctionResult(content)) {
            // Merge into the most recent buffered call (adjacent call+result → one section).
            const last = pending[pending.length - 1];
            const resultText = safeJsonMaybeString(content.result);
            if (last && !last.hasResult) {
                last.hasResult = true;
                last.resultText = resultText;
            } else {
                pending.push({ name: content.callId ?? 'result', callText: '', hasResult: true, resultText });
            }
            continue;
        }

        if (isFunctionCall(content)) {
            if (!openAssistant) {
                openAssistant = { kind: 'group', role: 'assistant', bubbles: [], participantName: m.participantName };
                groups.push(openAssistant);
            }
            pending.push({
                name: content.name ?? 'function',
                callText: safeJson(content.arguments ?? {}, true),
                hasResult: false,
                resultText: '',
            });
            continue;
        }

        if (isToolish(content)) {
            if (!openAssistant) {
                openAssistant = { kind: 'group', role: 'assistant', bubbles: [], participantName: m.participantName };
                groups.push(openAssistant);
            }
            pending.push({
                name: content.$type ?? 'tool',
                callText: safeJson(content, true),
                hasResult: false,
                resultText: '',
            });
            continue;
        }

        if (isSystemSide(m.role)) {
            flushPending();
            openAssistant = null;
            groups.push({ kind: 'system', content });
            continue;
        }

        if (isUserSide(m.role)) {
            flushPending();
            openAssistant = null;
            const prev = groups[groups.length - 1];
            if (prev && prev.kind === 'group' && prev.role === 'user') {
                prev.bubbles.push({ tools: [], text: content });
            } else {
                groups.push({ kind: 'group', role: 'user', bubbles: [{ tools: [], text: content }], participantName: m.participantName });
            }
            continue;
        }

        if (isToolSide(m.role)) {
            // A tool-role text turn with no function content — treat as buffered tool data.
            if (!openAssistant) {
                openAssistant = { kind: 'group', role: 'assistant', bubbles: [], participantName: m.participantName };
                groups.push(openAssistant);
            }
            pending.push({ name: 'tool', callText: safeJson(content, true), hasResult: false, resultText: '' });
            continue;
        }

        // assistant (or any non-user/system) reply text — attach pending tools + text.
        if (!openAssistant) {
            openAssistant = { kind: 'group', role: 'assistant', bubbles: [], participantName: m.participantName };
            groups.push(openAssistant);
        }
        openAssistant.bubbles.push({ tools: pending, text: content });
        pending = [];
    }
    flushPending();

    return groups;
};

const safeJsonMaybeString = (value: unknown): string =>
    typeof value === 'string' ? value : safeJson(value ?? null, true);

// ── Rendering ──────────────────────────────────────────────────────────────

const AV_BASE: CSSProperties = {
    width: '32px',
    height: '32px',
    alignSelf: 'start',
    borderRadius: 'var(--radius-circular)',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    color: 'var(--white)',
    flex: 'none',
    gridRow: 1,
};

const SparkleIcon = () => (
    <svg viewBox="0 0 20 20" fill="currentColor" style={{ width: '18px', height: '18px' }} aria-hidden="true">
        <path d="M10 1.6l1.2 2.6 2.6 1.2-2.6 1.2L10 9.2 8.8 6.6 6.2 5.4l2.6-1.2zM5 11l.8 1.7L7.5 13.5 5.8 14.3 5 16l-.8-1.7L2.5 13.5l1.7-.8zM15 11l.8 1.7 1.7.8-1.7.8L15 16l-.8-1.7-1.7-.8 1.7-.8z" />
    </svg>
);

const PersonIcon = () => (
    <svg viewBox="0 0 20 20" fill="currentColor" style={{ width: '18px', height: '18px' }} aria-hidden="true">
        <circle cx="10" cy="6" r="3.2" />
        <path d="M3.5 17c0-3.3 2.9-5.5 6.5-5.5s6.5 2.2 6.5 5.5z" />
    </svg>
);

const WrenchIcon = () => (
    <svg viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth={1.6} strokeLinecap="round" strokeLinejoin="round" style={{ width: '16px', height: '16px' }} aria-hidden="true">
        <path d="M13.7 3.5a3.5 3.5 0 00-4.6 4.2l-5 5a1.4 1.4 0 002 2l5-5a3.5 3.5 0 004.2-4.6l-2 2-1.6-.4-.4-1.6z" />
    </svg>
);

const MD_TEXT_STYLE: CSSProperties = {
    whiteSpace: 'normal',
    display: 'flex',
    flexDirection: 'column',
    gap: 'var(--spacing-xxs)',
    wordBreak: 'break-word',
};

const CODE_STYLE: CSSProperties = {
    margin: 0,
    fontFamily: 'var(--font-family-monospace)',
    fontSize: 'var(--font-size-200)',
    lineHeight: 'var(--line-height-200)',
    whiteSpace: 'pre-wrap',
    wordBreak: 'break-word',
    color: 'var(--neutral-foreground-1)',
};

const TextNode = ({ content }: { content: AIContent }) => {
    const { renderMarkdown, prettifyJson } = useReportContext();

    if (isTextContent(content)) {
        const trimmed = content.text.trim();
        try {
            const parsed = JSON.parse(trimmed);
            return <pre style={CODE_STYLE}>{JSON.stringify(parsed, null, prettifyJson ? 2 : 0)}</pre>;
        } catch {
            // not JSON — render as markdown / plain text below
        }
        return renderMarkdown ? (
            <div style={MD_TEXT_STYLE}><ReactMarkdown>{content.text}</ReactMarkdown></div>
        ) : (
            <span style={{ whiteSpace: 'pre-wrap' }}>{content.text}</span>
        );
    }

    if (isImageContent(content)) {
        const imageUrl = (content as UriContent).uri || (content as DataContent).uri;
        return <img src={imageUrl} alt="Content" style={{ maxWidth: '100%', maxHeight: '320px', borderRadius: 'var(--radius-small)' }} />;
    }

    return <pre style={CODE_STYLE}>{safeJson(content, prettifyJson)}</pre>;
};

const ToolSection = ({ tool }: { tool: ToolSectionVM }) => {
    const sectionStyle: CSSProperties = {
        width: '100%',
        boxSizing: 'border-box',
        border: `1px solid ${T_BORDER}`,
        background: T_TINT,
        borderRadius: 'var(--radius-large)',
        overflow: 'hidden',
    };
    const headStyle: CSSProperties = {
        display: 'flex',
        alignItems: 'center',
        gap: 'var(--spacing-s)',
        padding: 'var(--spacing-s) var(--spacing-m)',
        background: T_HEAD,
        fontFamily: 'var(--font-family-base)',
        fontSize: 'var(--font-size-200)',
        fontWeight: 'var(--font-weight-semibold)',
        color: T_ACCENT,
    };
    const titleStyle: CSSProperties = {
        flex: '1 1 auto',
        minWidth: 0,
        whiteSpace: 'nowrap',
        overflow: 'hidden',
        textOverflow: 'ellipsis',
    };
    const iconStyle: CSSProperties = {
        flex: 'none',
        display: 'inline-flex',
        alignItems: 'center',
        color: T_ACCENT,
    };
    const capStyle: CSSProperties = {
        padding: 'var(--spacing-s) var(--spacing-m) 0',
        background: T_TINT,
        borderTop: `1px solid ${T_BORDER}`,
        fontFamily: 'var(--font-family-base)',
        fontSize: 'var(--font-size-100)',
        fontWeight: 'var(--font-weight-semibold)',
        textTransform: 'uppercase',
        letterSpacing: '0.4px',
        color: T_ACCENT,
    };
    const bodyStyle: CSSProperties = {
        margin: 0,
        padding: 'var(--spacing-xxs) var(--spacing-m) var(--spacing-s)',
        background: T_TINT,
        fontFamily: 'var(--font-family-monospace)',
        fontSize: 'var(--font-size-200)',
        lineHeight: 'var(--line-height-200)',
        color: 'var(--neutral-foreground-2)',
        whiteSpace: 'pre-wrap',
        wordBreak: 'break-word',
    };
    return (
        <div style={sectionStyle}>
            <div style={headStyle}>
                <span style={titleStyle}>{`Tool call: ${tool.name}`}</span>
                <span style={iconStyle}><WrenchIcon /></span>
            </div>
            <div style={capStyle}>Input</div>
            <pre style={bodyStyle}>{tool.callText}</pre>
            {tool.hasResult && (
                <>
                    <div style={capStyle}>Output</div>
                    <pre style={bodyStyle}>{tool.resultText}</pre>
                </>
            )}
        </div>
    );
};

const Bubble = ({ bubble, me, first }: { bubble: BubbleVM; me: boolean; first: boolean }) => {
    const fill = me ? 'var(--teams-mine)' : 'var(--teams-other)';
    const hasTools = bubble.tools.length > 0;
    const hasText = bubble.text !== null;

    const msgStyle: CSSProperties = {
        maxWidth: hasTools ? '92%' : '82%',
        marginTop: 'var(--spacing-xxs)',
    };
    const bubbleStyle: CSSProperties = {
        position: 'relative',
        borderRadius: 'var(--radius-large)',
        color: 'var(--neutral-foreground-1)',
        wordBreak: 'break-word',
        background: fill,
        ...(hasTools
            ? { padding: 'var(--spacing-xs)', display: 'flex', flexDirection: 'column', gap: 'var(--spacing-xs)' }
            : { padding: 'var(--spacing-s-nudge) var(--spacing-m)', fontSize: 'var(--font-size-300)', lineHeight: 'var(--line-height-400)' }),
        ...(first ? (me ? { borderTopRightRadius: 'var(--radius-small)' } : { borderTopLeftRadius: 'var(--radius-small)' }) : {}),
    };
    const tailStyle: CSSProperties = {
        position: 'absolute',
        top: 0,
        ...(me ? { right: '-6px' } : { left: '-6px' }),
        width: '8px',
        height: '12px',
        background: fill,
        clipPath: me ? 'polygon(0 0, 0 100%, 100% 0)' : 'polygon(100% 0, 100% 100%, 0 0)',
    };
    const textStyle: CSSProperties | undefined = hasTools
        ? { padding: 'var(--spacing-xxs) var(--spacing-s)', fontSize: 'var(--font-size-300)', lineHeight: 'var(--line-height-400)' }
        : undefined;

    return (
        <div style={msgStyle}>
            <div style={bubbleStyle}>
                {first && <span style={tailStyle} aria-hidden="true" />}
                {bubble.tools.map((t, i) => (
                    <ToolSection key={`tool-${i}`} tool={t} />
                ))}
                {hasText && (
                    <div style={textStyle}><TextNode content={bubble.text as AIContent} /></div>
                )}
            </div>
        </div>
    );
};

const MessageGroup = ({ group, model, time }: { group: Extract<GroupVM, { kind: 'group' }>; model: string; time: string }) => {
    const me = group.role === 'user';
    const name = me ? 'You' : 'Assistant';
    const headTime = me ? time : `${model} · ${time}`;

    const gridStyle: CSSProperties = {
        display: 'grid',
        gridTemplateColumns: me ? '1fr 32px' : '32px 1fr',
        columnGap: 'var(--spacing-s)',
        marginTop: 'var(--spacing-l)',
    };
    const avStyle: CSSProperties = {
        ...AV_BASE,
        gridColumn: me ? 2 : 1,
        ...(me
            ? { background: 'var(--brand-background)' }
            : { background: 'linear-gradient(135deg, var(--palette-berry-foreground), var(--brand-background))' }),
    };
    const colStyle: CSSProperties = {
        gridRow: 1,
        gridColumn: me ? 1 : 2,
        minWidth: 0,
        display: 'flex',
        flexDirection: 'column',
        alignItems: me ? 'flex-end' : 'flex-start',
    };
    const headStyle: CSSProperties = {
        display: 'flex',
        gap: 'var(--spacing-s)',
        alignItems: 'baseline',
        margin: '0 var(--spacing-xs) var(--spacing-xxs)',
        ...(me ? { flexDirection: 'row-reverse' } : {}),
    };
    const nameStyle: CSSProperties = {
        fontSize: 'var(--font-size-200)',
        fontWeight: 'var(--font-weight-semibold)',
        color: 'var(--neutral-foreground-1)',
    };
    const timeStyle: CSSProperties = {
        fontSize: 'var(--font-size-100)',
        color: 'var(--neutral-foreground-3)',
        whiteSpace: 'nowrap',
    };

    return (
        <div style={gridStyle}>
            <div style={avStyle} aria-hidden="true">{me ? <PersonIcon /> : <SparkleIcon />}</div>
            <div style={colStyle}>
                <div style={headStyle}>
                    <span style={nameStyle}>{name}</span>
                    <span style={timeStyle}>{headTime}</span>
                </div>
                {group.bubbles.map((b, i) => (
                    <Bubble key={`b-${i}`} bubble={b} me={me} first={i === 0} />
                ))}
            </div>
        </div>
    );
};

const SystemGroup = ({ content }: { content: AIContent }) => {
    const wrapStyle: CSSProperties = {
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        marginTop: 'var(--spacing-l)',
    };
    const bubbleStyle: CSSProperties = {
        borderRadius: 'var(--radius-xlarge)',
        padding: 'var(--spacing-s) var(--spacing-xxl)',
        fontSize: 'var(--font-size-200)',
        lineHeight: 1.5,
        maxWidth: '88%',
        textAlign: 'center',
        color: 'var(--neutral-foreground-3)',
        background: 'var(--neutral-background-2)',
        border: '1px solid var(--neutral-stroke-1)',
        whiteSpace: 'pre-wrap',
        wordBreak: 'break-word',
    };
    return (
        <div style={wrapStyle}>
            <div style={bubbleStyle}><TextNode content={content} /></div>
        </div>
    );
};

export const TranscriptBlock = ({ messages, model: modelProp }: { messages: ChatMessageDisplay[]; model?: string }) => {
    const { dataset } = useReportContext();
    const groups = buildGroups(messages);
    const { time, date } = chatClock(dataset?.createdAt);

    // Prefer the wire model id (modelResponse.modelId, surfaced as conversation.model);
    // fall back to deriving it from the first assistant group's participant name (mockup chat header parity).
    const assistantGroup = groups.find((g): g is Extract<GroupVM, { kind: 'group' }> => g.kind === 'group' && g.role === 'assistant');
    const model = modelProp || (assistantGroup ? modelFromParticipant(assistantGroup.participantName) : '—');

    const cardStyle: CSSProperties = { margin: '-12px' };
    const headerRowStyle: CSSProperties = {
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        gap: 'var(--spacing-m-nudge)',
        padding: 'var(--spacing-m-nudge) var(--spacing-l)',
        borderBottom: '1px solid var(--neutral-stroke-3)',
    };
    const eyebrowStyle: CSSProperties = {
        fontSize: 'var(--font-size-100)',
        fontWeight: 'var(--font-weight-semibold)',
        color: 'var(--neutral-foreground-3)',
        textTransform: 'uppercase',
        letterSpacing: '0.4px',
    };
    const bodyStyle: CSSProperties = {
        padding: 'var(--spacing-xs) var(--spacing-l) var(--spacing-l)',
        display: 'flex',
        flexDirection: 'column',
    };
    const dividerStyle: CSSProperties = {
        display: 'flex',
        alignItems: 'center',
        gap: 'var(--spacing-m)',
        margin: 'var(--spacing-xs) 0 var(--spacing-s)',
        color: 'var(--neutral-foreground-3)',
    };
    const dividerLine: CSSProperties = { flex: 1, height: '1px', background: 'var(--neutral-stroke-2)' };
    const dividerLabel: CSSProperties = {
        fontSize: 'var(--font-size-100)',
        fontWeight: 'var(--font-weight-semibold)',
        letterSpacing: '0.4px',
        textTransform: 'uppercase',
    };
    const emptyStyle: CSSProperties = {
        fontSize: 'var(--font-size-200)',
        color: 'var(--neutral-foreground-3)',
        fontStyle: 'italic',
    };

    return (
        <Card appearance="outline" className="eval-transcript">
            <div style={cardStyle}>
                <div style={headerRowStyle}>
                    <span style={eyebrowStyle}>Transcript</span>
                </div>
                <div style={bodyStyle}>
                    {groups.length === 0 && <div style={emptyStyle}>No transcript for this case.</div>}
                    {groups.length > 0 && (
                        <div style={dividerStyle}>
                            <span style={dividerLine} />
                            <span style={dividerLabel}>{date}</span>
                            <span style={dividerLine} />
                        </div>
                    )}
                    {groups.map((group, index) =>
                        group.kind === 'system' ? (
                            <SystemGroup key={`grp-${index}`} content={group.content} />
                        ) : (
                            <MessageGroup key={`grp-${index}`} group={group} model={model} time={time} />
                        ),
                    )}
                </div>
            </div>
        </Card>
    );
};
