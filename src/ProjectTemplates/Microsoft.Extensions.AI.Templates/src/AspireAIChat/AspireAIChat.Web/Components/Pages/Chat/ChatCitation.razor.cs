using Microsoft.AspNetCore.Components;
using System.Web;

namespace AspireAIChat.Web.Components.Pages.Chat;
public partial class ChatCitation
{
    [Parameter]
    public required string File { get; set; }

    [Parameter]
    public int? PageNumber { get; set; }

    [Parameter]
    public required string Quote { get; set; }

    private string? viewerUrl;

    protected override void OnParametersSet()
    {
        viewerUrl = null;

        // If you ingest other types of content besides PDF files, construct a URL to an appropriate viewer here
        if (File.EndsWith(".pdf"))
        {
            var search = Quote?.Trim('.', ',', ' ', '\n', '\r', '\t', '"', '\'');
            viewerUrl = $"lib/pdf_viewer/viewer.html?file=/Data/{HttpUtility.UrlEncode(File)}#page={PageNumber}&search={HttpUtility.UrlEncode(search)}&phrase=true";
        }
    }
}