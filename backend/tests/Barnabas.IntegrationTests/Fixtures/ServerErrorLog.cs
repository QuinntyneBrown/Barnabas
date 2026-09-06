using Microsoft.Extensions.Logging;

namespace Barnabas.IntegrationTests.Fixtures;

/// <summary>
/// Keeps the exceptions the API logged, so a failing assertion can say why.
/// </summary>
/// <remarks>
/// Without this a handler that throws shows up in the suite as "expected 201, got 500", which
/// says nothing about what went wrong and sends whoever is reading it off to reproduce the
/// request by hand. The stack trace is right there; it may as well be in the failure.
/// </remarks>
public sealed class ServerErrorLog : ILoggerProvider
{
    private readonly List<Exception> _errors = [];

    public IReadOnlyList<Exception> Errors
    {
        get
        {
            lock (_errors)
            {
                return [.. _errors];
            }
        }
    }

    public string? Summary => Errors.Count == 0
        ? null
        : string.Join(Environment.NewLine, Errors.Select(error => error.ToString()));

    public ILogger CreateLogger(string categoryName) => new Recorder(this);

    public void Clear()
    {
        lock (_errors)
        {
            _errors.Clear();
        }
    }

    public void Dispose() => Clear();

    private void Record(Exception exception)
    {
        lock (_errors)
        {
            _errors.Add(exception);
        }
    }

    private sealed class Recorder : ILogger
    {
        private readonly ServerErrorLog _log;

        public Recorder(ServerErrorLog log) => _log = log;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Error && exception is not null)
            {
                _log.Record(exception);
            }
        }
    }
}
