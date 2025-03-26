// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import React, { useState, useCallback } from "react";
import { makeStyles, tokens, Tree, TreeItem, TreeItemLayout, TreeItemValue, TreeOpenChangeData, TreeOpenChangeEvent, mergeClasses, Table, TableHeader, TableRow, TableHeaderCell, TableBody, TableCell, TableCellLayout, Button } from "@fluentui/react-components";
import { ScoreNode, ScoreNodeType, getConversationDisplay, ChatMessageDisplay, ScoreSummary, getScoreHistory } from "./Summary";
import { PassFailBar } from "./PassFailBar";
import { MetricCardList, MetricDisplay, type MetricType } from "./MetricCard";
import ReactMarkdown from "react-markdown";
import { DismissCircle16Regular, Info16Regular, Warning16Regular, CheckmarkCircle16Regular, Copy16Regular, RadioButtonFilled, RadioButtonRegular } from "@fluentui/react-icons";
import { ChevronDown12Regular, ChevronRight12Regular } from '@fluentui/react-icons';
import { useReportContext } from "./ReportContext";
import { useStyles } from "./Styles";

const ScenarioLevel = ({ node, scoreSummary, isOpen }: {
    node: ScoreNode,
    scoreSummary: ScoreSummary,
    isOpen: (path: string) => boolean,
}) => {
    if (node.isLeafNode) {
        return <TreeItem itemType="branch" value={node.nodeKey}>
            <TreeItemLayout>
                <ScoreNodeHeader item={node} showPrompt={!isOpen(node.nodeKey)} />
            </TreeItemLayout>
            <Tree>
                <TreeItem itemType="leaf" >
                    <TreeItemLayout>
                        <ScoreDetail scenario={node.scenario!} scoreSummary={scoreSummary} />
                    </TreeItemLayout>
                </TreeItem>
            </Tree>
        </TreeItem>
    } else {
        return <TreeItem itemType="branch" value={node.nodeKey}>
            <TreeItemLayout>
                <ScoreNodeHeader item={node} showPrompt={!isOpen(node.nodeKey)} />
            </TreeItemLayout>
            <Tree>
                {node.childNodes.map((n) => (
                    <ScenarioLevel key={n.nodeKey} node={n} scoreSummary={scoreSummary} isOpen={isOpen} />
                ))}
            </Tree>
        </TreeItem>;
    }
};

export const ScenarioGroup = ({ node, scoreSummary, selectedTags }: {
    node: ScoreNode,
    scoreSummary: ScoreSummary,
    selectedTags: string[]
}) => {
    const [openItems, setOpenItems] = useState<Set<TreeItemValue>>(() => new Set());
    const handleOpenChange = useCallback((_: TreeOpenChangeEvent, data: TreeOpenChangeData) => {
        setOpenItems(data.openItems);
    }, []);
    const isOpen = (name: string) => openItems.has(name);

    const filterTree = (node: ScoreNode): ScoreNode | null => {
        if (selectedTags.length === 0) {
            return node;
        }

        if (node.isLeafNode) {
            return node.scenario?.tags?.some(tag => selectedTags.includes(tag)) ? node : null;
        }

        const filteredChildren = node.childNodes
            .map(filterTree)
            .filter((child): child is ScoreNode => child !== null);

        if (filteredChildren.length > 0) {
            const newNode = new ScoreNode(node.name, node.nodeType, node.nodeKey, node.executionName);
            newNode.setChildren(new Map(filteredChildren.map(child => [child.name, child])));
            newNode.aggregate(selectedTags);
            return newNode;
        }

        return null;
    };

    const filteredNode = filterTree(node);

    if (!filteredNode) {
        return <div>No results match the selected tags.</div>;
    }

    return (
        <Tree aria-label="Default" appearance="transparent" onOpenChange={handleOpenChange} defaultOpenItems={[filteredNode.nodeKey]}>
            <ScenarioLevel node={filteredNode} scoreSummary={scoreSummary} isOpen={isOpen} />
        </Tree>
    );
};

export const ScoreDetail = ({ scenario, scoreSummary }: { scenario: ScenarioRunResult, scoreSummary: ScoreSummary }) => {
    const classes = useStyles();
    const [selectedMetric, setSelectedMetric] = useState<MetricType | null>(null);
    const { messages, model, usage } = getConversationDisplay(scenario.messages, scenario.modelResponse);

    return (<div className={classes.iterationArea}>
        <ScoreHistory scoreSummary={scoreSummary} scenario={scenario} />
        <MetricCardList
            scenario={scenario}
            onMetricSelect={setSelectedMetric}
            selectedMetric={selectedMetric}
        />
        {selectedMetric && <MetricDetailsSection metric={selectedMetric} />}
        <ConversationDetails messages={messages} model={model} usage={usage} />
        {scenario.chatDetails && <ChatDetailsSection chatDetails={scenario.chatDetails} />}
    </div>);
};

export const MetricDetailsSection = ({ metric }: { metric: MetricType }) => {
    const classes = useStyles();
    const [isExpanded, setIsExpanded] = useState(true);

    const reason = metric.reason;
    const hasReason = reason != null;
    const interpretationReason = metric.interpretation?.reason;
    const hasInterpretationReason = interpretationReason != null;
    const diagnostics = metric.diagnostics || [];
    const hasDiagnostics = diagnostics.length > 0;

    if (!hasReason && !hasInterpretationReason && !hasDiagnostics) return null;

    return (
        <div className={classes.section}>
            <div className={classes.sectionHeader} onClick={() => setIsExpanded(!isExpanded)}>
                {isExpanded ? <ChevronDown12Regular /> : <ChevronRight12Regular />}
                <h3 className={classes.sectionHeaderText}>Metric Details: {metric.name}</h3>
            </div>

            {isExpanded && (
                <div className={classes.sectionContainer}>
                    {hasReason && (
                        <div className={classes.sectionContent}>
                            <div className={classes.sectionSubHeader}>Evaluation Reason</div>
                            <div>
                                <span>{reason}</span>
                            </div>
                        </div>
                    )}

                    {hasInterpretationReason && (
                        <div className={classes.sectionContent}>
                            {metric.interpretation?.failed ?
                                <div className={classes.sectionSubHeader}>Failure Reason</div> :
                                <div className={classes.sectionSubHeader}>Interpretation Reason</div>
                            }
                            <div>
                                {metric.interpretation?.failed ?
                                    <span className={classes.failMessage}><DismissCircle16Regular /> {interpretationReason}</span> :
                                    <span>{interpretationReason}</span>
                                }
                            </div>
                        </div>
                    )}

                    {hasDiagnostics && (
                        <div>
                            <div className={classes.sectionSubHeader}>Diagnostics</div>
                            <DiagnosticsContent diagnostics={diagnostics} />
                        </div>
                    )}
                </div>
            )}
        </div>
    );
};

const DiagnosticsContent = ({ diagnostics }: { diagnostics: EvaluationDiagnostic[] }) => {
    const classes = useStyles();

    const errorDiagnostics = diagnostics.filter(d => d.severity === "error");
    const warningDiagnostics = diagnostics.filter(d => d.severity === "warning");
    const infoDiagnostics = diagnostics.filter(d => d.severity === "informational");

    return (
        <>
            {errorDiagnostics.map((diag, index) => (
                <div key={`error-${index}`} className={classes.failMessage}>
                    <DismissCircle16Regular /> {diag.message}
                </div>
            ))}
            {warningDiagnostics.map((diag, index) => (
                <div key={`warning-${index}`} className={classes.warningMessage}>
                    <Warning16Regular /> {diag.message}
                </div>
            ))}
            {infoDiagnostics.map((diag, index) => (
                <div key={`info-${index}`} className={classes.infoMessage}>
                    <Info16Regular /> {diag.message}
                </div>
            ))}
        </>
    );
};

export const PassFailBadge = ({ pass, total }: { pass: number, total: number }) => {
    const classes = useStyles();
    return (<div className={classes.passFailBadge}>
        <span className={classes.score}>{pass}/{total} [{((pass * 100) / total).toFixed(1)}%]</span>
    </div>);
};

export const SelectionButton = ({ nodeKey }: { nodeKey: string }) => {
    const {selectScenarioLevel, selectedScenarioLevel} = useReportContext();
    return (
        <Button
            appearance="transparent"
            icon={nodeKey === selectedScenarioLevel ? <RadioButtonFilled /> : <RadioButtonRegular />}
            onClick={(evt) => {
                evt.stopPropagation();
                selectScenarioLevel(nodeKey);
            }}
            aria-label="Select"
        />
    );
}

const ScoreNodeHeader = ({ item, showPrompt }:
    {
        item: ScoreNode,
        showPrompt?: boolean,
    }) => {

    const classes = useStyles();
    let ctPass, ctFail;
    switch (item.nodeType) {
        case ScoreNodeType.Group:
            ctPass = item.numPassingIterations;
            ctFail = item.numFailingIterations;
            break;
        case ScoreNodeType.Scenario:
            ctPass = item.numPassingIterations;
            ctFail = item.numFailingIterations;
            break;
        case ScoreNodeType.Iteration:
            ctPass = item.failed ? 0 : 1;
            ctFail = item.failed ? 1 : 0;
            break;
    }

    const parts = item.name.split(' / ');

    return (<div className={classes.headerContainer}>
        <SelectionButton nodeKey={item.nodeKey}  />
        <PassFailBar pass={ctPass} total={ctPass + ctFail} width="24px" height="12px" />
        <div className={classes.scenarioLabel}>
            {parts.map((part, index) => (
                <React.Fragment key={`${part}-${index}`}>
                    {part}
                    {index < parts.length - 1 && <span className={classes.separator}>/</span>}
                </React.Fragment>
            ))}
        </div>
        <PassFailBadge pass={ctPass} total={ctPass + ctFail} />
        {showPrompt && item.shortenedPrompt && <div className={classes.hint}>{item.shortenedPrompt}</div>}
    </div>);
};

export const ConversationDetails = ({ messages, model, usage }: {
    messages: ChatMessageDisplay[],
    model?: string,
    usage?: UsageDetails,
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
                                        <pre className={classes.preWrap}>{message.content}</pre>
                                    }
                                </div>
                            </div>
                        );
                    })}
                </div>
            )}
        </div>
    );
};

export const ChatDetailsSection = ({ chatDetails }: { chatDetails: ChatDetails }) => {
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
                            <CheckmarkCircle16Regular className={classes.cacheHitIcon} />
                        }
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
                                                    </span>
                                                }
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



export const ScoreHistory = ({ scoreSummary, scenario }: { scoreSummary: ScoreSummary, scenario: ScenarioRunResult }) => {
    const classes = useStyles();
    const [isExpanded, setIsExpanded] = useState(false);

    if (!scoreSummary.executionHistory || scoreSummary.executionHistory.size === 0 ||
        (scoreSummary.executionHistory.size === 1 && [...scoreSummary.executionHistory.keys()][0] == scenario.executionName)) {
        return null;
    }

    const scoreHistory = getScoreHistory(scoreSummary, scenario);
    const executions = [...scoreSummary.executionHistory.keys()].reverse();
    const metrics = [...Object.keys(scenario.evaluationResult.metrics)];

    const getMetricDisplay = (execution: string, metric: string) => {
        const scenarioResult = scoreHistory.get(execution);
        if (!scenarioResult) return null;
        const metricResult = scenarioResult.evaluationResult.metrics[metric];
        if (!metricResult) return null;
        return (<MetricDisplay metric={metricResult} />);
    };

    const latestExecution = mergeClasses(classes.verticalText, classes.currentExecutionForeground);

    return (
        <div className={classes.section}>
            <div className={classes.sectionHeader} onClick={() => setIsExpanded(!isExpanded)}>
                {isExpanded ? <ChevronDown12Regular /> : <ChevronRight12Regular />}
                <h3 className={classes.sectionHeaderText}>Trends</h3>
            </div>

            {isExpanded && (
                <div className={classes.sectionContainer}>
                    <div className={classes.tableContainer}>
                        <Table>
                            <TableHeader>
                                <TableRow>
                                    {executions.map((execution) => (
                                        <TableHeaderCell key={execution}
                                            className={execution == scenario.executionName ? classes.currentExecutionBackground : undefined}>
                                            <div className={classes.executionHeaderCell}>
                                                <span className={execution == scenario.executionName ? latestExecution : classes.verticalText}>{execution}</span>
                                            </div>
                                        </TableHeaderCell>
                                    ))}
                                    <TableHeaderCell className={classes.currentExecutionBackground}></TableHeaderCell>
                                </TableRow>
                            </TableHeader>
                            <TableBody>
                                {metrics.map((metric) => (
                                    <TableRow key={metric}>
                                        {executions.map((execution) => (
                                            <TableCell key={execution}
                                                className={execution == scenario.executionName ? classes.currentExecutionBackground : undefined}>
                                                {getMetricDisplay(execution, metric)}
                                            </TableCell>
                                        ))}
                                        <TableCell className={classes.currentExecutionBackground}>
                                            <TableCellLayout className={classes.historyMetricCell}>{metric}</TableCellLayout>
                                        </TableCell>
                                    </TableRow>
                                ))}
                            </TableBody>
                        </Table>
                    </div>
                </div>
            )}
        </div>);
};
