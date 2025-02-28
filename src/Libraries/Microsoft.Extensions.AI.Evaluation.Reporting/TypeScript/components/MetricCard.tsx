// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { makeStyles, mergeClasses, tokens, Tooltip } from "@fluentui/react-components";
import { DismissCircle16Regular, ErrorCircleRegular, Info16Regular, InfoRegular, Warning16Regular, WarningRegular } from "@fluentui/react-icons";

const useCardListStyles = makeStyles({
    metricCardList: { display: 'flex', gap: '1rem', flexWrap: 'wrap' },
});

export const MetricCardList = ({ scenario }: { scenario: ScenarioRunResult }) => {
    const classes = useCardListStyles();
    return (
        <div className={classes.metricCardList}>
            {Object.values(scenario.evaluationResult.metrics).map((metric, index) => (
                <MetricCard metric={metric} key={index} />
            ))}
        </div>
    );
};

const useCardStyles = makeStyles({
    card: {
        display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '0.5rem',
        padding: '.75rem', border: '1px solid #e0e0e0', borderRadius: '4px',
        minWidth: '8rem'
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

export const MetricCard = ({ metric }: { metric: MetricType }) => {

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
    const hasReason = metric.interpretation?.reason != null;
    const hasInformationalMessages = metric.diagnostics.some((d: EvaluationDiagnostic) => d.severity == "informational");
    const hasWarningMessages = metric.diagnostics.some((d: EvaluationDiagnostic) => d.severity == "warning");
    const hasErrorMessages = metric.diagnostics.some((d: EvaluationDiagnostic) => d.severity == "error");
    const supportsHover = hasReason || hasInformationalMessages || hasWarningMessages || hasErrorMessages;
    const card =
        (<div className={mergeClasses(bg, classes.card)}>
            <div className={classes.metricText}>{metric.name} { (hasErrorMessages && <DismissCircle16Regular />) || 
                (hasWarningMessages && <Warning16Regular />) || 
                ((hasInformationalMessages || hasReason) && <Info16Regular />)}</div>
            <div className={mergeClasses(fg, classes.valueText)}>{renderValue(metric)}</div>
        </div>);
    if (supportsHover) {
        return (<Tooltip
            content={{ children: <MetricDetails metric={metric} /> }}
            relationship="description">
            {card}
        </Tooltip>);
    } else {
        return card;
    }
};

const useDetailStyles = makeStyles({
    diagError: { fontStyle: tokens.fontFamilyMonospace, color: tokens.colorStatusDangerForeground2 },
    diagWarn: { fontStyle: tokens.fontFamilyMonospace, color: tokens.colorStatusWarningForeground2 },
    diagInfo: { fontStyle: tokens.fontFamilyMonospace },
});

export const MetricDetails = ({ metric }: { metric: MetricWithNoValue | NumericMetric | BooleanMetric | StringMetric }) => {
    const classes = useDetailStyles();
    const reason = metric.interpretation?.reason;
    const failed = metric.interpretation?.failed ?? false;
    const informationalMessages = metric.diagnostics.filter((d: EvaluationDiagnostic) => d.severity == "informational").map((d: EvaluationDiagnostic) => d.message);
    const hasInformationalMessages = informationalMessages.length > 0;
    const warningMessages = metric.diagnostics.filter((d: EvaluationDiagnostic) => d.severity == "warning").map((d: EvaluationDiagnostic) => d.message);
    const hasWarningMessages = warningMessages.length > 0;
    const errorMessages = metric.diagnostics.filter((d: EvaluationDiagnostic) => d.severity == "error").map((d: EvaluationDiagnostic) => d.message);
    const hasErrorMessages = errorMessages.length > 0;
    return (
        <div>
            {reason && <div>
                {failed ? 
                    <p className={classes.diagError}><ErrorCircleRegular /> {reason}</p> :
                    <p className={classes.diagInfo}><InfoRegular /> {reason}</p>
                }
            </div>}
            {hasErrorMessages && <div>
                {errorMessages.map((message: string, index: number) =>
                    <p key={index} className={classes.diagError}><ErrorCircleRegular /> {message}</p>)}
            </div>}
            {hasWarningMessages && <div>
                {warningMessages.map((message: string, index: number) =>
                    <p key={index} className={classes.diagWarn}><WarningRegular /> {message}</p>)}
            </div>}
            {hasInformationalMessages && <div>
                {informationalMessages.map((message: string, index: number) =>
                    <p key={index} className={classes.diagInfo}><InfoRegular /> {message}</p>)}
            </div>}
        </div>);
};