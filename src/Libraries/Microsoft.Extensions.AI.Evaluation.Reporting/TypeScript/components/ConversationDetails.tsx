// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mergeClasses } from "@fluentui/react-components";
import { ChevronDown12Regular, ChevronRight12Regular } from "@fluentui/react-icons";
import { useState } from "react";
import ReactMarkdown from "react-markdown";
import { useReportContext } from "./ReportContext";
import { useStyles } from "./Styles";
import { ChatMessageDisplay, isTextContent, isImageContent } from "./Summary";

export const ConversationDetails = ({ messages, model, usage }: {
    messages: ChatMessageDisplay[];
    model?: string;
    usage?: UsageDetails;
}) => {
    const classes = useStyles();
    const [isExpanded, setIsExpanded] = useState(true);
    const { renderMarkdown } = useReportContext();

    const isUserSide = (role: string) => role.toLowerCase() === 'user' || role.toLowerCase() === 'system';

    const infoText = [
        model && `Model: ${model}`,
        usage?.inputTokenCount && `Input Tokens: ${usage.inputTokenCount}`,
        usage?.outputTokenCount && `Output Tokens: ${usage.outputTokenCount}`,
        usage?.totalTokenCount && `Total Tokens: ${usage.totalTokenCount}`,
    ].filter(Boolean).join(' • ');

    const renderContent = (content: AIContent) => {
        if (isTextContent(content)) {
            return renderMarkdown ?
                <ReactMarkdown>{content.text}</ReactMarkdown> :
                <pre className={classes.preWrap}>{content.text}</pre>;
        } else if (isImageContent(content)) {
            const imageUrl = (content as UriContent).uri || (content as DataContent).uri;
            return <img src={imageUrl} alt="Content" className={classes.imageContent} />;
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

    return (
        <div className={classes.section}>
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
                            <div key={index} className={messageRowClass}>
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
                </div>
            )}
        </div>
    );
};
