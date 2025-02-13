// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export enum ScoreNodeType {
    Group,
    Scenario,
    Iteration,
}

export class ScoreNode {
    constructor(name: string, nodeType: ScoreNodeType) {
        this.name = name;
        this.nodeType = nodeType;
        this.children = new Map<string, ScoreNode>();
    }
    private children: Map<string, ScoreNode>;

    nodeType: ScoreNodeType;
    name: string;
    scenario?: ScenarioRunResult;
    
    shortenedPrompt?: string;
    failed: boolean = false;
    numPassingIterations: number = 0;
    numFailingIterations: number = 0;
    numPassingScenarios: number = 0;
    numFailingScenarios: number = 0;

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
            const newChild = new ScoreNode(head, nodeType);
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
        return [...flattener(this, "")];
    }

    aggregate() {
        // Reset node to defaults before recalculating
        this.failed = false;
        this.numPassingIterations = 0;
        this.numFailingIterations = 0;
        this.numPassingScenarios = 0;
        this.numFailingScenarios = 0;

        if (this.isLeafNode) {
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
            const {history} = getPromptDetails(lastMessage ? [lastMessage] : [], this.scenario?.modelResponse);
            this.shortenedPrompt = shortenPrompt(history);
        } else {
            for (const child of this.childNodes) {
                child.aggregate();
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

    
};

export const DefaultRootNodeName = "All Evaluations";

export const createScoreTree = (dataset: Dataset): ScoreNode => {
    const root = new ScoreNode(DefaultRootNodeName, ScoreNodeType.Group);
    for (const scenario of dataset.scenarioRunResults) {
        const path = [...scenario.scenarioName.split('.'), scenario.iterationName];
        root.insertNode(path, scenario);
    }
    root.aggregate();
    return root;
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

function* flattener(node: ScoreNode, parentKey: string): Iterable<{key: string, node: ScoreNode}> {
    const key= `${parentKey}.${node.name}`;
    if (node.isLeafNode) {
        yield {key, node};
    } else {
        yield {key, node};
        for (const child of node.childNodes) {
            yield* flattener(child, key);
        }
    }
}

const isTextContent = (content: AIContent): content is TextContent => {
    return (content as TextContent).text !== undefined;
};

export const getPromptDetails = (messages: ChatMessage[], modelResponse?: ChatMessage): {history:string, response: string}=> {
    let history: string = "";
    if (messages.length === 1) {
        history = messages[0].contents.map(c => (c as TextContent).text).join("\n");
    } else if (messages.length > 1) {
        const historyItems: string[] = [];
        for (const m of messages) {
            for (const c of m.contents) {
                if (isTextContent(c)) {
                    const historyItem = m.authorName
                        ? `[${m.authorName} (${m.role})] ${c.text}` : `[${m.role}] ${c.text}`;
                    historyItems.push(historyItem);
                }
            }
        }
        history = historyItems.join("\n\n");
    }

    const response: string = modelResponse?.contents.map(c => (c as TextContent).text).join("\n") ?? "";

    return { history, response };
};