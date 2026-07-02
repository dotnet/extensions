// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { useContext, createContext, useState, useEffect, useMemo, useRef } from "react";
import { ReverseTextIndex, ScoreNode, ScoreNodeType, ScoreSummary } from "./Summary";

export type ReportView = 'overview' | 'cases' | 'history' | 'comparison';
export type ScenarioSort = 'name' | 'passRate';

export type ReportContextType = {
    dataset: Dataset,
    scoreSummary: ScoreSummary,
    selectedScenarioLevel?: string,
    selectScenarioLevel: (key: string) => void,
    renderMarkdown: boolean,
    setRenderMarkdown: (renderMarkdown: boolean) => void,
    prettifyJson: boolean,
    setPrettifyJson: (prettifyJson: boolean) => void,
    searchValue: string,
    setSearchValue: (searchValue: string) => void,
    selectedTags: string[],
    handleTagClick: (tag: string) => void,
    clearFilters: () => void,
    filterTree: (node: ScoreNode) => ScoreNode | null,
    view: ReportView,
    setView: (view: ReportView) => void,
    darkMode: boolean,
    setDarkMode: (darkMode: boolean) => void,
    failedOnly: boolean,
    setFailedOnly: (failedOnly: boolean) => void,
    scenSort: ScenarioSort,
    setScenSort: (scenSort: ScenarioSort) => void,
    casePage: number,
    setCasePage: (casePage: number) => void,
    cmpA?: string,
    setCmpA: (cmpA: string | undefined) => void,
    cmpB?: string,
    setCmpB: (cmpB: string | undefined) => void,
    exec?: string,
    setExec: (exec: string | undefined) => void,
    activeExecution: string,
    activeNode: ScoreNode,
    scopedNode: ScoreNode,
    isSettingsOpen: boolean,
    setIsSettingsOpen: (isSettingsOpen: boolean) => void,
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
        reverseTextIndex: new ReverseTextIndex(),
        reverseTextIndexByExecution: new Map<string, ReverseTextIndex>(),
    },
    selectedScenarioLevel: undefined,
    selectScenarioLevel: (_selectedScenarioLevel: string) => {
        throw new Error("selectScenarioLevel function not implemented");
    },
    renderMarkdown: true,
    setRenderMarkdown: (_renderMarkdown: boolean) => {
        throw new Error("setRenderMarkdown function not implemented");
    },
    prettifyJson: true,
    setPrettifyJson: (_prettifyJson: boolean) => {
        throw new Error("setPrettifyJson function not implemented");
    },
    searchValue: '',
    setSearchValue: (_searchValue: string | undefined) => { throw new Error("setSearchValue function not implemented"); },
    selectedTags: [],
    handleTagClick: (_tag: string) => { throw new Error("handleTagClick function not implemented"); },
    clearFilters: () => { throw new Error("clearFilters function not implemented"); },
    filterTree: (_node: ScoreNode) => { throw new Error("filterTree function not implemented"); },
    view: 'overview',
    setView: (_view: ReportView) => { throw new Error("setView function not implemented"); },
    darkMode: false,
    setDarkMode: (_darkMode: boolean) => { throw new Error("setDarkMode function not implemented"); },
    failedOnly: false,
    setFailedOnly: (_failedOnly: boolean) => { throw new Error("setFailedOnly function not implemented"); },
    scenSort: 'name',
    setScenSort: (_scenSort: ScenarioSort) => { throw new Error("setScenSort function not implemented"); },
    casePage: 1,
    setCasePage: (_casePage: number) => { throw new Error("setCasePage function not implemented"); },
    cmpA: undefined,
    setCmpA: (_cmpA: string | undefined) => { throw new Error("setCmpA function not implemented"); },
    cmpB: undefined,
    setCmpB: (_cmpB: string | undefined) => { throw new Error("setCmpB function not implemented"); },
    exec: undefined,
    setExec: (_exec: string | undefined) => { throw new Error("setExec function not implemented"); },
    activeExecution: "execution",
    activeNode: new ScoreNode("empty", ScoreNodeType.Group, "empty-root", "execution"),
    scopedNode: new ScoreNode("empty", ScoreNodeType.Group, "empty-root", "execution"),
    isSettingsOpen: false,
    setIsSettingsOpen: (_isSettingsOpen: boolean) => { throw new Error("setIsSettingsOpen function not implemented"); },
});

export const ReportContextProvider = ({ dataset, scoreSummary, persistKey, children }:
    { dataset: Dataset, scoreSummary: ScoreSummary, persistKey?: string, children: React.ReactNode }) => {

    const app = useProvideReportContext(dataset, scoreSummary, persistKey);

    return (
        <defaultReportContext.Provider value={app}>
            {children}
        </defaultReportContext.Provider>
    );
};

export const useReportContext = () => {
    return useContext(defaultReportContext);
};

const STORAGE_PREFIX = 'ai-eval-report:ui-intent:';

type PersistedUiIntent = {
    view: ReportView;
    darkMode: boolean;
    searchValue: string;
    selectedTags: string[];
    failedOnly: boolean;
    scenSort: ScenarioSort;
    casePage: number;
    cmpA?: string;
    cmpB?: string;
    exec?: string;
};

const readPersisted = (persistKey: string | undefined): Partial<PersistedUiIntent> | null => {
    if (!persistKey) return null;
    try {
        const raw = sessionStorage.getItem(STORAGE_PREFIX + persistKey);
        return raw ? (JSON.parse(raw) as Partial<PersistedUiIntent>) : null;
    } catch {
        return null;
    }
};

const writePersisted = (persistKey: string | undefined, value: PersistedUiIntent): void => {
    if (!persistKey) return;
    try {
        sessionStorage.setItem(STORAGE_PREFIX + persistKey, JSON.stringify(value));
    } catch {
    }
};

const useProvideReportContext = (
    dataset: Dataset,
    scoreSummary: ScoreSummary,
    persistKey: string | undefined,
): ReportContextType => {
    const [selectedScenarioLevel, setSelectedScenarioLevel] = useState<string | undefined>(undefined);
    const [renderMarkdown, setRenderMarkdown] = useState<boolean>(true);
    const [prettifyJson, setPrettifyJson] = useState<boolean>(true);
    const [selectedTags, setSelectedTags] = useState<string[]>([]);
    const [searchValue, setSearchValueRaw] = useState<string>("");

    const [view, setView] = useState<ReportView>('overview');
    const [darkMode, setDarkMode] = useState<boolean>(false);
    const [failedOnly, setFailedOnly] = useState<boolean>(false);
    const [scenSort, setScenSort] = useState<ScenarioSort>('name');
    const [casePage, setCasePage] = useState<number>(1);
    const [cmpA, setCmpA] = useState<string | undefined>(undefined);
    const [cmpB, setCmpB] = useState<string | undefined>(undefined);
    const [exec, setExec] = useState<string | undefined>(undefined);
    const [isSettingsOpen, setIsSettingsOpen] = useState<boolean>(false);

    const lastNonSearchView = useRef<ReportView>('overview');

    const hydratedKey = useRef<string | undefined>(undefined);
    useEffect(() => {
        if (!persistKey || hydratedKey.current === persistKey) return;
        hydratedKey.current = persistKey;
        const stored = readPersisted(persistKey);
        if (!stored) return;
        if (stored.view !== undefined) setView(stored.view);
        if (stored.darkMode !== undefined) setDarkMode(stored.darkMode);
        if (stored.searchValue !== undefined) setSearchValueRaw(stored.searchValue);
        if (stored.selectedTags !== undefined) setSelectedTags(stored.selectedTags);
        if (stored.failedOnly !== undefined) setFailedOnly(stored.failedOnly);
        if (stored.scenSort !== undefined) setScenSort(stored.scenSort);
        if (stored.casePage !== undefined) setCasePage(stored.casePage);
        if (stored.cmpA !== undefined) setCmpA(stored.cmpA);
        if (stored.cmpB !== undefined) setCmpB(stored.cmpB);
        if (stored.exec !== undefined) setExec(stored.exec);
        if (stored.view !== undefined && stored.view !== 'cases') {
            lastNonSearchView.current = stored.view;
        }
    }, [persistKey]);

    useEffect(() => {
        writePersisted(persistKey, {
            view, darkMode, searchValue, selectedTags, failedOnly, scenSort, casePage, cmpA, cmpB, exec,
        });
    }, [persistKey, view, darkMode, searchValue, selectedTags, failedOnly, scenSort, casePage, cmpA, cmpB, exec]);

    const selectScenarioLevel = (key: string) => {
        if (key === selectedScenarioLevel) {
            setSelectedScenarioLevel(undefined);
        } else {
            setSelectedScenarioLevel(key);
        }
    };

    const setSearchValue = (next: string) => {
        setSearchValueRaw(next);
        if (next !== "") {
            setView('cases');
            setCasePage(1);
        }
    };

    const setViewTracked = (next: ReportView) => {
        if (next !== 'cases') {
            lastNonSearchView.current = next;
        }
        setView(next);
    };

    const handleTagClick = (tag: string) => {
        setSelectedTags((prevTags) =>
          prevTags.includes(tag) ? prevTags.filter((t) => t !== tag) : [...prevTags, tag]
        );
    };

    const clearFilters = () => {
        setSelectedTags([]);
        setSearchValueRaw("");
        setView(lastNonSearchView.current);
    };

    const activeExecution = exec ?? scoreSummary.primaryResult.executionName;
    const activeNode = scoreSummary.executionHistory.get(activeExecution) ?? scoreSummary.primaryResult;

    const scopedNode = useMemo(() => {
        if (!selectedScenarioLevel) return activeNode;
        return activeNode.flattenedNodes.find(n => n.nodeKey === selectedScenarioLevel) ?? activeNode;
    }, [activeNode, selectedScenarioLevel]);

    const filterTree = (node: ScoreNode): ScoreNode | null => {
        if (selectedTags.length === 0 && searchValue === "") {
            return node;
        }

        const searchIndex =
            scoreSummary.reverseTextIndexByExecution.get(activeExecution) ?? scoreSummary.reverseTextIndex;
        const searchedNodes = searchIndex.search(searchValue);

        const srch = (node: ScoreNode) : ScoreNode | null => {
            if (node.isLeafNode) {
                const tagMatches = selectedTags.length > 0 && node.scenario?.tags?.some(tag => selectedTags.includes(tag));
                const searchMatches = searchValue !== "" && searchedNodes.has(node.nodeKey);
                return tagMatches || searchMatches ? node : null;
            }

            const filteredChildren = node.childNodes
                .map(srch)
                .filter((child): child is ScoreNode => child !== null);

            if (filteredChildren.length > 0) {
                const newNode = new ScoreNode(node.name, node.nodeType, node.nodeKey, node.executionName);
                newNode.setChildren(new Map(filteredChildren.map(child => [child.name, child])));
                newNode.aggregate();
                return newNode;
            }

            return null;
        };

        return srch(node);
    }

    return {
        dataset,
        scoreSummary,
        selectedScenarioLevel,
        selectScenarioLevel,
        renderMarkdown,
        setRenderMarkdown,
        prettifyJson,
        setPrettifyJson,
        searchValue,
        setSearchValue,
        selectedTags,
        handleTagClick,
        clearFilters,
        filterTree,
        view,
        setView: setViewTracked,
        darkMode,
        setDarkMode,
        failedOnly,
        setFailedOnly,
        scenSort,
        setScenSort,
        casePage,
        setCasePage,
        cmpA,
        setCmpA,
        cmpB,
        setCmpB,
        exec,
        setExec,
        activeExecution,
        activeNode,
        scopedNode,
        isSettingsOpen,
        setIsSettingsOpen,
    };
};
