// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ChevronDown12Regular, ChevronRight12Regular, DismissCircle16Regular } from "@fluentui/react-icons";
import { useState } from "react";
import type { MetricType } from "./MetricCard";
import { DiagnosticsContent } from "./DiagnosticsContent";
import { MetadataContent } from "./MetadataContent";
import { useStyles } from "./Styles";


export const MetricDetailsSection = ({ metric }: { metric: MetricType; }) => {
    const classes = useStyles();
    const [isExpanded, setIsExpanded] = useState(true);

    const reason = metric.reason;
    const hasReason = reason != null;
    const interpretationReason = metric.interpretation?.reason;
    const hasInterpretationReason = interpretationReason != null;
    const diagnostics = metric.diagnostics || [];
    const hasDiagnostics = diagnostics.length > 0;
    const metadata = metric.metadata || {};
    const hasMetadata = Object.keys(metadata).length > 0;

    if (!hasReason && !hasInterpretationReason && !hasDiagnostics && !hasMetadata) return null;

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
                                <div className={classes.sectionSubHeader}>Interpretation Reason</div>}
                            <div>
                                {metric.interpretation?.failed ?
                                    <span className={classes.failMessage}><DismissCircle16Regular /> {interpretationReason}</span> :
                                    <span>{interpretationReason}</span>}
                            </div>
                        </div>
                    )}

                    {hasDiagnostics && (
                        <div>
                            <div className={classes.sectionSubHeader}>Diagnostics</div>
                            <DiagnosticsContent diagnostics={diagnostics} />
                        </div>
                    )}
                    
                    {hasMetadata && (
                        <div>
                            <div className={classes.sectionSubHeader}>Metadata</div>
                            <MetadataContent metadata={metadata} />
                        </div>
                    )}
                </div>
            )}
        </div>
    );
};
