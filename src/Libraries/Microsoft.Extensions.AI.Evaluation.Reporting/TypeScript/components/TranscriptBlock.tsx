// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { makeStyles, mergeClasses, tokens } from '@fluentui/react-components';
import { Wrench16Regular } from '@fluentui/react-icons';
import ReactMarkdown from 'react-markdown';
import { useReportStyles } from './reportStyles';
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

const TOOL_TINT = 'color-mix(in srgb, var(--palette-teal-foreground) 7%, var(--neutral-background-1))';
const TOOL_HEAD = 'color-mix(in srgb, var(--palette-teal-foreground) 14%, var(--neutral-background-1))';
const TOOL_BORDER = 'color-mix(in srgb, var(--palette-teal-foreground) 30%, var(--neutral-background-1))';

const useStyles = makeStyles({
    headerRow: {
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        gap: 'var(--spacing-m-nudge)',
        padding: 'var(--spacing-m-nudge) var(--spacing-l)',
        borderBottom: '1px solid var(--neutral-stroke-3)',
    },
    body: {
        padding: 'var(--spacing-xs) var(--spacing-l) var(--spacing-l)',
        display: 'flex',
        flexDirection: 'column',
        gap: 'var(--spacing-l)',
    },

    group: { display: 'flex', flexDirection: 'column', minWidth: 0 },
    userGroup: { alignItems: 'flex-end' },
    assistantGroup: { alignItems: 'flex-start' },
    systemGroup: { alignItems: 'center', textAlign: 'center' },

    eyebrowRow: {
        display: 'inline-flex',
        alignItems: 'center',
        gap: 'var(--spacing-s-nudge)',
        margin: '0 var(--spacing-xs) var(--spacing-xs)',
    },
    eyebrowText: {
        fontSize: 'var(--font-size-200)',
        fontWeight: 'var(--font-weight-semibold)',
        color: 'var(--neutral-foreground-3)',
        whiteSpace: 'nowrap',
    },
    eyebrowDot: {
        width: '6px',
        height: '6px',
        borderRadius: 'var(--radius-circular)',
        backgroundColor: 'var(--brand-background)',
        flex: 'none',
    },

    bubble: {
        position: 'relative',
        maxWidth: '82%',
        borderRadius: 'var(--radius-large)',
        color: 'var(--neutral-foreground-1)',
        fontSize: 'var(--font-size-300)',
        lineHeight: 'var(--line-height-400)',
        wordBreak: 'break-word',
        display: 'flex',
        flexDirection: 'column',
        gap: 'var(--spacing-s)',
        padding: 'var(--spacing-s-nudge) var(--spacing-m)',
    },
    bubbleWide: {
        maxWidth: '92%',
        padding: 'var(--spacing-xs)',
        gap: 'var(--spacing-xs)',
    },
    userBubble: {
        backgroundColor: 'var(--teams-mine)',
        borderTopRightRadius: 'var(--radius-small)',
    },
    assistantBubble: {
        backgroundColor: 'var(--teams-other)',
        borderTopLeftRadius: 'var(--radius-small)',
    },
    systemBubble: {
        maxWidth: '88%',
        backgroundColor: 'var(--neutral-background-2)',
        border: '1px solid var(--neutral-stroke-2)',
        borderRadius: 'var(--radius-xlarge)',
        color: 'var(--neutral-foreground-3)',
        fontSize: 'var(--font-size-200)',
        lineHeight: 1.5,
        padding: 'var(--spacing-s) var(--spacing-xxl)',
        whiteSpace: 'pre-wrap',
    },
    bubbleText: {
        padding: 'var(--spacing-xxs) var(--spacing-s)',
    },

    toolSection: {
        width: '100%',
        boxSizing: 'border-box',
        border: `1px solid ${TOOL_BORDER}`,
        backgroundColor: TOOL_TINT,
        borderRadius: 'var(--radius-large)',
        overflow: 'hidden',
    },
    toolHeader: {
        display: 'flex',
        alignItems: 'center',
        gap: 'var(--spacing-s)',
        padding: 'var(--spacing-s) var(--spacing-m)',
        backgroundColor: TOOL_HEAD,
        fontSize: 'var(--font-size-200)',
        fontWeight: 'var(--font-weight-semibold)',
        color: 'var(--tool-accent-text)',
    },
    toolIcon: {
        flex: 'none',
        display: 'inline-flex',
        alignItems: 'center',
        color: 'var(--tool-accent-text)',
    },
    toolCaptionInline: {
        flex: 'none',
        textTransform: 'uppercase',
        letterSpacing: '0.4px',
        fontSize: 'var(--font-size-100)',
    },
    toolName: {
        flex: '1 1 auto',
        minWidth: 0,
        fontFamily: 'var(--font-family-monospace)',
        whiteSpace: 'nowrap',
        overflow: 'hidden',
        textOverflow: 'ellipsis',
    },
    toolSubCaption: {
        padding: 'var(--spacing-s) var(--spacing-m) 0',
        borderTop: `1px solid ${TOOL_BORDER}`,
        fontSize: 'var(--font-size-100)',
        fontWeight: 'var(--font-weight-semibold)',
        textTransform: 'uppercase',
        letterSpacing: '0.4px',
        color: 'var(--tool-accent-text)',
    },
    toolBody: {
        margin: 0,
        padding: 'var(--spacing-xxs) var(--spacing-m) var(--spacing-s)',
        fontFamily: 'var(--font-family-monospace)',
        fontSize: 'var(--font-size-200)',
        lineHeight: 'var(--line-height-200)',
        color: 'var(--neutral-foreground-2)',
        whiteSpace: 'pre-wrap',
        wordBreak: 'break-word',
    },

    code: {
        margin: 0,
        fontFamily: 'var(--font-family-monospace)',
        fontSize: 'var(--font-size-200)',
        lineHeight: 'var(--line-height-200)',
        whiteSpace: 'pre-wrap',
        wordBreak: 'break-word',
        color: 'var(--neutral-foreground-1)',
    },
    text: {
        whiteSpace: 'pre-wrap',
        wordBreak: 'break-word',
        '& p': { margin: '0 0 0.5rem' },
        '& p:last-child': { margin: 0 },
        '& pre': { whiteSpace: 'pre-wrap', wordBreak: 'break-word' },
    },
    image: { maxWidth: '100%', maxHeight: '320px', borderRadius: tokens.borderRadiusSmall },
    empty: {
        fontSize: 'var(--font-size-200)',
        color: 'var(--neutral-foreground-3)',
        fontStyle: 'italic',
    },
});

type MessageGroup = { role: string; participantName: string; contents: AIContent[] };

const groupMessages = (messages: ChatMessageDisplay[]): MessageGroup[] => {
    const result: MessageGroup[] = [];
    for (const message of messages) {
        const last = result[result.length - 1];
        if (last && last.role === message.role && last.participantName === message.participantName) {
            last.contents.push(message.content);
        } else {
            result.push({ role: message.role, participantName: message.participantName, contents: [message.content] });
        }
    }
    return result;
};

const isUserSide = (role: string) => {
    const r = role.toLowerCase();
    return r === 'user';
};
const isSystemSide = (role: string) => role.toLowerCase() === 'system';

const safeJson = (value: unknown, pretty: boolean): string => {
    try {
        return JSON.stringify(value, null, pretty ? 2 : 0) ?? String(value);
    } catch {
        return String(value);
    }
};

const ToolSection = ({ caption, name, body, subCaption, subBody }: {
    caption: string;
    name: string;
    body: string;
    subCaption?: string;
    subBody?: string;
}) => {
    const classes = useStyles();
    return (
        <div className={classes.toolSection}>
            <div className={classes.toolHeader}>
                <Wrench16Regular className={classes.toolIcon} />
                <span className={classes.toolCaptionInline}>{caption}</span>
                <span className={classes.toolName}>{name}</span>
            </div>
            <pre className={classes.toolBody}>{body}</pre>
            {subCaption !== undefined && subBody !== undefined && (
                <>
                    <div className={classes.toolSubCaption}>{subCaption}</div>
                    <pre className={classes.toolBody}>{subBody}</pre>
                </>
            )}
        </div>
    );
};

const ContentBlock = ({ content }: { content: AIContent }) => {
    const classes = useStyles();
    const { renderMarkdown, prettifyJson } = useReportContext();

    if (isFunctionCall(content)) {
        const name = content.name ?? 'function';
        const args = content.arguments ?? {};
        return (
            <ToolSection
                caption="Tool call"
                name={name}
                body={safeJson(args, prettifyJson)}
            />
        );
    }

    if (isFunctionResult(content)) {
        return (
            <ToolSection
                caption="Tool result"
                name={content.callId ?? 'result'}
                body={safeJson(content.result ?? null, prettifyJson)}
            />
        );
    }

    if (isToolish(content)) {
        return (
            <ToolSection
                caption="Tool data"
                name={content.$type}
                body={safeJson(content, prettifyJson)}
            />
        );
    }

    if (isTextContent(content)) {
        const trimmed = content.text.trim();
        try {
            const parsed = JSON.parse(trimmed);
            return <pre className={classes.code}>{JSON.stringify(parsed, null, prettifyJson ? 2 : 0)}</pre>;
        } catch {
        }
        return renderMarkdown ? (
            <div className={classes.text}><ReactMarkdown>{content.text}</ReactMarkdown></div>
        ) : (
            <pre className={classes.code}>{content.text}</pre>
        );
    }

    if (isImageContent(content)) {
        const imageUrl = (content as UriContent).uri || (content as DataContent).uri;
        return <img src={imageUrl} alt="Content" className={classes.image} />;
    }

    return <pre className={classes.code}>{safeJson(content, prettifyJson)}</pre>;
};

const RoleEyebrow = ({ label, user }: { label: string; user: boolean }) => {
    const classes = useStyles();
    return (
        <div className={classes.eyebrowRow} style={user ? { flexDirection: 'row-reverse' } : undefined}>
            <span className={classes.eyebrowText}>{label}</span>
            {user && <span className={classes.eyebrowDot} aria-hidden="true" />}
        </div>
    );
};

export const TranscriptBlock = ({ messages }: { messages: ChatMessageDisplay[] }) => {
    const classes = useStyles();
    const s = useReportStyles();
    const groups = groupMessages(messages);

    return (
        <div className={s.cardNested}>
            <div className={classes.headerRow}>
                <span className={s.eyebrow}>Transcript</span>
            </div>
            <div className={classes.body}>
                {groups.length === 0 && <div className={classes.empty}>No transcript for this case.</div>}
                {groups.map((group, index) => {
                    const system = isSystemSide(group.role);
                    const user = isUserSide(group.role);
                    const hasTools = group.contents.some(
                        (c) => isFunctionCall(c) || isFunctionResult(c) || isToolish(c),
                    );
                    const groupClass = mergeClasses(
                        classes.group,
                        system ? classes.systemGroup : user ? classes.userGroup : classes.assistantGroup,
                    );
                    const bubbleClass = mergeClasses(
                        classes.bubble,
                        hasTools && !system && classes.bubbleWide,
                        system ? classes.systemBubble : user ? classes.userBubble : classes.assistantBubble,
                    );
                    return (
                        <div key={`grp-${index}`} className={groupClass}>
                            {!system && <RoleEyebrow label={group.participantName} user={user} />}
                            <div className={bubbleClass}>
                                {group.contents.map((content, ci) => {
                                    const isTool = isFunctionCall(content) || isFunctionResult(content) || isToolish(content);
                                    if (hasTools && !system && !isTool) {
                                        return (
                                            <div key={ci} className={classes.bubbleText}>
                                                <ContentBlock content={content} />
                                            </div>
                                        );
                                    }
                                    return <ContentBlock key={ci} content={content} />;
                                })}
                            </div>
                        </div>
                    );
                })}
            </div>
        </div>
    );
};
