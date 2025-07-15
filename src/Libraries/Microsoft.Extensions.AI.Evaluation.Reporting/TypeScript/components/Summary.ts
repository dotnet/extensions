// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export enum ScoreNodeType {
    Group,
    Scenario,
    Iteration,
}

export type ScoreSummary = {
    primaryResult: ScoreNode;
    includesReportHistory: boolean;
    executionHistory: Map<string, ScoreNode>;
    nodesByKey: Map<string, Map<string, ScoreNode>>;
};

export class ScoreNode {
    constructor(name: string, nodeType: ScoreNodeType, nodeKey: string, executionName: string) {
        this.name = name;
        this.nodeType = nodeType;
        this.children = new Map<string, ScoreNode>();
        this.nodeKey = nodeKey;
        this.executionName = executionName;
    }
    private children: Map<string, ScoreNode>;

    nodeType: ScoreNodeType;
    name: string;
    executionName: string;
    nodeKey: string;

    scenario?: ScenarioRunResult;

    shortenedPrompt?: string;
    failed: boolean = false;
    numPassingIterations: number = 0;
    numFailingIterations: number = 0;
    numPassingScenarios: number = 0;
    numFailingScenarios: number = 0;

    setChildren(children: Map<string, ScoreNode>) {
        this.children = children;
    }

    insertNode(path: string[], scenario: ScenarioRunResult) {
        if (path.length === 0) {
            if (this.scenario) {
                throw new Error(`Duplicate scenario: ${scenario.scenarioName}`);
            }
            this.scenario = scenario;
            return;
        }

        const [head, ...tail] = path;
        const child = this.children.get(head);
        if (child) {
            child.insertNode(tail, scenario);
        } else {
            let nodeType: ScoreNodeType;
            switch (path.length) {
                case 1: nodeType = ScoreNodeType.Iteration; break;
                case 2: nodeType = ScoreNodeType.Scenario; break;
                default: nodeType = ScoreNodeType.Group; break;
            }
            const newChild = new ScoreNode(head, nodeType, `${this.nodeKey}.${head}`, this.executionName);
            newChild.insertNode(tail, scenario);
            this.children.set(head, newChild);
        }
    }

    get isLeafNode() {
        return this.scenario !== undefined;
    }

    get childNodes() {
        return [...this.children.values()];
    }

    get flattenedNodes() {
        return [...flattener(this)];
    }

    aggregate(filteredTags: string[] = []) {
        this.failed = false;
        this.numPassingIterations = 0;
        this.numFailingIterations = 0;
        this.numPassingScenarios = 0;
        this.numFailingScenarios = 0;

        if (this.isLeafNode) {
            if (filteredTags.length > 0 && !this.scenario?.tags?.some(tag => filteredTags.includes(tag))) {
                return;
            }

            this.failed = false;
            for (const metric of Object.values(this.scenario?.evaluationResult.metrics ?? [])) {
                if ((metric.interpretation && metric.interpretation.failed) ||
                    (metric.diagnostics.some(d => d.severity === "error"))) {
                    this.failed = true;
                    break;
                }
            }

            this.numPassingIterations = this.failed ? 0 : 1;
            this.numFailingIterations = this.failed ? 1 : 0;
            const lastMessage = this.scenario?.messages[this.scenario?.messages.length - 1];

            const { messages } = getConversationDisplay(lastMessage ? [lastMessage] : [], this.scenario?.modelResponse);
            let history = "";
            if (messages.length === 1) {
                history = messages[0].content;
            } else if (messages.length > 1) {
                history = messages.map(m => `[${m.participantName}] ${m.content}`).join("\n\n");
            }

            this.shortenedPrompt = shortenPrompt(history);
        } else {
            for (const child of this.childNodes) {
                child.aggregate(filteredTags);
                if (filteredTags.length === 0 || child.numPassingIterations + child.numFailingIterations > 0) {
                    this.failed = this.failed || child.failed;
                    this.numPassingIterations += child.numPassingIterations;
                    this.numFailingIterations += child.numFailingIterations;
                    if (this.nodeType == ScoreNodeType.Scenario) {
                        this.numPassingScenarios = this.failed ? 0 : 1;
                        this.numFailingScenarios = this.failed ? 1 : 0;
                        this.shortenedPrompt = child.shortenedPrompt;
                    } else if (this.nodeType == ScoreNodeType.Group) {
                        this.numPassingScenarios += child.numPassingScenarios;
                        this.numFailingScenarios += child.numFailingScenarios;
                    }
                }
            }
        }
    }

    collapseSingleChildNodes() {
        if (this.isLeafNode) {
            return;
        }

        while (this.childNodes.length === 1) {
            const onlyChild = this.childNodes[0];
            this.name += ` / ${onlyChild.name}`;
            this.children = onlyChild.children;
            this.scenario = onlyChild.scenario;
        }

        for (const child of this.childNodes) {
            child.collapseSingleChildNodes();
        }
    }
};

export const createScoreSummary = (dataset: Dataset): ScoreSummary => {

    const executionHistory = new Map<string, ScoreNode>();
    for (const scenario of dataset.scenarioRunResults) {
        const executionName = scenario.executionName;
        if (!executionHistory.has(executionName)) {
            const newRoot = new ScoreNode(`All Evaluations [${executionName}]`, ScoreNodeType.Group, "root", executionName);
            executionHistory.set(executionName, newRoot);
        }
        const scoreNode = executionHistory.get(executionName)!;
        const path = [...scenario.scenarioName.split('.'), scenario.iterationName];
        scoreNode.insertNode(path, scenario);
    }

    const nodesByKey = new Map<string, Map<string, ScoreNode>>();
    for (const executionName of executionHistory.keys()) {
        nodesByKey.set(executionName, new Map<string, ScoreNode>());
    }

    for (const scoreNode of executionHistory.values()) {
        scoreNode.aggregate();

        for (const node of scoreNode.flattenedNodes) {
            const nodeList = nodesByKey.get(node.executionName)!;
            nodeList.set(node.nodeKey, node);
        }
    }

    const [primaryResult] = executionHistory.values();
    primaryResult.collapseSingleChildNodes();

    return {
        primaryResult,
        includesReportHistory: executionHistory.size > 1,
        executionHistory,
        nodesByKey,
    } as ScoreSummary;
};

export const getScoreHistory = (scoreSummary: ScoreSummary, scenario: ScenarioRunResult): Map<string, ScenarioRunResult> => {

    const scenarioName = scenario.scenarioName;
    const iterationName = scenario.iterationName;

    const scoreHistory = new Map<string, ScenarioRunResult>();
    for (const [key, node] of scoreSummary.executionHistory.entries()) {
        for (const leafNode of node.flattenedNodes) {
            if (leafNode.scenario?.scenarioName == scenarioName &&
                leafNode.scenario?.iterationName == iterationName) {
                scoreHistory.set(key, leafNode.scenario);
            }
        }
    }
    return scoreHistory;
};

const shortenPrompt = (prompt: string | undefined) => {
    if (!prompt) {
        return "";
    }
    if (prompt.length > 80) {
        return prompt.substring(0, 80) + "...";
    }
    return prompt;
};

const flattener = function* (node: ScoreNode): Iterable<ScoreNode> {
    if (node.isLeafNode) {
        yield node;
    } else {
        yield node;
        for (const child of node.childNodes) {
            yield* flattener(child);
        }
    }
};

const isTextContent = (content: AIContent): content is TextContent => {
    return (content as TextContent).text !== undefined;
};

export type ConversationDisplay = {
    messages: ChatMessageDisplay[];
    model?: string;
    usage?: UsageDetails;
};

export type ChatMessageDisplay = {
    role: string;
    participantName: string;
    content: string;
};

export const getConversationDisplay = (messages: ChatMessage[], modelResponse?: ChatResponse): ConversationDisplay => {
    const chatMessages: ChatMessageDisplay[] = [];

    for (const m of messages) {
        for (const c of m.contents) {
            if (isTextContent(c)) {
                const participantName = m.authorName ? `${m.authorName} (${m.role})` : m.role;
                chatMessages.push({
                    role: m.role,
                    participantName: participantName,
                    content: c.text
                });
            }
        }
    }

    if (modelResponse?.messages) {
        for (const m of modelResponse.messages) {
            for (const c of m.contents) {
                if (isTextContent(c)) {
                    const participantName = m.authorName ? `${m.authorName} (${m.role})` : m.role || 'Assistant';
                    chatMessages.push({
                        role: m.role,
                        participantName: participantName,
                        content: c.text
                    });
                }
            }
        }
    }

    return {
        messages: chatMessages,
        model: modelResponse?.modelId,
        usage: modelResponse?.usage
    };
};
