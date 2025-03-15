using AspireAIChat.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.AI;
using System.ComponentModel;

namespace AspireAIChat.Web.Components.Pages.Chat;
public partial class Chat : IDisposable
{
    [Inject]
    public required IChatClient ChatClient {  get; set; }

    [Inject]
    public required SemanticSearch Search { get; set; }

    private const string SystemPrompt = @"
        You are an assistant who answers questions about information you retrieve.
        Do not answer questions about anything else.
        Use only simple markdown to format your responses.

        Use the search tool to find relevant information. When you do this, end your
        reply with citations in the special XML format:

        <citation filename='string' page_number='number'>exact quote here</citation>

        Always include the citation in your response if there are results.

        The quote must be max 5 words, taken word-for-word from the search result, and is the basis for why the citation is relevant.
        Don't refer to the presence of citations; just emit these tags right at the end, with no surrounding text.
        ";

    private readonly ChatOptions chatOptions = new();
    private readonly List<ChatMessage> messages = [];
    private CancellationTokenSource? currentResponseCancellation;
    private ChatMessage? currentResponseMessage;
    private ChatInput? chatInput;
    private ChatSuggestions? chatSuggestions;

    protected override void OnInitialized()
    {
        messages.Add(new(ChatRole.System, SystemPrompt));
        chatOptions.Tools = [
            AIFunctionFactory.Create(SearchAsync),
            AIFunctionFactory.Create(GetWeather)
        ];
    }

    private async Task AddUserMessageAsync(ChatMessage userMessage)
    {
        CancelAnyCurrentResponse();

        // Add the user message to the conversation
        messages.Add(userMessage);
        chatSuggestions?.Clear();
        await chatInput!.FocusAsync();

        // Display a new response from the IChatClient, streaming responses
        // aren't supported because Ollama will not support both streaming and using Tools
        currentResponseCancellation = new();
        var response = await ChatClient.GetResponseAsync(messages, chatOptions, currentResponseCancellation.Token);
        currentResponseMessage = response.Message;
        ChatMessageItem.NotifyChanged(currentResponseMessage);

        // Store the final response in the conversation, and begin getting suggestions
        messages.Add(currentResponseMessage!);
        currentResponseMessage = null;
        chatSuggestions?.Update(messages);
    }

    private void CancelAnyCurrentResponse()
    {
        // If a response was cancelled while streaming, include it in the conversation so it's not lost
        if (currentResponseMessage is not null)
        {
            messages.Add(currentResponseMessage);
        }

        currentResponseCancellation?.Cancel();
        currentResponseMessage = null;
    }

    private async Task ResetConversationAsync()
    {
        CancelAnyCurrentResponse();
        messages.Clear();
        messages.Add(new(ChatRole.System, SystemPrompt));
        chatSuggestions?.Clear();
        await chatInput!.FocusAsync();
    }

    [Description("Searches for information using a phrase or keyword")]
    private async Task<IEnumerable<string>> SearchAsync(
        [Description("The phrase to search for.")] string searchPhrase,
        [Description("Whenever possible, specify the filename to search that file only. If not provided, the search includes all files.")] string? filenameFilter = null)
    {
        await InvokeAsync(StateHasChanged);
        var results = await Search.SearchAsync(searchPhrase, filenameFilter, maxResults: 5);
        return results.Select(result =>
            $"<result filename=\"{result.FileName}\" page_number=\"{result.PageNumber}\">{result.Text}</result>");
    }

    private Task<string> GetWeather([Description("The city, correctly capitalized")] string city)
    {
        string[] weatherValues = ["Sunny", "Cloudy", "Rainy", "Snowy", "Balmy", "Bracing"];
        return Task.FromResult(city == "London" ? "Drizzle" : weatherValues[Random.Shared.Next(weatherValues.Length)]);
    }

    public void Dispose()
        => currentResponseCancellation?.Cancel();
}