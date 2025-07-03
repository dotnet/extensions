// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Table, TableHeader, TableRow, TableHeaderCell, TableBody, TableCell, TableCellLayout, Button, mergeClasses } from "@fluentui/react-components";
import { useReportContext } from "./ReportContext";
import { useStyles } from "./Styles";
import { ScoreNode, ScoreNodeType, ScoreSummary } from "./Summary";
import { PassFailBarLabel, PassFailVerticalBar } from "./PassFailBar";
import { DismissRegular } from "@fluentui/react-icons";

type ScoreNodeHistoryData = {
    executionName: string,
    selected: boolean,
    hasData: boolean,
    iterationPass: number,
    iterationTotal: number,
    scenarioPass: number,
    scenarioTotal: number,
    previous?: ScoreNodeHistoryData,
};

const calculateScoreNodeHistoryData = (scoreSummary: ScoreSummary, selectedScoreNode: ScoreNode) : ScoreNodeHistoryData[] => {
    
    const data: ScoreNodeHistoryData[] = [];
    
    const executions = [...scoreSummary.executionHistory.keys()].reverse();
    for (const executionName of executions) {
        const historyNode = scoreSummary.nodesByKey.get(executionName)?.get(selectedScoreNode.nodeKey);
        if (!historyNode) {
            data.push({
                executionName, 
                selected: false,
                hasData: false, 
                iterationPass: 0, 
                iterationTotal: 0, 
                scenarioPass: 0, 
                scenarioTotal: 0
            });
            continue;
        }

        let itCtPass, itCtFail, scCtPass, scCtFail;
        switch (historyNode.nodeType) {
            case ScoreNodeType.Group:
                itCtPass = historyNode.numPassingIterations;
                itCtFail = historyNode.numFailingIterations;
                scCtPass = historyNode.numPassingScenarios;
                scCtFail = historyNode.numFailingScenarios;
                break;
            case ScoreNodeType.Scenario:
                itCtPass = historyNode.numPassingIterations;
                itCtFail = historyNode.numFailingIterations;
                scCtPass = historyNode.numPassingScenarios;
                scCtFail = historyNode.numFailingScenarios;
                break;
            case ScoreNodeType.Iteration:
                itCtPass = historyNode.failed ? 0 : 1;
                itCtFail = historyNode.failed ? 1 : 0;
                scCtPass = historyNode.failed ? 0 : 1;
                scCtFail = historyNode.failed ? 1 : 0;
                break;
        }

        const previous = data.length > 0 ? data[data.length - 1] : undefined;

        data.push({
            executionName,
            selected: historyNode.executionName === selectedScoreNode.executionName,
            hasData: true,
            iterationPass: itCtPass,
            iterationTotal: itCtPass + itCtFail,
            scenarioPass: scCtPass,
            scenarioTotal: scCtPass + scCtFail,
            previous,
        });
    
    }

    return data;
}

export const ScoreNodeHistory = () => {
    const classes = useStyles();
    const { scoreSummary, selectedScenarioLevel, selectScenarioLevel } = useReportContext();

    if (!selectedScenarioLevel) {
        return null;
    }

    const latestExecution = scoreSummary.primaryResult.executionName;

    const summaryNode = scoreSummary.nodesByKey.get(latestExecution)?.get(selectedScenarioLevel);
    if (!summaryNode) {
        return null;
    }

    const latestExecutionStyle = mergeClasses(classes.verticalText, classes.currentExecutionForeground);

    const historyData = calculateScoreNodeHistoryData(scoreSummary, summaryNode);

    return (<div className={classes.section}>
        <div className={classes.dismissableSectionHeader}>
            <Button icon={<DismissRegular />} appearance="subtle" onClick={() => selectScenarioLevel(selectedScenarioLevel)} />
            <h3 className={classes.sectionHeaderText}>Pass/Fail Trends for {summaryNode.name}</h3>
        </div>

        <div className={classes.sectionContainer}>
            <Table>
                <TableHeader>
                    <TableRow>
                        {historyData.map((data) => (
                            <TableHeaderCell key={data.executionName}
                                className={data.selected ? classes.currentExecutionBackground : undefined}>
                                <div className={classes.executionHeaderCell}>
                                    <span className={data.selected ? latestExecutionStyle : classes.verticalText}>{data.executionName}</span>
                                </div>
                            </TableHeaderCell>
                        ))}
                        <TableHeaderCell className={classes.currentExecutionBackground}>
                        </TableHeaderCell>
                    </TableRow>
                </TableHeader>
                <TableBody>
                    <TableRow>
                        {historyData.map((data) => (
                            <TableCell key={data.executionName}
                                className={data.selected ? classes.currentExecutionBackground : undefined}>
                                {data.hasData && <HistoryCellContent data={data} mode="scenario"/>}
                            </TableCell>
                        ))}
                        <TableCell className={classes.currentExecutionBackground}>
                            <TableCellLayout className={classes.historyMetricCell}>Scenario<br />Pass Rate</TableCellLayout>
                        </TableCell>
                    </TableRow>
                    <TableRow>
                        {historyData.map((data) => (
                            <TableCell key={data.executionName}
                                className={data.selected ? classes.currentExecutionBackground : undefined}>
                                {data.hasData && <HistoryCellContent data={data} mode="iteration"/>}
                            </TableCell>
                        ))}
                        <TableCell className={classes.currentExecutionBackground}>
                            <TableCellLayout className={classes.historyMetricCell}>Iteration<br />Pass Rate</TableCellLayout>
                        </TableCell>
                    </TableRow>
                </TableBody>
            </Table>
        </div>
    </div>);
};

const HistoryCellContent = ({ data, mode}: {data: ScoreNodeHistoryData, mode: "iteration" | "scenario"}) => {
    const classes = useStyles();

    let ctPass, ctTotal, prevCtPass, prevCtTotal;
    switch (mode) {
        case "iteration":
            ctPass = data.iterationPass;
            ctTotal = data.iterationTotal;
            prevCtPass = data.previous?.iterationPass;
            prevCtTotal = data.previous?.iterationTotal;
            break;
        case "scenario":
            ctPass = data.scenarioPass;
            ctTotal = data.scenarioTotal;
            prevCtPass = data.previous?.scenarioPass;
            prevCtTotal = data.previous?.scenarioTotal;
            break;
    }

    return (<div className={classes.scenarioHistoryCell}>
        <PassFailVerticalBar pass={ctPass} total={ctTotal} width="24px" height="48px" />
        <PassFailBarLabel pass={ctPass} total={ctTotal} prevPass={prevCtPass} prevTotal={prevCtTotal}/>
    </div>);
};
