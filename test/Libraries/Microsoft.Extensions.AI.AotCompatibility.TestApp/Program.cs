// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CA1031 // Do not catch general exception types
#pragma warning disable S108 // Nested blocks of code should not be left empty
#pragma warning disable S125 // Remove this commented out code

using Microsoft.Extensions.AI;

// Use types from each library

// Microsoft.Extensions.AI.AzureAIInference
// using var a = new Azure.AI.Inference.ChatCompletionClient(new Uri("http://localhost"), new("apikey")); // uncomment once warnings in Azure.AI.Inference are addressed

// Microsoft.Extensions.AI.OpenAI
// using var c = new OpenAI.OpenAIClient("apikey").AsChatClient("gpt-4o-mini"); // uncomment once warnings in OpenAI are addressed

// Microsoft.Extensions.AI.Ollama
using var b = new OllamaChatClient("http://localhost:11434", "llama3.2");

// Microsoft.Extensions.AI
AIFunctionFactory.Create(() => { });

System.Console.WriteLine("Success!");
