using System;
using CrescentWreath.Client.Net;
using UnityEngine;

namespace CrescentWreath.Client.UI
{
public sealed class ProjectionViewState : MonoBehaviour
{
    public ProjectionViewModel? Current { get; private set; }

    public event Action<ProjectionViewModel>? ProjectionChanged;

    public void Apply(ProjectionViewModel projection)
    {
        Current = projection;
        var handlers = ProjectionChanged;
        if (handlers is null)
        {
            return;
        }

        foreach (Action<ProjectionViewModel> handler in handlers.GetInvocationList())
        {
            try
            {
                handler(projection);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[ProjectionViewState] ProjectionChanged handler failed: {exception}");
            }
        }
    }
}
}
