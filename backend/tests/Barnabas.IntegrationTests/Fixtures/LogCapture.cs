using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.Extensions.Logging;

namespace Barnabas.IntegrationTests.Fixtures;

/// <summary>
/// Everything the API logged, with the scopes each entry was inside.
/// </summary>
/// <remarks>
/// <see cref="ServerErrorLog"/> keeps exceptions so a failure can say why; this keeps entries so
/// the suite can say what was written down. They are separate because they answer opposite
/// questions — one is a debugging aid, and this one is the assertion that <c>L2-119</c> is met.
/// <para>
/// The scopes matter as much as the message. <c>L2-117 AC1</c> asks for a correlation identifier
/// on every entry, and it is not in the message text: it is in the scope the middleware opened,
/// which is exactly how a real log processor would find it.
/// </para>
/// </remarks>
public sealed class LogCapture : ILoggerProvider
{
    /// <summary>
    /// The scopes open on this asynchronous flow.
    /// </summary>
    /// <remarks>
    /// Kept here rather than through <c>ISupportExternalScope</c>. A provider that declares
    /// support for external scopes is never asked to open one - the factory pushes onto its own
    /// shared provider instead - and reading them back out of that is a race against every other
    /// provider in the process. Owning the stack means this sees exactly the scopes that were
    /// open when an entry was written, which is the whole assertion.
    /// </remarks>
    private static readonly AsyncLocal<ImmutableStack<object>> Open = new();

    private readonly ConcurrentQueue<LoggedEntry> _entries = new();

    public IReadOnlyList<LoggedEntry> Entries => [.. _entries];

    /// <summary>Every entry's message and its scope values, as one string to search.</summary>
    public string Everything => string.Join(
        Environment.NewLine,
        Entries.Select(entry => $"{entry.Message} {string.Join(" ", entry.Scopes.Select(Describe))}"));

    public ILogger CreateLogger(string categoryName) => new Recorder(this, categoryName);

    public void Clear() => _entries.Clear();

    public void Dispose() => Clear();

    /// <summary>The entries carrying a scope value with the given name.</summary>
    public IReadOnlyList<LoggedEntry> WithScope(string name) =>
        [.. Entries.Where(entry => entry.ScopeValue(name) is not null)];

    private static string Describe(object scope) => scope switch
    {
        IEnumerable<KeyValuePair<string, object?>> pairs =>
            string.Join(" ", pairs.Select(pair => $"{pair.Key}={pair.Value}")),
        _ => scope.ToString() ?? string.Empty,
    };

    private void Record(LogLevel level, string category, string message, Exception? exception)
    {
        var scopes = (Open.Value ?? ImmutableStack<object>.Empty).ToList();

        _entries.Enqueue(new LoggedEntry(level, category, message, exception, scopes));
    }

    private static IDisposable Push(object state)
    {
        var previous = Open.Value ?? ImmutableStack<object>.Empty;

        Open.Value = previous.Push(state);

        return new Popper(previous);
    }

    private sealed class Popper : IDisposable
    {
        private readonly ImmutableStack<object> _previous;

        public Popper(ImmutableStack<object> previous) => _previous = previous;

        public void Dispose() => Open.Value = _previous;
    }

    private sealed class Recorder : ILogger
    {
        private readonly LogCapture _capture;
        private readonly string _category;

        public Recorder(LogCapture capture, string category)
        {
            _capture = capture;
            _category = category;
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => Push(state);

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);

            if (IsEnabled(logLevel))
            {
                _capture.Record(logLevel, _category, formatter(state, exception), exception);
            }
        }
    }
}

/// <summary>One thing the API wrote down.</summary>
public sealed record LoggedEntry(
    LogLevel Level,
    string Category,
    string Message,
    Exception? Exception,
    IReadOnlyList<object> Scopes)
{
    /// <summary>The value of a named scope key, or null when no scope carried one.</summary>
    public string? ScopeValue(string name)
    {
        foreach (var scope in Scopes)
        {
            if (scope is IEnumerable<KeyValuePair<string, object?>> pairs)
            {
                foreach (var pair in pairs)
                {
                    if (pair.Key == name)
                    {
                        return pair.Value?.ToString();
                    }
                }
            }
        }

        return null;
    }
}
