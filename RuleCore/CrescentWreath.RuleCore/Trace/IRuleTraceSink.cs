using System.Collections.Generic;

namespace CrescentWreath.RuleCore.Trace;

public interface IRuleTraceSink
{
    void Trace(string stepKey, IReadOnlyDictionary<string, string> fields);
}
