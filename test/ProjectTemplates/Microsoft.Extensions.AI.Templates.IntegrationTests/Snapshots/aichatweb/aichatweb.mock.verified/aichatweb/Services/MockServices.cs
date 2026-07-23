using Microsoft.Extensions.AI;
namespace aichatweb.Services;

internal static class MockServices
{
    internal static MockChatClient CreateChatClient() =>
        new MockChatClient()
            .AddResponse(
                static _ => true,
                "Hi! I can help you plan an outdoor trip or find the right gear for it. What would you like to know?",
                minDelay: 500,
                maxDelay: 500,
                suggestions: ["What kind of adventures?", "What kinds of products?"])
            .AddResponse(
                static request => LastQuestionContains(request, "what kind of adventures", "outdoor activities"),
                "Mostly off-grid trips: hiking, backpacking, trail running, and mountaineering are the classics, and plenty of people use the same gear for camping and long day hikes. Anywhere you lose cell service, the right gear makes the trip safer and easier.",
                minDelay: 750,
                maxDelay: 1500,
                suggestions: ["Tell me about hiking adventures.", "Tell me about trail safety."])
            .AddResponse(
                static request => LastQuestionContains(request, "what kinds of products"),
                "Two main categories: adventure GPS watches for navigation and tracking, and emergency survival kits for backcountry safety. The GPS watch is the flagship, though a lot of people carry both. Want to dig into either one?",
                minDelay: 750,
                maxDelay: 1500,
                suggestions: ["Tell me about GPS watches.", "Tell me about survival kits."])
            .AddResponse(
                static request => LastQuestionContains(request, "hiking adventures", "hiking"),
                "Hiking is right in the TrailMaster GPS Watch's wheelhouse. It's made for hikers, backpackers, and trail runners heading into remote terrain, with offline topographic maps and real-time location sharing to keep you on route and reachable.",
                minDelay: 1000,
                maxDelay: 3000,
                suggestions: ["What does TrailMaster track?", "How rugged is TrailMaster?", "How does location sharing work?"],
                includeCitation: true)
            .AddResponse(
                static request => LastQuestionContains(request, "gps watches", "gps watch"),
                "The TrailMaster GPS Watch pairs precise positioning with a rugged build. It runs a multi-constellation receiver for GPS, GLONASS, and Galileo, plus an altimeter, barometer, and compass, so navigation holds up even where satellite coverage is thin.",
                minDelay: 1000,
                maxDelay: 3000,
                suggestions: ["What does TrailMaster track?", "What navigation tools are built in?", "How rugged is TrailMaster?"],
                includeCitation: true)
            .AddResponse(
                static request => LastQuestionContains(request, "trail safety", "safety"),
                "Trail safety really comes down to preparation. Before heading out, make sure the watch is charged and calibrated and that you know its controls, then use real-time location sharing so someone always knows where you are. For longer trips, an emergency survival kit is a smart backup.",
                minDelay: 1000,
                maxDelay: 3000,
                suggestions: ["How does location sharing work?", "How do I fix GPS signal loss?", "Tell me about survival kits."],
                includeCitation: true)
            .AddResponse(
                static request => LastQuestionContains(request, "survival kits", "survival kit", "emergency kit"),
                "The Life Guard X Emergency Survival Kit is built for when a trip goes sideways. It packs first aid supplies, high-calorie emergency food, a water purification system, an emergency shelter, and signaling tools so you can stay safe and reach help.",
                minDelay: 1000,
                maxDelay: 3000,
                suggestions: ["What's in the first aid supplies?", "How does water purification work?", "How do I signal for help?"],
                includeCitation: true)
            .AddResponse(
                static request => LastQuestionContains(request, "what does trailmaster track", "what does it track", "performance metrics", "heart rate", "elevation gain"),
                "It tracks the numbers that matter for training and pacing: distance traveled, speed, elevation gain, and heart rate. You can watch them live in tracking mode or review them after the activity.",
                minDelay: 1500,
                maxDelay: 7500,
                suggestions: [],
                includeCitation: true)
            .AddResponse(
                static request => LastQuestionContains(request, "how rugged", "rugged", "durable", "shock-resistant", "harsh conditions"),
                "It's built for abuse. The casing is durable and shock-resistant, and the reinforced strap and rugged design shrug off extreme temperatures, water, and impact out in the field.",
                minDelay: 1500,
                maxDelay: 7500,
                suggestions: [],
                includeCitation: true)
            .AddResponse(
                static request => LastQuestionContains(request, "location sharing", "share location", "share my location"),
                "Press the share button and pick the contacts you want to reach, and the watch sends your position to them in real time. It needs a stable GPS signal and your phone connected through the TrailMaster app.",
                minDelay: 1500,
                maxDelay: 7500,
                suggestions: [],
                includeCitation: true)
            .AddResponse(
                static request => LastQuestionContains(request, "navigation tools", "route planning", "waypoint", "trail tracking"),
                "Plenty to work with: preloaded topographic maps, trail tracking, waypoint management, and route planning, backed by an onboard compass, altimeter, and barometer for orientation and weather.",
                minDelay: 1500,
                maxDelay: 7500,
                suggestions: [],
                includeCitation: true)
            .AddResponse(
                static request => LastQuestionContains(request, "gps signal loss", "no gps signal", "lost gps", "gps troubleshooting"),
                "If it shows \"No GPS Signal,\" start with your surroundings: dense foliage, buildings, or terrain can block satellites. Check for RF interference, run a signal diagnostics test, and if it keeps happening, reach out to ExpeditionTech support.",
                minDelay: 1500,
                maxDelay: 7500,
                suggestions: [],
                includeCitation: true)
            .AddResponse(
                static request => LastQuestionContains(request, "bluetooth", "cannot connect", "connection problem", "connectivity issues"),
                "For pairing trouble, keep the watch within Bluetooth range, clear out nearby sources of interference, and update to the latest firmware. If it still won't connect, a Bluetooth signal analyzer can help track down the cause.",
                minDelay: 1500,
                maxDelay: 7500,
                suggestions: [],
                includeCitation: true)
            .AddResponse(
                static request => LastQuestionContains(request, "first aid"),
                "The first aid supplies cover the basics for minor injuries: adhesive bandages, sterile gauze and tape, antiseptic wipes and antibiotic ointment, over-the-counter medications, and tools like tweezers, scissors, and a thermometer. A first aid guide is included too.",
                minDelay: 1500,
                maxDelay: 7500,
                suggestions: [],
                includeCitation: true)
            .AddResponse(
                static request => LastQuestionContains(request, "water purification", "water filter", "purify water", "clean water"),
                "It comes with a portable water filter that removes 99.9999% of waterborne bacteria and 99.9% of protozoan parasites, so you can drink from rivers and lakes. Purification tablets are included too for an extra layer of protection.",
                minDelay: 1500,
                maxDelay: 7500,
                suggestions: [],
                includeCitation: true)
            .AddResponse(
                static request => LastQuestionContains(request, "signal for help", "signal mirror", "two-way radio", "signaling"),
                "For signaling, the kit includes a whistle for short range, a signal mirror that reflects sunlight to catch a rescuer's eye, and a weather-resistant two-way radio with up to 20 miles of range to reach your group or emergency services.",
                minDelay: 1500,
                maxDelay: 7500,
                suggestions: [],
                includeCitation: true);

    internal static IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator() =>
        new LexicalMockEmbeddingGenerator(IngestedChunk.VectorDimensions);

    private static bool LastQuestionContains(MockChatClientRequest request, params string[] terms)
    {
        string? question = GetLastQuestion(request);
        if (string.IsNullOrWhiteSpace(question))
        {
            return false;
        }

        foreach (string term in terms)
        {
            if (question.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string? GetLastQuestion(MockChatClientRequest request)
    {
        int startIndex = request.Options?.ResponseFormat is ChatResponseFormatJson
            ? request.Messages.Count - 2
            : request.Messages.Count - 1;

        for (int i = startIndex; i >= 0; i--)
        {
            ChatMessage message = request.Messages[i];
            if (message.Role == ChatRole.User && !string.IsNullOrWhiteSpace(message.Text))
            {
                return message.Text;
            }
        }

        return null;
    }
}
