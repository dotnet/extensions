// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mergeClasses } from "@fluentui/react-components";
import { ChevronDown12Regular, ChevronRight12Regular } from "@fluentui/react-icons";
import { useState } from "react";
import ReactMarkdown from "react-markdown";
import { useReportContext } from "./ReportContext";
import { useStyles } from "./Styles";
import { ChatMessageDisplay, isTextContent, isImageContent } from "./Summary";
import type { MetricType } from "./MetricCard";

export const ConversationDetails = ({ messages, model, usage, selectedMetric }: {
    messages: ChatMessageDisplay[];
    model?: string;
    usage?: UsageDetails;
    selectedMetric?: MetricType | null;
}) => {
    const classes = useStyles();
    const [isExpanded, setIsExpanded] = useState(true);
    const { renderMarkdown, prettifyJson } = useReportContext();

    const isUserSide = (role: string) => role.toLowerCase() === 'user' || role.toLowerCase() === 'system';

    const infoText = [
        model && `Model: ${model}`,
        usage?.inputTokenCount && `Input Tokens: ${usage.inputTokenCount}`,
        usage?.outputTokenCount && `Output Tokens: ${usage.outputTokenCount}`,
        usage?.totalTokenCount && `Total Tokens: ${usage.totalTokenCount}`,
    ].filter(Boolean).join(' • ');

    const isValidJson = (text: string): { isValid: boolean; parsedJson?: any } => {
        try {
            const parsedJson = JSON.parse(text.trim());
            return { isValid: true, parsedJson };
        } catch {
            return { isValid: false };
        }
    };

    const renderContent = (content: AIContent) => {
        if (isTextContent(content)) {
            const { isValid, parsedJson } = isValidJson(content.text);
            if (isValid) {
                const jsonContent = JSON.stringify(parsedJson, null, prettifyJson ? 2 : 0);
                return <pre className={classes.preWrap}>{jsonContent}</pre>;
            } else {
                return renderMarkdown ?
                    <ReactMarkdown>{content.text}</ReactMarkdown> :
                    <pre className={classes.preWrap}>{content.text}</pre>;
            }
        } else if (isImageContent(content)) {
            const imageUrl = (content as UriContent).uri || (content as DataContent).uri;
            return <img src={imageUrl} alt="Content" className={classes.imageContent} />;
        } else {
            // For any other content type, display the serialized JSON
            const jsonContent = JSON.stringify(content, null, prettifyJson ? 2 : 0);
            return <pre className={classes.preWrap}>{jsonContent}</pre>;
        }
    };

    const groupMessages = () => {
        const result: { role: string, participantName: string, contents: AIContent[] }[] = [];

        for (const message of messages) {
            // If this message has the same role and participant as the previous one, append its content
            const lastGroup = result[result.length - 1];
            if (lastGroup && lastGroup.role === message.role && lastGroup.participantName === message.participantName) {
                lastGroup.contents.push(message.content);
            } else {
                // Otherwise, start a new group
                result.push({
                    role: message.role,
                    participantName: message.participantName,
                    contents: [message.content]
                });
            }
        }

        return result;
    };

    const messageGroups = groupMessages();
    const contextGroups = selectedMetric?.context ? Object.values(selectedMetric.context) : [];

    return (
        <div className={classes.section} tabIndex={0}
            onKeyUp={e => e.key === 'Enter' && setIsExpanded(!isExpanded)}>
            <div className={classes.sectionHeader} onClick={() => setIsExpanded(!isExpanded)}>
                {isExpanded ? <ChevronDown12Regular /> : <ChevronRight12Regular />}
                <h3 className={classes.sectionHeaderText}>Conversation</h3>
                {infoText && <div className={classes.hint}>{infoText}</div>}
            </div>

            {isExpanded && (
                <div className={classes.sectionContainer}>
                    {messageGroups.map((group, index) => {
                        const isFromUserSide = isUserSide(group.role);
                        const messageRowClass = mergeClasses(
                            classes.messageRow,
                            isFromUserSide ? classes.userMessageRow : classes.assistantMessageRow
                        );

                        return (
                            <div key={`msg-${index}`} className={messageRowClass}>
                                <div className={classes.messageParticipantName}>{group.participantName}</div>
                                <div className={classes.messageBubble}>
                                    {group.contents.map((content, contentIndex) => (
                                        <div key={contentIndex}>
                                            {renderContent(content)}
                                        </div>
                                    ))}
                                </div>
                            </div>
                        );
                    })}

                    {contextGroups.map((group, index) => (
                        <div key={`context-${index}`} className={mergeClasses(classes.messageRow, classes.userMessageRow)}>
                            <div className={classes.messageParticipantName}>{`supplied ${group.name.toLowerCase()}`}</div>
                            <div className={classes.contextBubble}>
                                {group.contents.map((content, contentIndex) => (
                                    <div key={contentIndex}>
                                        {renderContent(content)}
                                    </div>
                                ))}
                            </div>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
};
