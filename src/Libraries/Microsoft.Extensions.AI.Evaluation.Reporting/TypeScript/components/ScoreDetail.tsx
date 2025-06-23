// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { useEffect, useRef, useState } from "react";
import { ChatDetailsSection } from "./ChatDetailsSection";
import { ConversationDetails } from "./ConversationDetails";
import { type MetricType, MetricCardList } from "./MetricCard";
import { MetricDetailsSection } from "./MetricDetailsSection";
import { ScenarioRunHistory } from "./ScenarioRunHistory";
import { useStyles } from "./Styles";
import { ScoreSummary, getConversationDisplay } from "./Summary";
import { MoverDirections, getTabsterAttribute } from "tabster";

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

    return (<div className={classes.iterationArea} ref={tagRef} {...getTabsterAttribute({
        mover: { direction: MoverDirections.Both },
    })}>
        <ScenarioRunHistory scoreSummary={scoreSummary} scenario={scenario} />
        <MetricCardList
            scenario={scenario}
            onMetricSelect={setSelectedMetric}
            selectedMetric={selectedMetric} />
        {selectedMetric && <MetricDetailsSection metric={selectedMetric} />}
        <ConversationDetails messages={messages} model={model} usage={usage} selectedMetric={selectedMetric} />
        {scenario.chatDetails && scenario.chatDetails.turnDetails.length > 0 && <ChatDetailsSection chatDetails={scenario.chatDetails} />}
    </div>);
};
