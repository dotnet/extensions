using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.AI;
using Microsoft.JSInterop;

namespace AspireAIChat.Web.Components.Pages.Chat;
public partial class ChatMessageList
{
    [Parameter]
    public required IEnumerable<ChatMessage> Messages { get; set; }

    [Parameter]
    public ChatMessage? InProgressMessage { get; set; }

    [Parameter]
    public RenderFragment? NoMessagesContent { get; set; }

    [Inject]
    public required IJSRuntime JS { get; set; }

    private bool IsEmpty => !Messages.Any(m => (m.Role == ChatRole.User || m.Role == ChatRole.Assistant) && !string.IsNullOrEmpty(m.Text));

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Activates the auto-scrolling behavior
            await JS.InvokeVoidAsync("import", "./Components/Pages/Chat/ChatMessageList.razor.js");
        }
    }
}