import { useContext, createContext, useState } from "react";
import { ScoreNode, ScoreNodeType, ScoreSummary } from "./Summary";

export type ReportContextType = {
    dataset: Dataset,
    scoreSummary: ScoreSummary,
    selectedScenarioLevel?: string,
    selectScenarioLevel: (key: string) => void,
    renderMarkdown: boolean,
    setRenderMarkdown: (renderMarkdown: boolean) => void,
};

// Create the default context, which will be used to provide the context value
// if no provider is found in the component tree. This should never happen in practice.
const defaultReportContext = createContext<ReportContextType>({
    dataset: {} as Dataset,
    scoreSummary: {
        primaryResult: new ScoreNode("empty", ScoreNodeType.Group, "empty-root", "execution"),
        includesReportHistory: false,
        executionHistory: new Map<string, ScoreNode>(),
        nodesByKey: new Map<string, Map<string, ScoreNode>>(),
    },
    selectedScenarioLevel: undefined,
    selectScenarioLevel: (_selectedScenarioLevel: string) => {
        throw new Error("selectScenarioLevel function not implemented");
    },
    renderMarkdown: true,
    setRenderMarkdown: (_renderMarkdown: boolean) => {
        throw new Error("setRenderMarkdown function not implemented");
    }
});

export const ReportContextProvider = ({ dataset, scoreSummary, children }:
    { dataset: Dataset, scoreSummary: ScoreSummary, children: React.ReactNode }) => {

    const app = useProvideReportContext(dataset, scoreSummary);

    return (
        <defaultReportContext.Provider value={app}>
            {children}
        </defaultReportContext.Provider>
    );
};

export const useReportContext = () => {
    return useContext(defaultReportContext);
};

const useProvideReportContext = (dataset: Dataset, scoreSummary: ScoreSummary): ReportContextType => {
    const [selectedScenarioLevel, setSelectedScenarioLevel] = useState<string | undefined>(undefined);
    const [renderMarkdown, setRenderMarkdown] = useState<boolean>(true);

    const selectScenarioLevel = (key: string) => {
        if (key === selectedScenarioLevel) {
            // if already selected, then unselect it
            setSelectedScenarioLevel(undefined);
        } else {
            setSelectedScenarioLevel(key);
        }
    };

    return {
        dataset,
        scoreSummary,
        selectedScenarioLevel,
        selectScenarioLevel,
        renderMarkdown,
        setRenderMarkdown,
    };
};