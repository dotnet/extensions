using Microsoft.Extensions.AI;

internal static class MockServices
{
    internal static MockChatClient CreateChatClient() =>
        new MockChatClient()
            .AddResponse(
                static request => IsWriterStage(request),
                """
                ## Welcome to the Mock Writing Room

                Meet The Writer, The Editor, and The Formatter. They are ready for their curtain call.

                Try: **Write a .NET story.**, **Edit this story.**, or **Format for publishing.**
                """,
                maxDelay: 2000)
            .AddResponse(
                static request => HasMessageContent(request, "Welcome to the Mock Writing Room"),
                """
                The room is ready when you are. Bring a draft, and it will come back polished.
                """,
                maxDelay: 2000)
            .AddResponse(
                static request => IsWriterStage(request) && IsWritePrompt(request),
                """
                Maya opened her first .NET console project during an afternoon study session. The program printed "Hello, World!", but she wanted it to ask for a number and tell her whether it was even.

                At first, compiler errors made every small change feel impossible. Instead of guessing, she read each message, used `int.TryParse`, and ran the app after every improvement.

                By the end of the session, Maya had a working program and a short list of ideas for her next project. She realized that learning .NET was not about knowing everything at once; it was about trying one small step, seeing what happened, and learning from it.

                """,
                maxDelay: 2000)
            .AddResponse(
                static request => HasDraftContent(request, "Maya opened her first .NET console project during an afternoon study session."),
                """
                **Title**: Maya's First Green Check

                Maya opened her first .NET console project during an afternoon study session. It printed "Hello, World!", but she wanted it to ask for a number and report whether it was even.

                Compiler errors made every small change feel impossible at first. Rather than guessing, Maya read each message, used `int.TryParse` to handle input safely, and ran the app after every improvement.

                By the end of the session, she had a working program and a short list of ideas for her next project. Maya learned that .NET did not require her to know everything at once: progress came from taking one small step, observing the result, and learning from it.
                """,
                maxDelay: 2000)
            .AddResponse(
                static request => IsWriterStage(request) && IsEditPrompt(request),
                """
                maya opened her first .net console project and wanted it to check whether a number was even. compiler errors made her frustrated at first.

                she read the error messages, used int.tryparse, and kept running the program after each change. by the end, it worked and she was excited to build another project.

                """,
                maxDelay: 2000)
            .AddResponse(
                static request => HasDraftContent(request, "maya opened her first .net console project and wanted it to check whether a number was even."),
                """
                Maya opened her first .NET console project hoping to write a program that could identify even numbers. Compiler errors frustrated her at first, but she chose to slow down and read each message carefully.

                She used `int.TryParse` to make the input safer and ran the program after every change. By the end of the session, Maya had a working app and a new confidence that learning .NET happens one experiment at a time.
                """,
                maxDelay: 2000)
            .AddResponse(
                static request => IsWriterStage(request) && IsFormatPrompt(request),
                """
                Maya learned to build her first .NET console app by reading compiler errors, trying `int.TryParse`, and testing each small change.

                """,
                maxDelay: 2000)
            .AddResponse(
                static request => HasDraftContent(request, "Maya learned to build her first .NET console app by reading compiler errors, trying `int.TryParse`, and testing each small change."),
                """
                **Title**: Maya's First .NET Project

                Maya began her first .NET console app with a simple goal: identify whether a number was even. When compiler errors appeared, she slowed down, read the messages, and made one change at a time.

                She tried `int.TryParse`, tested each improvement, and watched the program gradually come together. The project taught Maya that every small experiment is part of becoming a confident .NET developer.
                """,
                maxDelay: 2000);

    private static bool IsWritePrompt(MockChatClientRequest request) =>
        HasInteractivePrompt(request, "write");

    private static bool IsEditPrompt(MockChatClientRequest request) =>
        HasInteractivePrompt(request, "edit");

    private static bool IsFormatPrompt(MockChatClientRequest request) =>
        HasInteractivePrompt(request, "format");

    private static bool IsWriterStage(MockChatClientRequest request) =>
        !HasAssistantMessage(request);

    private static bool HasAssistantMessage(MockChatClientRequest request)
    {
        foreach (ChatMessage message in request.Messages)
        {
            if (message.Role == ChatRole.Assistant &&
                !string.IsNullOrWhiteSpace(message.Text))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasDraftContent(MockChatClientRequest request, string text) =>
        request.LastUserText?.Contains(text, StringComparison.OrdinalIgnoreCase) is true ||
        HasAssistantContent(request, text);

    private static bool HasInteractivePrompt(MockChatClientRequest request, string text)
    {
        string? lastUserText = request.LastUserText;
        return lastUserText?.Contains(text, StringComparison.OrdinalIgnoreCase) is true &&
            lastUserText.Contains("Welcome to the Mock Writing Room", StringComparison.OrdinalIgnoreCase) is false;
    }

    private static bool HasMessageContent(MockChatClientRequest request, string text)
    {
        foreach (ChatMessage message in request.Messages)
        {
            if (message.Text?.Contains(text, StringComparison.OrdinalIgnoreCase) is true)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasAssistantContent(MockChatClientRequest request, string text)
    {
        foreach (ChatMessage message in request.Messages)
        {
            if (message.Role == ChatRole.Assistant &&
                message.Text?.Contains(text, StringComparison.OrdinalIgnoreCase) is true)
            {
                return true;
            }
        }

        return false;
    }
}
