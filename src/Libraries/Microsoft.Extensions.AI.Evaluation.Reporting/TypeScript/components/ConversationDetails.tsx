import { mergeClasses } from "@fluentui/react-components";
import { ChevronDown12Regular, ChevronRight12Regular } from "@fluentui/react-icons";
import { useState } from "react";
import ReactMarkdown from "react-markdown";
import { useReportContext } from "./ReportContext";
import { useStyles } from "./Styles";
import { ChatMessageDisplay } from "./Summary";


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

    return (
        <div className={classes.section}>
            <div className={classes.sectionHeader} onClick={() => setIsExpanded(!isExpanded)}>
                {isExpanded ? <ChevronDown12Regular /> : <ChevronRight12Regular />}
                <h3 className={classes.sectionHeaderText}>Conversation</h3>
                {infoText && <div className={classes.hint}>{infoText}</div>}
            </div>

            {isExpanded && (
                <div className={classes.sectionContainer}>
                    {messages.map((message, index) => {
                        const isFromUserSide = isUserSide(message.role);
                        const messageRowClass = mergeClasses(
                            classes.messageRow,
                            isFromUserSide ? classes.userMessageRow : classes.assistantMessageRow
                        );

                        return (
                            <div key={index} className={messageRowClass}>
                                <div className={classes.messageParticipantName}>{message.participantName}</div>
                                <div className={classes.messageBubble}>
                                    {renderMarkdown ?
                                        <ReactMarkdown>{message.content}</ReactMarkdown> :
                                        <pre className={classes.preWrap}>{message.content}</pre>}
                                </div>
                            </div>
                        );
                    })}
                </div>
            )}
        </div>
    );
};
