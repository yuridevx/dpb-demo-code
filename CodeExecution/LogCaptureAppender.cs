using log4net.Appender;
using log4net.Core;

namespace GoBo.Infrastructure.CodeExecution;

/// <summary>
/// A log4net appender that captures log events to a list for later retrieval.
/// Used to capture logs during script execution sessions.
/// </summary>
public class LogCaptureAppender : AppenderSkeleton
{
    private readonly List<LogEntry> _entries = [];
    private readonly int _maxEntries;

    public LogCaptureAppender(int maxEntries = 1000)
    {
        _maxEntries = maxEntries;
        Name = $"LogCapture_{Guid.NewGuid():N}";
    }

    public IReadOnlyList<LogEntry> Entries
    {
        get
        {
            lock (_entries)
            {
                return _entries.ToList();
            }
        }
    }

    protected override void Append(LoggingEvent loggingEvent)
    {
        var entry = new LogEntry(
            loggingEvent.TimeStamp,
            loggingEvent.Level.Name,
            loggingEvent.LoggerName,
            loggingEvent.RenderedMessage,
            loggingEvent.ExceptionObject?.ToString()
        );

        lock (_entries)
        {
            _entries.Add(entry);

            // Trim old entries if over limit
            while (_entries.Count > _maxEntries)
            {
                _entries.RemoveAt(0);
            }
        }
    }

    public void Clear()
    {
        lock (_entries)
        {
            _entries.Clear();
        }
    }
}

public record LogEntry(
    DateTime Timestamp,
    string Level,
    string Logger,
    string Message,
    string Exception
);
