using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using CrescentWreath.RuleCore.Trace;

namespace CrescentWreath.ServerPrototype;

public sealed class ServerTraceContext
{
    public string traceId { get; init; } = string.Empty;
    public long requestId { get; set; }
    public string actionType { get; set; } = string.Empty;
    public string sessionMode { get; init; } = string.Empty;
    public string remote { get; init; } = string.Empty;
    public string connectionId { get; init; } = string.Empty;
    public long? viewerPlayerNumericId { get; set; }
    public long? assignedPlayerNumericId { get; set; }
    public long? actorPlayerNumericId { get; set; }
}

public sealed class ServerTraceScope : IDisposable
{
    private readonly ServerTraceContext? previousContext;
    private readonly IRuleTraceSink previousRuleSink;
    private bool isDisposed;

    internal ServerTraceScope(ServerTraceContext context)
    {
        Context = context;
        previousContext = ServerTraceLogger.CurrentContext;
        previousRuleSink = RuleTraceContext.Sink;
        ServerTraceLogger.CurrentContext = context;
        RuleTraceContext.Sink = new ServerRuleTraceSink(context);
    }

    public ServerTraceContext Context { get; }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;
        RuleTraceContext.Sink = previousRuleSink;
        ServerTraceLogger.CurrentContext = previousContext;
    }
}

public static class ServerTraceLogger
{
    private static readonly AsyncLocal<ServerTraceContext?> Context = new();
    private static readonly object FileWriteLock = new();
    private static readonly string TraceLevel =
        Environment.GetEnvironmentVariable("CRESCENT_TRACE_LEVEL")?.Trim().ToLowerInvariant() ?? "debug";
    private static readonly Lazy<string?> TraceFilePath = new(createTraceFilePath);

    internal static ServerTraceContext? CurrentContext
    {
        get => Context.Value;
        set => Context.Value = value;
    }

    public static bool IsEnabled => !string.Equals(TraceLevel, "off", StringComparison.OrdinalIgnoreCase);

    public static bool IsDebugEnabled =>
        IsEnabled &&
        !string.Equals(TraceLevel, "info", StringComparison.OrdinalIgnoreCase);

    public static ServerTraceScope BeginRequest(
        string sessionMode,
        string remote,
        string connectionId,
        long? viewerPlayerNumericId,
        long? assignedPlayerNumericId,
        long requestId,
        string actionType)
    {
        var context = new ServerTraceContext
        {
            traceId = Guid.NewGuid().ToString("N")[..12],
            sessionMode = sessionMode,
            remote = remote,
            connectionId = connectionId,
            viewerPlayerNumericId = viewerPlayerNumericId,
            assignedPlayerNumericId = assignedPlayerNumericId,
            requestId = requestId,
            actionType = actionType,
        };
        return new ServerTraceScope(context);
    }

    public static void SetActor(long? actorPlayerNumericId)
    {
        if (CurrentContext is not null)
        {
            CurrentContext.actorPlayerNumericId = actorPlayerNumericId;
        }
    }

    public static void Trace(string stepKey, params (string key, object? value)[] fields)
    {
        if (!IsEnabled)
        {
            return;
        }

        write(stepKey, mergeFields(CurrentContext, fields));
    }

    public static void Debug(string stepKey, params (string key, object? value)[] fields)
    {
        if (!IsDebugEnabled)
        {
            return;
        }

        write(stepKey, mergeFields(CurrentContext, fields));
    }

    private static IReadOnlyDictionary<string, string> mergeFields(
        ServerTraceContext? context,
        IEnumerable<(string key, object? value)> fields)
    {
        var values = new Dictionary<string, string>
        {
            ["traceId"] = context?.traceId ?? "(none)",
            ["requestId"] = context?.requestId.ToString() ?? "(none)",
            ["action"] = context?.actionType ?? "(none)",
            ["mode"] = context?.sessionMode ?? "(none)",
            ["remote"] = context?.remote ?? "(none)",
            ["connectionId"] = context?.connectionId ?? "(none)",
            ["viewer"] = context?.viewerPlayerNumericId?.ToString() ?? "(none)",
            ["assigned"] = context?.assignedPlayerNumericId?.ToString() ?? "(none)",
            ["actor"] = context?.actorPlayerNumericId?.ToString() ?? "(none)",
        };

        foreach (var field in fields)
        {
            values[field.key] = field.value?.ToString() ?? "(null)";
        }

        return values;
    }

    private static void write(string stepKey, IReadOnlyDictionary<string, string> fields)
    {
        var builder = new StringBuilder();
        builder.Append("[Trace][");
        builder.Append(stepKey);
        builder.Append(']');
        foreach (var pair in fields)
        {
            builder.Append(' ');
            builder.Append(pair.Key);
            builder.Append('=');
            builder.Append(sanitize(pair.Value));
        }

        Console.WriteLine(builder.ToString());
        writeToFile(builder.ToString());
    }

    private static string sanitize(string value)
    {
        return value
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal);
    }

    private static string? createTraceFilePath()
    {
        if (!IsEnabled)
        {
            return null;
        }

        var explicitPath = Environment.GetEnvironmentVariable("CRESCENT_TRACE_FILE")?.Trim();
        var traceFilePath = string.IsNullOrWhiteSpace(explicitPath)
            ? Path.Combine(
                Directory.GetCurrentDirectory(),
                "Logs",
                "Trace",
                $"server-trace-{DateTime.Now:yyyyMMdd-HHmmss}.log")
            : explicitPath;

        try
        {
            var traceDirectory = Path.GetDirectoryName(traceFilePath);
            if (!string.IsNullOrWhiteSpace(traceDirectory))
            {
                Directory.CreateDirectory(traceDirectory);
            }

            File.AppendAllText(
                traceFilePath,
                $"[Trace][file.begin] startedAt={DateTime.Now:O} level={TraceLevel}{Environment.NewLine}",
                Encoding.UTF8);
            Console.WriteLine($"[Trace][file.ready] path={traceFilePath}");
            return traceFilePath;
        }
        catch (Exception exception)
        {
            Console.WriteLine($"[Trace][file.error] message={sanitize(exception.Message)}");
            return null;
        }
    }

    private static void writeToFile(string line)
    {
        var traceFilePath = TraceFilePath.Value;
        if (string.IsNullOrWhiteSpace(traceFilePath))
        {
            return;
        }

        try
        {
            lock (FileWriteLock)
            {
                File.AppendAllText(traceFilePath, line + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine($"[Trace][file.writeError] message={sanitize(exception.Message)}");
        }
    }
}

internal sealed class ServerRuleTraceSink : IRuleTraceSink
{
    private readonly ServerTraceContext context;

    public ServerRuleTraceSink(ServerTraceContext context)
    {
        this.context = context;
    }

    public void Trace(string stepKey, IReadOnlyDictionary<string, string> fields)
    {
        if (!ServerTraceLogger.IsDebugEnabled)
        {
            return;
        }

        var merged = new List<(string key, object? value)>();
        foreach (var pair in fields)
        {
            merged.Add((pair.Key, pair.Value));
        }

        ServerTraceLogger.Debug(stepKey, merged.ToArray());
    }
}
