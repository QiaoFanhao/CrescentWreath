using System.Collections.Generic;

namespace CrescentWreath.RuleCore.Trace;

public sealed class NullRuleTraceSink : IRuleTraceSink
{
    public static readonly NullRuleTraceSink Instance = new();

    private NullRuleTraceSink()
    {
    }

    public void Trace(string stepKey, IReadOnlyDictionary<string, string> fields)
    {
    }
}
