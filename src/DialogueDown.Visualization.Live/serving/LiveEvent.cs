namespace DialogueDown.Visualization.Live.Serving;

/// <summary>
/// A server-sent event pushed to live clients: a named event and its JSON data.
/// </summary>
/// <param name="Event">The SSE event name (<c>reload</c> or <c>problem</c>).</param>
/// <param name="Data">The event payload as a single-line JSON string.</param>
internal sealed record LiveEvent(string Event, string Data);
