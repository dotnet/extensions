// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import React, { useState, useCallback } from "react";
import { makeStyles, tokens, Tree, TreeItem, TreeItemLayout, TreeItemValue, TreeOpenChangeData, TreeOpenChangeEvent, mergeClasses, Button, Divider, Table, TableBody, TableCell, TableHeader, TableHeaderCell, TableRow } from "@fluentui/react-components";
import { ScoreNode, ScoreNodeType, getPromptDetails, ChatMessageDisplay, ScoreSummary } from "./Summary";
import { PassFailBar } from "./PassFailBar";
import { MetricCardList, type MetricType } from "./MetricCard";
import ReactMarkdown from "react-markdown";
import { DismissCircle16Regular, Info16Regular, Warning16Regular, DataTrendingRegular } from "@fluentui/react-icons";
import { ChevronDown12Regular, ChevronRight12Regular } from '@fluentui/react-icons';

const ScenarioLevel = ({ node, parentPath, isOpen, renderMarkdown }: {
    node: ScoreNode,
    parentPath: string,
    isOpen: (path: string) => boolean,
    renderMarkdown: boolean,
}) => {
    const [showTrendsAtLevel, setShowTrendsAtLevel] = useState(false);

    const path = `${parentPath}.${node.name}`;
    if (node.isLeafNode) {
        return <TreeItem itemType="branch" value={path}>
            <TreeItemLayout>
                <ScoreNodeHeader item={node} showPrompt={!isOpen(path)} />
            </TreeItemLayout>
            <Tree>
                <TreeItem itemType="leaf" >
                    <TreeItemLayout>
                        <IterationScoreTrends scenario={node.scenario!} initiallyOpen={false} />
                        <ScoreDetail scenario={node.scenario!} renderMarkdown={renderMarkdown} />
                    </TreeItemLayout>
                </TreeItem>
            </Tree>
        </TreeItem>
    } else {
        return <TreeItem itemType="branch" value={path}>
            <TreeItemLayout actions={
                <Button aria-label="Trends" onClick={() => setShowTrendsAtLevel(!showTrendsAtLevel)}
                    appearance="subtle" icon={<DataTrendingRegular />} />
            }>
                <ScoreNodeHeader item={node} showPrompt={!isOpen(path)} />
                {showTrendsAtLevel && <IterationScoreTrends scenario={node.scenario!} initiallyOpen={true} />}
            </TreeItemLayout>
            <Tree>
                {node.childNodes.map((n) => (
                    <React.Fragment key={path + '.' + n.name}>
                        <ScenarioLevel node={n} key={path + '.' + n.name} parentPath={path} isOpen={isOpen} renderMarkdown={renderMarkdown} />
                    </React.Fragment>
                ))}
            </Tree>
        </TreeItem>;
    }
};

export const ScenarioGroup = ({ summaryResults, renderMarkdown }: { summaryResults: ScoreSummary, renderMarkdown: boolean }) => {
    const [openItems, setOpenItems] = useState<Set<TreeItemValue>>(() => new Set());
    const handleOpenChange = useCallback((_: TreeOpenChangeEvent, data: TreeOpenChangeData) => {
        setOpenItems(data.openItems);
    }, []);
    const isOpen = (name: string) => openItems.has(name);

    return (
        <Tree aria-label="Default" appearance="transparent" onOpenChange={handleOpenChange}
            defaultOpenItems={["." + summaryResults.primaryResult.name]}>
            <ScenarioLevel node={summaryResults.primaryResult} parentPath={""} isOpen={isOpen} renderMarkdown={renderMarkdown} />
        </Tree>);
};

export const ScoreDetail = ({ scenario, renderMarkdown }: { scenario: ScenarioRunResult, renderMarkdown: boolean }) => {
    const classes = useStyles();
    const [selectedMetric, setSelectedMetric] = useState<MetricType | null>(null);
    const { messages } = getPromptDetails(scenario.messages, scenario.modelResponse);

    return (<div className={classes.iterationArea}>
        <MetricCardList
            scenario={scenario}
            onMetricSelect={setSelectedMetric}
            selectedMetric={selectedMetric}
        />
        {selectedMetric && <MetricDetailsSection metric={selectedMetric} />}
        {messages.length > 0 && <PromptDetails messages={messages} renderMarkdown={renderMarkdown} />}
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

const useStyles = makeStyles({
    headerContainer: { display: 'flex', alignItems: 'center', flexDirection: 'row', gap: '0.5rem' },
    promptHint: { fontFamily: tokens.fontFamilyMonospace, opacity: 0.6, fontSize: '0.7rem', paddingLeft: '1rem', whiteSpace: 'nowrap' },
    score: { fontSize: tokens.fontSizeBase200 },
    passFailBadge: {
        display: 'flex',
        flexDirection: 'row',
        alignItems: 'center',
        padding: '0 0.25rem',
        borderRadius: '4px',
        backgroundColor: tokens.colorNeutralBackground3,
    },
    scenarioLabel: {
        whiteSpace: 'nowrap',
        fontWeight: '500',
        fontSize: tokens.fontSizeBase300,
        display: 'flex',
        gap: '0.5rem',
        alignItems: 'center',
    },
    separator: {
        color: tokens.colorNeutralForeground4,
        fontSize: tokens.fontSizeBase200,
        fontWeight: '300',
        padding: '0 0.125rem',
    },
    iterationArea: {
        marginTop: '1rem',
        marginBottom: '1rem',
    },
    section: {
        marginTop: '0.75rem',
        padding: '1rem',
        border: '2px solid ' + tokens.colorNeutralStroke1,
        borderRadius: '8px',
        right: '0',
    },
    sectionHeader: {
        display: 'flex',
        alignItems: 'center',
        cursor: 'pointer',
        userSelect: 'none',
    },
    sectionHeaderText: {
        margin: 0,
        marginLeft: '0.5rem',
        fontSize: tokens.fontSizeBase300,
        fontWeight: '500',
    },
    sectionSubHeader: {
        fontSize: tokens.fontSizeBase300,
        fontWeight: '500',
        marginBottom: '0.25rem',
    },
    sectionContent: {
        marginBottom: '0.75rem',
    },
    failMessage: {
        color: tokens.colorStatusDangerForeground2,
        marginBottom: '0.25rem',
    },
    warningMessage: {
        color: tokens.colorStatusWarningForeground2,
        marginBottom: '0.25rem',
    },
    infoMessage: {
        color: tokens.colorNeutralForeground1,
        marginBottom: '0.25rem',
    },
    failContainer: {
        padding: '1rem',
        border: '1px solid #e0e0e0',
        backgroundColor: tokens.colorNeutralBackground2,
        cursor: 'text',
    },
    sectionContainer: {
        display: 'flex',
        flexDirection: 'column',
        gap: '0.75rem',
        padding: '0.75rem 0',
        cursor: 'text',
        position: 'relative',
        '& pre': {
            whiteSpace: 'pre-wrap',
            wordWrap: 'break-word',
        },
    },
    messageRow: {
        display: 'flex',
        flexDirection: 'column',
        position: 'relative',
    },
    userMessageRow: {
        marginLeft: '0',
        marginRight: '10rem',
    },
    assistantMessageRow: {
        marginLeft: '10rem',
        marginRight: '0',
    },
    messageParticipantName: {
        fontSize: tokens.fontSizeBase200,
        marginBottom: '0.25rem',
        color: tokens.colorNeutralForeground3,
        paddingLeft: '0.5rem',
    },
    messageBubble: {
        padding: '0.75rem 1rem',
        borderRadius: '12px',
        overflow: 'hidden',
        wordBreak: 'break-word',
        backgroundColor: tokens.colorNeutralBackground3,
        border: '1px solid ' + tokens.colorNeutralStroke2,
    },
});

const PassFailBadge = ({ pass, total }: { pass: number, total: number }) => {
    const classes = useStyles();
    return (<div className={classes.passFailBadge}>
        <span className={classes.score}>{pass}/{total} [{((pass * 100) / total).toFixed(1)}%]</span>
    </div>);
};

const ScoreNodeHeader = (
    { item, showPrompt }:
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
        {showPrompt && item.shortenedPrompt && <div className={classes.promptHint}>{item.shortenedPrompt}</div>}
    </div>);
};

export const PromptDetails = ({ messages, renderMarkdown }: {
    messages: ChatMessageDisplay[],
    renderMarkdown: boolean
}) => {
    const classes = useStyles();
    const [isExpanded, setIsExpanded] = useState(true);

    const isUserSide = (role: string) => role.toLowerCase() === 'user' || role.toLowerCase() === 'system';

    return (
        <div className={classes.section}>
            <div className={classes.sectionHeader} onClick={() => setIsExpanded(!isExpanded)}>
                {isExpanded ? <ChevronDown12Regular /> : <ChevronRight12Regular />}
                <h3 className={classes.sectionHeaderText}>Conversation</h3>
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
                                        <pre style={{ whiteSpace: 'pre-wrap' }}>{message.content}</pre>
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

export const IterationScoreTrends = ({scenario, initiallyOpen }: { scenario: ScenarioRunResult, initiallyOpen: boolean }) => {
    const classes = useStyles();
    const [isExpanded, setIsExpanded] = useState(initiallyOpen);

    return (
        <div className={classes.section}>
            <div className={classes.sectionHeader} onClick={(evt) => {
                evt.stopPropagation();
                setIsExpanded(!isExpanded);
            }}>
                {isExpanded ? <ChevronDown12Regular /> : <ChevronRight12Regular />}
                <h3 className={classes.sectionHeaderText}>Data Trends</h3>
            </div>

            {isExpanded && (
                <div className={classes.sectionContainer}>
                    <Table>
                        <TableHeader>
                            <TableHeaderCell>{scenario.iterationName}</TableHeaderCell>
                        </TableHeader>
                        <TableBody>
                            <TableRow>
                                <TableCell>
                                    
                                </TableCell>
                            </TableRow>
                        </TableBody>
                    </Table>
                </div>
            )}
        </div>
    );
};
