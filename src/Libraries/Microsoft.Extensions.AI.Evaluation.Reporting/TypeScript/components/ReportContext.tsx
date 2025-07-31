// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { useContext, createContext, useState } from "react";
import { ReverseTextIndex, ScoreNode, ScoreNodeType, ScoreSummary } from "./Summary";

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
    const [prettifyJson, setPrettifyJson] = useState<boolean>(true);
    const [selectedTags, setSelectedTags] = useState<string[]>([]);
    const [searchValue, setSearchValue] = useState<string>("");

    const selectScenarioLevel = (key: string) => {
        if (key === selectedScenarioLevel) {
            // if already selected, then unselect it
            setSelectedScenarioLevel(undefined);
        } else {
            setSelectedScenarioLevel(key);
        }
    };

    const handleTagClick = (tag: string) => {
        setSelectedTags((prevTags) =>
          prevTags.includes(tag) ? prevTags.filter((t) => t !== tag) : [...prevTags, tag]
        );
    };
    
    const clearFilters = () => {
        setSelectedTags([]);
        setSearchValue("");
    };

    const filterTree = (node: ScoreNode): ScoreNode | null => {
        if (selectedTags.length === 0 && searchValue === "") {
            return node;
        }

        const searchedNodes = scoreSummary.reverseTextIndex.search(searchValue);

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
    };
};
