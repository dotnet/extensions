// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mergeClasses, Table, TableHeader, TableRow, TableHeaderCell, TableBody, TableCell, TableCellLayout } from "@fluentui/react-components";
import { ChevronDown12Regular, ChevronRight12Regular } from "@fluentui/react-icons";
import { useState } from "react";
import { MetricDisplay } from "./MetricCard";
import { useStyles } from "./Styles";
import { ScoreSummary, getScoreHistory } from "./Summary";


export const ScenarioRunHistory = ({ scoreSummary, scenario }: { scoreSummary: ScoreSummary; scenario: ScenarioRunResult; }) => {
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
