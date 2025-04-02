import { Table, TableHeader, TableRow, TableHeaderCell, TableBody, TableCell } from "@fluentui/react-components";
import { ChevronDown12Regular, ChevronRight12Regular, Warning16Regular, CheckmarkCircle16Regular, Copy16Regular } from "@fluentui/react-icons";
import { useState } from "react";
import { useStyles } from "./Styles";


export const ChatDetailsSection = ({ chatDetails }: { chatDetails: ChatDetails; }) => {
    const classes = useStyles();
    const [isExpanded, setIsExpanded] = useState(false);

    const totalTurns = chatDetails.turnDetails.length;
    const cachedTurns = chatDetails.turnDetails.filter(turn => turn.cacheHit === true).length;

    const hasCacheKey = chatDetails.turnDetails.some(turn => turn.cacheKey !== undefined);
    const hasCacheStatus = chatDetails.turnDetails.some(turn => turn.cacheHit !== undefined);
    const hasModelInfo = chatDetails.turnDetails.some(turn => turn.model !== undefined);
    const hasInputTokens = chatDetails.turnDetails.some(turn => turn.usage?.inputTokenCount !== undefined);
    const hasOutputTokens = chatDetails.turnDetails.some(turn => turn.usage?.outputTokenCount !== undefined);
    const hasTotalTokens = chatDetails.turnDetails.some(turn => turn.usage?.totalTokenCount !== undefined);

    const copyToClipboard = (text: string) => {
        navigator.clipboard.writeText(text);
    };
    return (
        <div className={classes.section}>
            <div className={classes.sectionHeader} onClick={() => setIsExpanded(!isExpanded)}>
                {isExpanded ? <ChevronDown12Regular /> : <ChevronRight12Regular />}
                <h3 className={classes.sectionHeaderText}>LLM Chat Diagnostic Details</h3>
                {hasCacheStatus && (
                    <div className={classes.hint}>
                        {cachedTurns != totalTurns ?
                            <Warning16Regular className={classes.cacheMissIcon} /> :
                            <CheckmarkCircle16Regular className={classes.cacheHitIcon} />}
                        {cachedTurns}/{totalTurns} chat responses for this evaluation were fulfiled from cache
                    </div>
                )}
            </div>

            {isExpanded && (
                <div className={classes.sectionContainer}>
                    <div className={classes.tableContainer}>
                        <Table>
                            <TableHeader>
                                <TableRow>
                                    {hasCacheKey && <TableHeaderCell>Cache Key</TableHeaderCell>}
                                    {hasCacheStatus && <TableHeaderCell>Cache Status</TableHeaderCell>}
                                    <TableHeaderCell>Latency (s)</TableHeaderCell>
                                    {hasModelInfo && <TableHeaderCell>Model Used</TableHeaderCell>}
                                    {hasInputTokens && <TableHeaderCell>Input Tokens</TableHeaderCell>}
                                    {hasOutputTokens && <TableHeaderCell>Output Tokens</TableHeaderCell>}
                                    {hasTotalTokens && <TableHeaderCell>Total Tokens</TableHeaderCell>}
                                </TableRow>
                            </TableHeader>
                            <TableBody>
                                {chatDetails.turnDetails.map((turn, index) => (
                                    <TableRow key={index}>
                                        {hasCacheKey && (
                                            <TableCell className={classes.cacheKeyCell}>
                                                {turn.cacheKey ? (
                                                    <div className={classes.cacheKeyContainer} title={turn.cacheKey}>
                                                        <span className={classes.cacheKey}>
                                                            {turn.cacheKey.substring(0, 8)}...
                                                        </span>
                                                        <button
                                                            className={classes.copyButton}
                                                            onClick={(e) => {
                                                                e.stopPropagation();
                                                                copyToClipboard(turn.cacheKey || "");
                                                            }}
                                                            title="Copy Cache Key"
                                                        >
                                                            <Copy16Regular />
                                                        </button>
                                                    </div>
                                                ) : (
                                                    <span className={classes.noCacheKey}>N/A</span>
                                                )}
                                            </TableCell>
                                        )}
                                        {hasCacheStatus && (
                                            <TableCell>
                                                {turn.cacheHit === true ?
                                                    <span className={classes.cacheHit}>
                                                        <CheckmarkCircle16Regular className={classes.cacheHitIcon} /> Hit
                                                    </span> :
                                                    <span className={classes.cacheMiss}>
                                                        <Warning16Regular className={classes.cacheMissIcon} /> Miss
                                                    </span>}
                                            </TableCell>
                                        )}
                                        <TableCell>{turn.latency.toFixed(2)}</TableCell>
                                        {hasModelInfo && <TableCell>{turn.model || '-'}</TableCell>}
                                        {hasInputTokens && <TableCell>{turn.usage?.inputTokenCount || '-'}</TableCell>}
                                        {hasOutputTokens && <TableCell>{turn.usage?.outputTokenCount || '-'}</TableCell>}
                                        {hasTotalTokens && <TableCell>{turn.usage?.totalTokenCount || '-'}</TableCell>}
                                    </TableRow>
                                ))}
                            </TableBody>
                        </Table>
                    </div>
                </div>
            )}
        </div>
    );
};
