// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { makeStyles, Switch, tokens, Tree, TreeItem, TreeItemLayout, TreeItemValue, TreeOpenChangeData, TreeOpenChangeEvent } from "@fluentui/react-components";
import { useState, useCallback } from "react";
import { DefaultRootNodeName, ScoreNode, ScoreNodeType, getPromptDetails } from "./Summary";
import { PassFailBar } from "./PassFailBar";
import { MetricCardList } from "./MetricCard";
import ReactMarkdown from "react-markdown";
import { ErrorCircleRegular } from "@fluentui/react-icons";

const ScenarioLevel = ({ node, parentPath, isOpen }: { node: ScoreNode, parentPath: string, isOpen: (path: string) => boolean }) => {
    const path = `${parentPath}.${node.name}`;
    if (node.isLeafNode) {
        return <TreeItem itemType="branch" value={path}>
            <TreeItemLayout>
                <ScoreNodeHeader item={node} showPrompt={!isOpen(path)}/>
            </TreeItemLayout>
            <Tree>
                <TreeItem itemType="leaf" >
                    <TreeItemLayout>
                        <ScoreDetail scenario={node.scenario!}/>
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
                    <ScenarioLevel node={n} key={n.name} parentPath={path} isOpen={isOpen}/>
                ))}
            </Tree>
        </TreeItem>;
    }
};

export const ScenarioGroup = ({ node }: { node: ScoreNode }) => {
    const [openItems, setOpenItems] = useState<Set<TreeItemValue>>(() => new Set());
    const handleOpenChange = useCallback((_: TreeOpenChangeEvent, data: TreeOpenChangeData) => {
        setOpenItems(data.openItems);
    }, []);
    const isOpen = (name: string) => openItems.has(name);

    return (
        <Tree aria-label="Default" appearance="transparent" onOpenChange={handleOpenChange} defaultOpenItems={["." + DefaultRootNodeName]}>
            <ScenarioLevel node={node} parentPath={""} isOpen={isOpen} />
        </Tree>);
};

export const ScoreDetail = ({ scenario }: { scenario: ScenarioRunResult }) => {
    const classes = useStyles();

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
        <MetricCardList scenario={scenario} />
        {failureMessages && failureMessages.length > 0 && <FailMessage messages={failureMessages} />}
        <PromptDetails history={history} response={response} />
    </div>);
};

const useStyles = makeStyles({
    headerContainer: { display: 'flex', alignItems: 'center', flexDirection: 'row', gap: '0.5rem' },
    promptHint: { fontFamily: tokens.fontFamilyMonospace, opacity: 0.6, fontSize: '0.7rem', paddingLeft: '1rem', whiteSpace: 'nowrap' },
    score: { 
        fontSize: tokens.fontSizeBase100,
    },
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
     },
    iterationArea: {
        marginTop: '1rem',
        marginBottom: '1rem',
    },
    failMessage: {
        color: tokens.colorStatusDangerForeground2,
    },
    failContainer: {
        padding: '1rem',
        border: '1px solid #e0e0e0',
        backgroundColor: tokens.colorNeutralBackground2,
    },
    promptBox: {
        border: '1px solid #e0e0e0',
        borderRadius: '4px',
        padding: '1rem',
        maxHeight: '20rem',
        overflow: 'auto',
    },
    promptTitleLine: {
        display: 'flex',
        flexDirection: 'row',
        alignItems: 'center',
    },
    promptTitle: { flexGrow: 1 },
});

export const FailMessage = ({ messages }: { messages: string[] }) => {
    const classes = useStyles();
    return <div>
        <h3>Failure Reasons</h3>
        <div className={classes.failContainer}>
            {messages.map((msg) => <><span className={classes.failMessage} key={msg}><ErrorCircleRegular /> {msg}</span><br /></>)}
        </div>
    </div>;
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

export const PromptDetails = ({ history, response }: { history: string, response: string }) => {
    const classes = useStyles();
    const [renderPrompt, setRenderPrompt] = useState(true);
    const onChangeRenderPrompt = useCallback((ev: React.ChangeEvent<HTMLInputElement>) => {
        setRenderPrompt(ev.currentTarget.checked);
    }, [setRenderPrompt]);
    const [renderResponse, setRenderResponse] = useState(true);
    const onChangeRenderResponse = useCallback((ev: React.ChangeEvent<HTMLInputElement>) => {
        setRenderResponse(ev.currentTarget.checked);
    }, [setRenderResponse]);

    return (<div>
        <div className={classes.promptTitleLine}>
            <h3 className={classes.promptTitle}>Prompt</h3>
            <Switch checked={renderPrompt} onChange={onChangeRenderPrompt} label="Render Markdown" />
        </div>

        <div className={classes.promptBox}>
            {renderPrompt ? <ReactMarkdown>{history}</ReactMarkdown> : <pre>{history}</pre>}
        </div>

        <div className={classes.promptTitleLine}>
            <h3 className={classes.promptTitle}>Response</h3>
            <Switch checked={renderResponse} onChange={onChangeRenderResponse} label="Render Markdown" />
        </div>
        <div className={classes.promptBox}>
            {renderResponse ? <ReactMarkdown>{response}</ReactMarkdown> : <pre>{response}</pre>}
        </div>
    </div>);
};
