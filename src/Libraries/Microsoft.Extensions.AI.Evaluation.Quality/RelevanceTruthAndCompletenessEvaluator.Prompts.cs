// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI.Evaluation.Quality;

public partial class RelevanceTruthAndCompletenessEvaluator
{
    private static class Prompts
    {
        internal static string BuildEvaluationPrompt(string userQuery, string modelResponse, string history)
        {
#pragma warning disable S103 // Lines should not be too long
            return
                $$"""
                Read the History, User Query, and Model Response below and produce your response as a single JSON object.
                Do not include any other text in your response besides the JSON object.

                The JSON object should have the following format. However, do not include any markdown tags in your
                response. Your response should start with an open curly brace and end with a closing curly brace for the
                JSON.
                ```
                {
                    "relevance": 1,
                    "truth": 1,
                    "completeness": 1
                }
                ```

                -----

                History: {{history}}

                -----

                User Query: {{userQuery}}

                -----

                Model Response: {{modelResponse}}

                -----

                That's the History, User Query, and Model Response you will rate. Now, in 3 Steps, you will evaluate the Model Response on 3 criteria.

                -----

                Step 1: Rate the relevance of the response.

                Regardless of truth of statements, how much of the response is directly related to the request?

                1 = None of the response is at all related
                2 = A little of the response is directly related, or the response is a little bit related
                3 = Some of the response is related, or the response is somewhat related
                4 = Most of the response is related, or the response is mostly related
                5 = 100% of the response is 100% related to the request

                Record your response as the value of the "relevance" property in the JSON output.

                -----

                Step 2: Rate the truth of the response.

                Read the History, Query, and Model Response again.

                Regardless of relevance, how true are the verifiable statements in the response?

                1 = The entire response is totally false
                2 = A little of the response is true, or the response is a little bit true
                3 = Some of the response is true, or the response is somewhat true
                4 = Most of the response is true, or the response is mostly true
                5 = 100% of the response is 100% true

                Record your response as the value of the "truth" property in the JSON output.

                -----

                Step 3: Rate the completeness of the response.

                Read the History, Query, and Model Response again.

                Regardless of whether the statements made in the response are true, how many of the points necessary to address the request, does the response contain?

                1 = The response omits all points that are necessary to address the request.
                2 = The response includes a little of the points that are necessary to address the request.
                3 = The response includes some of the points that are necessary to address the request.
                4 = The response includes most of the points that are necessary to address the request.
                5 = The response includes all points that are necessary to address the request. For explain tasks, nothing is left unexplained. For improve tasks, I looked for all potential improvements, and none were left out. For fix tasks, the response purports to get the user all the way to a fixed state (regardless of whether it actually works). For "do task" responses, it does everything requested.

                Record your response as the value of the "completeness" property in the JSON output.

                -----
                """;
#pragma warning restore S103
        }

        internal static string BuildEvaluationPromptWithReasoning(
            string userQuery,
            string modelResponse,
            string history)
        {
#pragma warning disable S103 // Lines should not be too long
            return
                $$"""
                Read the History, User Query, and Model Response below and produce your response as a single JSON object.
                Do not include any other text in your response besides the JSON object. Make sure the response is a valid
                JSON object.

                The JSON object should have the following format. However, do not include any markdown tags in your
                response. Your response should start with an open curly brace and end with a closing curly brace for the
                JSON.
                ```
                {
                    "relevance": 1,
                    "relevanceReasoning": "The reason for the relevance score",
                    "relevanceReasons": ["relevance_reason_distant_topic"],
                    "truth": 1,
                    "truthReasoning": "The reason for the truth score",
                    "truthReasons": ["truth_reason_incorrect_information", "truth_reason_outdated_information", "truth_reason_misleading_incorrectforintent"],
                    "completeness": 1,
                    "completenessReasoning": "The reason for the completeness score",
                    "completenessReasons": ["completeness_reason_no_solution", "completeness_reason_genericsolution_missingcode"],
                }
                ```

                -----

                History: {{history}}

                -----

                User Query: {{userQuery}}

                -----

                Model Response: {{modelResponse}}

                -----

                That's the History, User Query, and Model Response you will rate. Now, in 3 Steps, you will evaluate the Model Response on 3 criteria.

                -----

                Step 1: Rate the relevance of the response.

                Regardless of truth of statements, how much of the response is directly related to the request?

                1 = None of the response is at all related
                2 = A little of the response is directly related, or the response is a little bit related
                3 = Some of the response is related, or the response is somewhat related
                4 = Most of the response is related, or the response is mostly related
                5 = 100% of the response is 100% related to the request

                Step 1a. Record your response as the value of the "relevance" property in the JSON output.

                Step 1b. Now, record your reasoning for that score as the value of the "relevanceReasoning" property. If the score is 5, record the value of "relevanceReasoning" as "100% of the response is 100% related to the request". If the score is not 5, write your Reasoning as fewer than 100 words and prioritizing the most important contributing reasons to the score.

                Step 1c. Now, record whether your relevance reasoning includes each of these categories, as an array of strings for the value of the "relevanceReasons" property. The possible values for this array are "relevance_reason_distant_topic", "relevance_reason_scope_too_small", "relevance_reason_scope_too_large", "relevance_reason_wrong_intent_served", or "relevance_reason_other". If the score is 5, the array should be empty.
                - relevance_reason_distant_topic. In a taxonomy of programming-related topics, the response is a non-zero distance from the user request. Example: User: "How can I implement a binary search in Python?" Model Response: "HTML (HyperText Markup Language) is the standard markup language for web pages."
                - relevance_reason_scope_too_small. Scope or level of abstraction is smaller than user intent. Example: User: "Can you explain object-oriented programming?" Model Response: "In object-oriented programming, a class is a blueprint for creating objects."
                - relevance_reason_scope_too_large. Scope or level of abstraction is larger than user intent.
                Example: User: "Why am I getting a null pointer exception here?" Model Response: "A Null Pointer Exception in Java occurs when you try to use a reference that points to no location in memory (null) as though it were referencing an object."
                - relevance_reason_wrong_intent_served. Alignment with inferred or explicit intent, or in absence of that, general standards. Example: User: "convert a JSON object to a Python dictionary" Model Response: "JSON is a lightweight data-interchange format that is easy for humans to read and write and easy for machines to parse and generate." (The user intends to write code and the model is serving an intent to explain a concept)
                - relevance_reason_other. Any other reasoning.

                -----

                Step 2: Rate the truth of the response.

                Read the History, Query, and Model Response again.

                Regardless of relevance, how true are the verifiable statements in the response?

                1 = The entire response is totally false
                2 = A little of the response is true, or the response is a little bit true
                3 = Some of the response is true, or the response is somewhat true
                4 = Most of the response is true, or the response is mostly true
                5 = 100% of the response is 100% true

                Step 2a. Record your response as the value of the "truth" property in the JSON output.

                Step 2b. Now, record your reasoning for that score as the value of the "truthReasoning" property. If the score is 5, record the value of "truthReasoning" as "100% of the response is 100% true". If the score is not 5, write your Reasoning as fewer than 100 words and prioritizing the most important contributing reasons to the score.

                Step 2c. Now, record whether your truth reasoning includes each of these categories, as an array of strings for the value of the "truthReasons" property. The possible values for this array are "truth_reason_incorrect_information", "truth_reason_outdated_information", "truth_reason_misleading_incorrectforintent", or "truth_reason_other". If the score is 5, the array should be empty.
                - truth_reason_incorrect_information. The response contains information that is factually incorrect. Example: User: "What is the time complexity of quicksort?" Model Response: "Quicksort has a time complexity of O(n)."
                - truth_reason_outdated_information. The response contains information that was once true but is no longer true. Example: User: "How do I install Python 2?" Model Response: "You can install Python 2 using the command sudo apt-get install python."
                - truth_reason_misleading_incorrectforintent. The response is true but irrelevant to the user's intent, causing results that are incorrect for the user's context. User: "How do I sort a list in Python?" Model Response: "You can use the sorted() function to sort a list in Python." (sorted() returns a new sorted list, leaving the original list unchanged. If the user's intent was to sort the original list, they should use list.sort().)
                - truth_reason_other. any other reasoning.

                -----

                Step 3: Rate the completeness of the response.

                Read the History, Query, and Model Response again.

                Regardless of whether the statements made in the response are true, how many of the points necessary to address the request, does the response contain?

                1 = The response omits all points that are necessary to address the request.
                2 = The response includes a little of the points that are necessary to address the request.
                3 = The response includes some of the points that are necessary to address the request.
                4 = The response includes most of the points that are necessary to address the request.
                5 = The response includes all points that are necessary to address the request. For explain tasks, nothing is left unexplained. For improve tasks, I looked for all potential improvements, and none were left out. For fix tasks, the response purports to get the user all the way to a fixed state (regardless of whether it actually works). For "do task" responses, it does everything requested.

                Step 3a. Record your response as the value of the "completeness" property in the JSON output.

                Step 3b. Now, record your reasoning for that score as the value of the "completenessReasoning" property. If the score is 5, record the value of "completenessReasoning" as "The response includes all points that are necessary to address the request". If the score is not 5, write your Reasoning as fewer than 100 words and prioritizing the most important contributing reasons to the score.

                Step 3c. Now, record whether your completeness reasoning includes each of these categories, as an array of strings for the value of the "completenessReasons" property. The possible values for this array are "completeness_reason_no_solution", "completeness_reason_lacks_information_about_solution", "completeness_reason_genericsolution_missingcode", "completeness_reason_generic_code", "completeness_reason_failed_to_change_code", "completeness_reason_failed_to_change_code", "completeness_reason_incomplete_list", "completeness_reason_incomplete_code", "completeness_reason_missing_warnings", or "completeness_reason_other". If the score is 5, the array should be empty.
                - completeness_reason_no_solution. The model response does not achieve or offer a solution to the user intent. Example 1: User: "How can I implement a binary search in Python?" Model Response: "Binary search is a search algorithm." Example 2: User: "How can I implement a binary search in Python?" Model Response: "500 error"
                - completeness_reason_lacks_information_about_solution. The model response does not include enough information about its solution, such as why its solution is reasonable, or how it addresses the user intent. Example: User: "How can I reverse a string in Python?" Model Response: "Hello, World!"[::-1]"
                - completeness_reason_genericsolution_missingcode. The user intends to generate code or get help writing code. The model response includes a response that solves the problem generically, but does not include code. Example: User: "How can I implement a binary search in Python?" Model Response: "You can implement a binary search by dividing the search space in half each time you fail to find the target value."
                - completeness_reason_generic_code. The user intends to generate code or get help writing code that uses specific functions, names, or other components in their current code. The model response includes generic code, and does not modify or use components from the user's current code. Example: User: "How do I use my foo function?" Model Response: "Here's how you can use a function in Python: function_name()."
                - completeness_reason_failed_to_change_code. The user intends to generate code or get help writing code, but the model response returns code that the user already has.
                - completeness_reason_incomplete_list. Serving the user intent requires several natural language components, such as a description of some concept, or a list of system capabilities, reasons to use a particular approach, or problems with code, but the model response addresses fewer than all of the required components or misses parts of components. Example: User: "What are the steps to implement a binary search in Python?" Model Response: "The first step in implementing a binary search is to sort the array."
                - completeness_reason_incomplete_code. Serving the user intent requires several code components, such as library imports, object creations and manipulations, and the model offers code, but the code offers fewer than all of the required components. Example: User: "How can I read a CSV file in Python?" Model response: "You can import the pandas library: `import pandas`."
                - completeness_reason_lazy_unopinionated. The model claims not to know how, or not be certain enough, to address the user intent and does not offer the user any recourse (e.g., asking the user to be more specific, or offering potential subtopics for ambiguous user requests). Example: User: "compile error" Model response: "I can't help with that, I need more information." (The response doesn't offer any typical troubleshooting ideas based on the user's code, context, or general programming knowledge.)
                - completeness_reason_missing_warnings. The response has potential pitfalls or dangers, but does not warn the user about them. Example: User: "How do I delete a file in Python?" Model Response: "You can use os.remove()." (This response should warn the user that this operation is irreversible and should be done with caution.)
                - completeness_reason_other. Any other reasoning.

                -----
                """;
#pragma warning restore S103
        }
    }
}
