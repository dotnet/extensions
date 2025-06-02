// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { DismissCircle16Regular, Warning16Regular, Info16Regular, Copy16Regular } from "@fluentui/react-icons";
import { Table, TableHeader, TableRow, TableHeaderCell, TableBody, TableCell, mergeClasses } from "@fluentui/react-components";
import { useStyles } from "./Styles";

export const DiagnosticsContent = ({ diagnostics }: { diagnostics: EvaluationDiagnostic[]; }) => {
    const classes = useStyles();

    if (diagnostics.length === 0) {
        return null;
    }

    const renderSeverityCell = (diagnostic: EvaluationDiagnostic) => {
        if (diagnostic.severity === "error") {
            return (
                <span className={classes.diagnosticErrorCell}>
                    <DismissCircle16Regular /> Error
                </span>
            );
        } else if (diagnostic.severity === "warning") {
            return (
                <span className={classes.diagnosticWarningCell}>
                    <Warning16Regular /> Warning
                </span>
            );
        } else {
            return (
                <span className={classes.diagnosticInfoCell}>
                    <Info16Regular /> Info
                </span>
            );
        }
    };

    const copyToClipboard = (text: string) => {
        navigator.clipboard.writeText(text);
    };

    return (
        <div className={classes.tableContainer} tabIndex={0}>
            <Table className={classes.autoWidthTable}>
                <TableHeader>
                    <TableRow>
                        <TableHeaderCell className={mergeClasses(classes.tableHeaderCell, classes.diagnosticSeverityCell)}>Severity</TableHeaderCell>
                        <TableHeaderCell className={mergeClasses(classes.tableHeaderCell, classes.diagnosticMessageCell)}>Message</TableHeaderCell>
                        <TableHeaderCell className={mergeClasses(classes.tableHeaderCell, classes.diagnosticCopyButtonCell)}></TableHeaderCell>
                    </TableRow>
                </TableHeader>
                <TableBody>
                    {diagnostics.map((diag, index) => (
                        <TableRow key={`diag-${index}`}>
                            <TableCell className={classes.diagnosticSeverityCell}>
                                {renderSeverityCell(diag)}
                            </TableCell>
                            <TableCell className={classes.diagnosticMessageCell}>
                                <pre className={classes.diagnosticMessageText}>
                                    {diag.message}
                                </pre>
                            </TableCell>
                            <TableCell className={classes.diagnosticCopyButtonCell}>
                                <button
                                    className={classes.copyButton}
                                    onClick={() => copyToClipboard(`${diag.severity}: ${diag.message}`)}
                                    title="Copy Diagnostic"
                                >
                                    <Copy16Regular />
                                </button>
                            </TableCell>
                        </TableRow>
                    ))}
                </TableBody>
            </Table>
        </div>
    );
};
