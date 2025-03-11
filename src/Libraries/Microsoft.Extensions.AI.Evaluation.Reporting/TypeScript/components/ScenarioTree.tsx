// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { makeStyles, tokens, Tree, TreeItem, TreeItemLayout, TreeItemValue, TreeOpenChangeData, TreeOpenChangeEvent } from "@fluentui/react-components";
import { useState, useCallback } from "react";
import { DefaultRootNodeName, ScoreNode, ScoreNodeType, getPromptDetails } from "./Summary";
import { PassFailBar } from "./PassFailBar";
import { MetricCardList, type MetricType } from "./MetricCard";
import ReactMarkdown from "react-markdown";
import { DismissCircle16Regular, Info16Regular, Warning16Regular } from "@fluentui/react-icons";
import { ChevronDown12Regular, ChevronRight12Regular } from '@fluentui/react-icons';

const ScenarioLevel = ({ node, parentPath, isOpen, renderMarkdown }: { 
  node: ScoreNode, 
  parentPath: string, 
  isOpen: (path: string) => boolean, 
  renderMarkdown: boolean,
}) => {
    node.collapseSingleChildNodes();
    const path = `${parentPath}.${node.name}`;
    if (node.isLeafNode) {
        return <TreeItem itemType="branch" value={path}>
            <TreeItemLayout>
                <ScoreNodeHeader item={node} showPrompt={!isOpen(path)}/>
            </TreeItemLayout>
            <Tree>
                <TreeItem itemType="leaf" >
                    <TreeItemLayout>
                        <ScoreDetail scenario={node.scenario!} renderMarkdown={renderMarkdown}/>
                    </TreeItemLayout>
                </TreeItem>
            </Tree>
        </TreeItem>
    } else {
        return <TreeItem itemType="branch" value={path}>
            <TreeItemLayout>
                <ScoreNodeHeader item={node} showPrompt={!isOpen(path)}/>
            </TreeItemLayout>
            <Tree>
                {node.childNodes.map((n) => (
                    <ScenarioLevel node={n} key={n.name} parentPath={path} isOpen={isOpen} renderMarkdown={renderMarkdown}/>
                ))}
            </Tree>
        </TreeItem>;
    }
};

export const ScenarioGroup = ({ node, renderMarkdown }: { node: ScoreNode, renderMarkdown: boolean }) => {
    const [openItems, setOpenItems] = useState<Set<TreeItemValue>>(() => new Set());
    const handleOpenChange = useCallback((_: TreeOpenChangeEvent, data: TreeOpenChangeData) => {
        setOpenItems(data.openItems);
    }, []);
    const isOpen = (name: string) => openItems.has(name);

    return (
        <Tree aria-label="Default" appearance="transparent" onOpenChange={handleOpenChange} defaultOpenItems={["." + DefaultRootNodeName]}>
            <ScenarioLevel node={node} parentPath={""} isOpen={isOpen} renderMarkdown={renderMarkdown} />
        </Tree>);        
};

export const ScoreDetail = ({ scenario, renderMarkdown }: { scenario: ScenarioRunResult, renderMarkdown: boolean }) => {
    const classes = useStyles();
    const [selectedMetric, setSelectedMetric] = useState<MetricType | null>(null);

    const failureMessages = [];
    for (const e of Object.values(scenario.evaluationResult.metrics)) {
        if (e.interpretation && e.interpretation.failed) {
            failureMessages.push(e.interpretation.reason || "Metric failed.");
        }
        for (const d of e.diagnostics) {
            if (d.severity === "error") {
                failureMessages.push(d.message);
            }
        }
    }
    const {history, response} = getPromptDetails(scenario.messages, scenario.modelResponse);

    return (<div className={classes.iterationArea}>
        <MetricCardList
          scenario={scenario}
          onMetricSelect={setSelectedMetric}
          selectedMetric={selectedMetric}
        />
        {selectedMetric && <MetricDetailsSection metric={selectedMetric} />}
        {failureMessages && failureMessages.length > 0 && <FailMessage messages={failureMessages} />}
        <PromptDetails history={history} response={response} renderMarkdown={renderMarkdown} />
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
                <div className={classes.conversationBox}>
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
    },
    iterationArea: {
        marginTop: '1rem',
        marginBottom: '1rem',
    },
    section: {
        marginTop: '1rem',
    },
    sectionHeader: {
        display: 'flex',
        alignItems: 'center',
        cursor: 'pointer',
        userSelect: 'none',
        marginBottom: '1rem',
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
        marginBottom: '0.5rem',
    },
    sectionContent: {
        marginBottom: '1.5rem',
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
    conversationBox: {
        border: '1px solid #e0e0e0',
        borderRadius: '4px',
        padding: '1rem',
        maxHeight: '20rem',
        overflow: 'auto',
        cursor: 'text',
        '& pre': {
            whiteSpace: 'pre-wrap',
            wordWrap: 'break-word',
        },
    },
});

export const FailMessage = ({ messages }: { messages: string[] }) => {
    const classes = useStyles();
    const [isExpanded, setIsExpanded] = useState(true);

    return (
        <div className={classes.section}>
            <div className={classes.sectionHeader} onClick={() => setIsExpanded(!isExpanded)}>
                {isExpanded ? <ChevronDown12Regular /> : <ChevronRight12Regular />}
                <h3 className={classes.sectionHeaderText}>Failure Reasons</h3>
            </div>

            {isExpanded && (
                <div className={classes.failContainer}>
                    {messages.map((msg) => <><span className={classes.failMessage} key={msg}><DismissCircle16Regular /> {msg}</span><br /></>)}
                </div>
            )}
        </div>
    );
};

const PassFailBadge = ({ pass, total }: { pass: number, total: number }) => {
    const classes = useStyles();
    return (<div className={classes.passFailBadge}>
        <span className={classes.score}>{pass}/{total} [{((pass * 100) / total).toFixed(1)}%]</span>
    </div>);
};

const ScoreNodeHeader = ({ item, showPrompt }: { item: ScoreNode, showPrompt?: boolean }) => {
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

    return (<div className={classes.headerContainer}>
        <PassFailBar pass={ctPass} total={ctPass + ctFail} width="24px" height="12px"/>
        <div className={classes.scenarioLabel}>{item.name}</div>
        <PassFailBadge pass={ctPass} total={ctPass + ctFail} />
        {showPrompt && item.shortenedPrompt && <div className={classes.promptHint}>{item.shortenedPrompt}</div>}
    </div>);
};

export const PromptDetails = ({ history, response, renderMarkdown }: { history: string, response: string, renderMarkdown: boolean }) => {
    const classes = useStyles();
    const [isExpanded, setIsExpanded] = useState(true);

    return (
        <div className={classes.section}>
            <div className={classes.sectionHeader} onClick={() => setIsExpanded(!isExpanded)}>
                {isExpanded ? <ChevronDown12Regular /> : <ChevronRight12Regular />}
                <h3 className={classes.sectionHeaderText}>Conversation</h3>
            </div>

            {isExpanded && (
                <div className={classes.conversationBox}>
                    <div className={classes.sectionContent}>
                        <div className={classes.sectionSubHeader}>Prompt</div>
                        {renderMarkdown ? <ReactMarkdown>{history}</ReactMarkdown> : <pre>{history}</pre>}
                    </div>
                    
                    <div>
                        <div className={classes.sectionSubHeader}>Response</div>
                        {renderMarkdown ? <ReactMarkdown>{response}</ReactMarkdown> : <pre>{response}</pre>}
                    </div>
                </div>
            )}
        </div>
    );
};
