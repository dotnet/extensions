﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { makeStyles, mergeClasses, tokens, Tooltip } from "@fluentui/react-components";
import { DismissCircle16Regular, Info16Regular, Warning16Regular } from "@fluentui/react-icons";

const useCardListStyles = makeStyles({
    metricCardList: {
        display: 'flex',
        gap: '1rem',
        flexWrap: 'wrap',
        maxWidth: '75rem',
        margin: '0 auto',
    },
});

export const MetricCardList = ({ scenario, onMetricSelect, selectedMetric }: { 
  scenario: ScenarioRunResult, 
  onMetricSelect: (metric: MetricType | null) => void,
  selectedMetric: MetricType | null
}) => {
    const classes = useCardListStyles();
    return (
        <div className={classes.metricCardList}>
            {Object.values(scenario.evaluationResult.metrics).map((metric, index) => (
                <MetricCard 
                  metric={metric} 
                  key={index} 
                  onClick={() => onMetricSelect(selectedMetric === metric ? null : metric)}
                  isSelected={selectedMetric === metric}
                />
            ))}
        </div>
    );
};

const useCardStyles = makeStyles({
    card: {
        display: 'flex', 
        flexDirection: 'column', 
        alignItems: 'center', 
        gap: '0.5rem',
        padding: '.75rem', 
        border: `1px solid ${tokens.colorNeutralStroke2}`,
        borderRadius: '4px',
        width: '12rem',
        cursor: 'pointer',
        transition: 'box-shadow 0.2s ease-in-out, outline 0.2s ease-in-out',
        position: 'relative',
        '&:hover': {
            opacity: 0.9,
            boxShadow: tokens.shadow4,
        }
    },
    selectedCard: {
        zIndex: 1,
        boxShadow: tokens.shadow8,
        outline: `2px solid ${tokens.colorNeutralForeground3}`,
        outlineOffset: '0px',
        border: 'none'
    },
    metricNameText: { 
        fontSize: '1rem', 
        fontWeight: 'normal',
        width: '80%',
        textAlign: 'center',
        overflow: 'hidden',
        textOverflow: 'ellipsis',
        lineHeight: '1.2',
        height: '1.2em',
        display: "block",
        whiteSpace: 'nowrap',
        marginTop: '-0.5rem',
    },
    iconPlaceholder: {
        height: '4px',
        width: '100%',
        position: 'relative',
        marginBottom: '0',
    },
    metricIcon: {
        position: 'absolute',
        top: '-0.25rem',
        right: '-0.25rem',
    },
    metricValueText: { 
        fontSize: '1rem', 
        fontWeight: 'bold',
        width: '80%',
        textAlign: 'center',
        overflow: 'hidden',
        textOverflow: 'ellipsis',
        whiteSpace: 'nowrap',
        maxHeight: '1.2em',
    },
    scoreFgDefault: { color: tokens.colorNeutralStrokeAccessible },
    scoreFg0: { color: tokens.colorStatusDangerForeground1 },
    scoreFg1: { color: tokens.colorStatusDangerForeground2 },
    scoreFg2: { color: tokens.colorStatusDangerForeground2 },
    scoreFg3: { color: tokens.colorStatusWarningForeground2 },
    scoreFg4: { color: tokens.colorStatusSuccessForeground2 },
    scoreFg5: { color: tokens.colorStatusSuccessForeground2 },
    scoreBgDefault: { backgroundColor: tokens.colorNeutralCardBackground },
    scoreBg0: { backgroundColor: tokens.colorStatusDangerBackground1 },
    scoreBg1: { backgroundColor: tokens.colorStatusDangerBackground2 },
    scoreBg2: { backgroundColor: tokens.colorStatusDangerBackground2 },
    scoreBg3: { backgroundColor: tokens.colorStatusWarningBackground2 },
    scoreBg4: { backgroundColor: tokens.colorStatusSuccessBackground2 },
    scoreBg5: { backgroundColor: tokens.colorStatusSuccessBackground2 },
});

const useCardColors = (interpretation?: EvaluationMetricInterpretation) => {
    const classes = useCardStyles();
    let fg = classes.scoreFgDefault;
    let bg = classes.scoreBgDefault;
    if (interpretation?.rating) {
        switch (interpretation.rating) {
            case "unknown":
            case "inconclusive":
                fg = classes.scoreFg0;
                bg = classes.scoreBg0;
                break;
            case "exceptional":
                fg = classes.scoreFg5;
                bg = classes.scoreBg5;
                break;
            case "good":
                fg = classes.scoreFg4;
                bg = classes.scoreBg4;
                break;
            case "average":
                fg = classes.scoreFg3;
                bg = classes.scoreBg3;
                break;
            case "poor":
                fg = classes.scoreFg2;
                bg = classes.scoreBg2;
                break;
            case "unacceptable":
                fg = classes.scoreFg1;
                bg = classes.scoreBg1;
                break;
        }
    }
    return { fg, bg };
};

export type MetricType = StringMetric | NumericMetric | BooleanMetric | MetricWithNoValue;

export const MetricCard = ({ 
    metric, 
    onClick,
    isSelected
}: { 
    metric: MetricType, 
    onClick: () => void,
    isSelected: boolean
}) => {
    const getValue = (metric: MetricType): string => {
        switch (metric.$type) {
            case "string":
                return metric?.value ?? "??";
            case "boolean":
                return !metric || metric.value === undefined || metric.value === null ? 
                    '??' :
                    metric.value ? 'Pass' : 'Fail';
            case "numeric":
                return metric?.value?.toString() ?? "??";
            case "none":
                return "None";
            default:
                throw new Error(`Unknown metric type: ${metric["$type"]}`);
        }
    };

    const metricValue = getValue(metric);
    const classes = useCardStyles();
    const { fg, bg } = useCardColors(metric.interpretation);
    
    const hasReasons = metric.reason != null || metric.interpretation?.reason != null;
    const hasInformationalMessages = metric.diagnostics.some((d: EvaluationDiagnostic) => d.severity == "informational");
    const hasWarningMessages = metric.diagnostics.some((d: EvaluationDiagnostic) => d.severity == "warning");
    const hasErrorMessages = metric.diagnostics.some((d: EvaluationDiagnostic) => d.severity == "error");
    
    const cardClass = mergeClasses(
        bg, 
        classes.card, 
        isSelected ? classes.selectedCard : undefined
    );
    
    let statusIcon = null;
    let statusTooltip = '';
    
    if (hasErrorMessages) {
        statusIcon = <DismissCircle16Regular className={classes.metricIcon} />;
        statusTooltip = 'This metric has errors. Click the card to view more details.';
    } else if (hasWarningMessages) {
        statusIcon = <Warning16Regular className={classes.metricIcon} />;
        statusTooltip = 'This metric has warnings. Click the card to view more details.';
    } else if (hasInformationalMessages || hasReasons) {
        statusIcon = <Info16Regular className={classes.metricIcon} />;
        statusTooltip = 'This metric has additional information. Click the card to view more details.';
    }
    
    const tooltipContent = (
        <div>
            <div>Name: {metric.name}</div>
            <div>Value: {metricValue}</div>
        </div>
    );
    
    return (
        <Tooltip content={tooltipContent} relationship="label">
            <div className={cardClass} onClick={onClick}>
                <div className={classes.iconPlaceholder}>
                    {statusIcon && (
                        <Tooltip content={statusTooltip} relationship="description">
                            <span>{statusIcon}</span>
                        </Tooltip>
                    )}
                </div>
                <div className={classes.metricNameText}>
                    {metric.name}
                </div>
                <div className={mergeClasses(fg, classes.metricValueText)}>
                    {metricValue}
                </div>
            </div>
        </Tooltip>
    );
};
