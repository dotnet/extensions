import { useContext, createContext, useState } from "react";

export type ReportContextType = {
    selectedScenarioLevel?: string,
    selectScenarioLevel: (key: string) => void,
    renderMarkdown: boolean,
    setRenderMarkdown: (renderMarkdown: boolean) => void,
};

const defaultReportContext = createContext<ReportContextType>({
    selectedScenarioLevel: undefined,
    selectScenarioLevel: (/* key */) => {
        throw new Error("selectScenarioLevel function not implemented");
    },
    renderMarkdown: true,
    setRenderMarkdown: (/* renderMarkdown */) => {
        throw new Error("setRenderMarkdown function not implemented");
    }
});

export const ReportContextProvider = ({children}: {children: React.ReactNode}) => {
    const app = useProvideReportContext();
    return (
        <defaultReportContext.Provider value={app}>
            {children}
        </defaultReportContext.Provider>
    );
};

export const useReportContext = () => {
    return useContext(defaultReportContext);
};

const useProvideReportContext = () : ReportContextType => {
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
        selectedScenarioLevel,
        selectScenarioLevel,
        renderMarkdown,
        setRenderMarkdown,
    };
};