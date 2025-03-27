import { useState } from "react";
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

    return (<div className={classes.iterationArea}>
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
