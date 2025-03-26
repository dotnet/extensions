import { Table, TableHeader, TableRow, TableHeaderCell, TableBody, TableCell, TableCellLayout, Button, mergeClasses } from "@fluentui/react-components";
import { useReportContext } from "./ReportContext";
import { useStyles } from "./Styles";
import { ScoreNodeType } from "./Summary";
import { PassFailBar } from "./PassFailBar";
import { PassFailBadge } from "./ScenarioTree";
import { DismissRegular } from "@fluentui/react-icons";

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

    const executions = Array.from(scoreSummary.executionHistory.keys()).reverse();

    const getHistoryContent = (executionName: string) => {
        const historyNode = scoreSummary.nodesByKey.get(executionName)?.get(summaryNode.nodeKey);
        if (!historyNode) {
            return undefined;
        }

        let ctPass, ctFail;
        switch (historyNode.nodeType) {
            case ScoreNodeType.Group:
                ctPass = historyNode.numPassingIterations;
                ctFail = historyNode.numFailingIterations;
                break;
            case ScoreNodeType.Scenario:
                ctPass = historyNode.numPassingIterations;
                ctFail = historyNode.numFailingIterations;
                break;
            case ScoreNodeType.Iteration:
                ctPass = historyNode.failed ? 0 : 1;
                ctFail = historyNode.failed ? 1 : 0;
                break;
        }

        return (<>
            <PassFailBar pass={ctPass} total={ctPass + ctFail} width="24px" height="12px" />
            <PassFailBadge pass={ctPass} total={ctPass + ctFail} />
        </>);
    }

    const latestExecutionStyle = mergeClasses(classes.verticalText, classes.currentExecutionForeground);
    
    return (<div className={classes.section}>
        <div className={classes.sectionHeader}>
            <Button icon={<DismissRegular/>} appearance="transparent" onClick={() => selectScenarioLevel(selectedScenarioLevel)}/>
            <h3 className={classes.sectionHeaderText}>Pass/Fail Trends for {summaryNode.name}</h3>
        </div>

        <div className={classes.sectionContainer}>
            <div className={classes.tableContainer}>
                <Table>
                    <TableHeader>
                        <TableRow>
                            {executions.map((execution) => (
                                <TableHeaderCell key={execution}
                                    className={execution == summaryNode.executionName ? classes.currentExecutionBackground : undefined}>
                                    <div className={classes.executionHeaderCell}>
                                        <span className={execution == summaryNode.executionName ? latestExecutionStyle : classes.verticalText}>{execution}</span>
                                    </div>
                                </TableHeaderCell>
                            ))}
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        <TableRow>
                            {executions.map((execution) => (
                                <TableCell key={execution}
                                    className={execution == summaryNode.executionName ? classes.currentExecutionBackground : undefined}>
                                    {getHistoryContent(execution)}
                                </TableCell>
                            ))}
                        </TableRow>
                    </TableBody>
                </Table>
            </div>
        </div>
    </div>);
};