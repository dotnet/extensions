using Microsoft.AspNetCore.Components;

namespace AspireAIChat.Web.Components.Pages.Chat;
public partial class ChatHeader
{
    [Parameter]
    public EventCallback OnNewChat { get; set; }
}