import { useEffect, useRef, useState } from "react";
import { ChatDetailsSection } from "./ChatDetailsSection";
import { ConversationDetails } from "./ConversationDetails";
import { type MetricType, MetricCardList } from "./MetricCard";
import { MetricDetailsSection } from "./MetricDetailsSection";
import { ScenarioRunHistory } from "./ScenarioRunHistory";
import { useStyles } from "./Styles";
import { ScoreSummary, getConversationDisplay } from "./Summary";


export const ScoreDetail = ({ scenario, scoreSummary }: { scenario: ScenarioRunResult; scoreSummary: ScoreSummary; }) => {
    const classes = useStyles();
    const [selectedMetric, setSelectedMetric] = useState<MetricType | null>(null);
    const { messages, model, usage } = getConversationDisplay(scenario.messages, scenario.modelResponse);
    const tagRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        if (tagRef.current) {
            // Super hacky way to get the parent element to stretch to 100% width
            // since it is not directly addressable in CSS
            tagRef.current.parentElement?.style.setProperty("width", "100%");
        }
    }, [tagRef]);

    return (<div className={classes.iterationArea} ref={tagRef}>
        <ScenarioRunHistory scoreSummary={scoreSummary} scenario={scenario} />
        <MetricCardList
            scenario={scenario}
            onMetricSelect={setSelectedMetric}
            selectedMetric={selectedMetric} />
        {selectedMetric && <MetricDetailsSection metric={selectedMetric} />}
        <ConversationDetails messages={messages} model={model} usage={usage} />
        {scenario.chatDetails && <ChatDetailsSection chatDetails={scenario.chatDetails} />}
    </div>);
};
