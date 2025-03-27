// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import React, { useState, useCallback } from "react";
import { Tree, TreeItem, TreeItemLayout, TreeItemValue, TreeOpenChangeData, TreeOpenChangeEvent, Button } from "@fluentui/react-components";
import { ScoreNode, ScoreNodeType, ScoreSummary } from "./Summary";
import { PassFailBadge, PassFailBar } from "./PassFailBar";
import { RadioButtonFilled, RadioButtonRegular } from "@fluentui/react-icons";
import { useReportContext } from "./ReportContext";
import { useStyles } from "./Styles";
import { ScoreDetail } from "./ScoreDetail";

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

const SelectionButton = ({ nodeKey }: { nodeKey: string }) => {
    const { selectScenarioLevel, selectedScenarioLevel } = useReportContext();
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
        <SelectionButton nodeKey={item.nodeKey} />
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


