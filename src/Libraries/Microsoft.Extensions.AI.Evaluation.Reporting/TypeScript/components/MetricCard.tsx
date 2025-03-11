// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { makeStyles, mergeClasses, tokens } from "@fluentui/react-components";
import { DismissCircle16Regular, Info16Regular, Warning16Regular } from "@fluentui/react-icons";

const useCardListStyles = makeStyles({
    metricCardList: { display: 'flex', gap: '1rem', flexWrap: 'wrap' },
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
        display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '0.5rem',
        padding: '.75rem', border: '1px solid #e0e0e0', borderRadius: '4px',
        minWidth: '8rem',
        cursor: 'pointer',
        transition: 'box-shadow 0.2s ease-in-out, outline 0.2s ease-in-out',
        position: 'relative',
        '&:hover': {
            opacity: 0.9,
            boxShadow: '0 2px 4px rgba(0, 0, 0, 0.1)'
        }
    },
    selectedCard: {
        zIndex: 1,
        boxShadow: '0 4px 8px rgba(0, 0, 0, 0.15)',
        outline: `2px solid ${tokens.colorNeutralForeground3}`,
        outlineOffset: '0px',
        border: 'none'
    },
    metricText: { fontSize: '1rem', fontWeight: 'normal' },
    valueText: { fontSize: '1.5rem', fontWeight: 'bold' },
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

type MetricType = StringMetric | NumericMetric | BooleanMetric | MetricWithNoValue;

export const MetricCard = ({ 
    metric, 
    onClick,
    isSelected
}: { 
    metric: MetricType, 
    onClick: () => void,
    isSelected: boolean
}) => {
    let renderValue: (metric: MetricType) => React.ReactNode;
    switch (metric.$type) {
        case "string":
            renderValue = (metric: MetricType) => <>{metric?.value ?? "??"}</>;
            break;
        case "boolean":
            renderValue = (metric: MetricType) => <>{
                !metric || metric.value === undefined || metric.value === null ? 
                '??' :
                metric.value ? 'Pass' : 'Fail'}</>;
            break;
        case "numeric":
            renderValue = (metric: MetricType) => <>{metric?.value ?? "??"}</>;
            break;
        case "none":
            renderValue = () => <>None</>;
            break;
        default:
            throw new Error(`Unknown metric type: ${metric["$type"]}`);
    }

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
    
    return (
        <div className={cardClass} onClick={onClick}>
            <div className={classes.metricText}>{metric.name} {
                    (hasErrorMessages && <DismissCircle16Regular />) || 
                    (hasWarningMessages && <Warning16Regular />) || 
                    ((hasInformationalMessages || hasReasons) && <Info16Regular />)}
            </div>
            <div className={mergeClasses(fg, classes.valueText)}>{renderValue(metric)}</div>
        </div>
    );
};

const useDetailStyles = makeStyles({
    diagError: { fontStyle: tokens.fontFamilyMonospace, color: tokens.colorStatusDangerForeground2 },
    diagWarn: { fontStyle: tokens.fontFamilyMonospace, color: tokens.colorStatusWarningForeground2 },
    diagInfo: { fontStyle: tokens.fontFamilyMonospace },
});
