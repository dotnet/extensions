import { useContext, createContext, useState } from "react";

export type ReportContextType = {
    selectedScenarioLevel?: string,
    selectScenarioLevel: (key: string) => void,
};

const defaultReportContext = createContext<ReportContextType>({
    selectedScenarioLevel: undefined,
    selectScenarioLevel: (/* key */) => {
        throw new Error("selectScenarioLevel function not implemented");
    },
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

    const selectScenarioLevel = (key: string) => {
        setSelectedScenarioLevel(key);
    };

    return {
        selectedScenarioLevel,
        selectScenarioLevel,
    };
};