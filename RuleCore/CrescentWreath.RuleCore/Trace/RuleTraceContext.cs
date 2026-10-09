using System.Collections.Generic;
using System.Threading;

namespace CrescentWreath.RuleCore.Trace;

public static class RuleTraceContext
{
    private static readonly AsyncLocal<IRuleTraceSink?> CurrentSink = new();

    public static IRuleTraceSink Sink
    {
        get => CurrentSink.Value ?? NullRuleTraceSink.Instance;
        set => CurrentSink.Value = value;
    }

    public static void Trace(string stepKey, params (string key, object? value)[] fields)
    {
        var values = new Dictionary<string, string>();
        foreach (var field in fields)
        {
            values[field.key] = field.value?.ToString() ?? "(null)";
        }

        Sink.Trace(stepKey, values);
    }
}
