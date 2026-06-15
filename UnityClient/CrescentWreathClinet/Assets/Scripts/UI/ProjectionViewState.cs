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
        ProjectionChanged?.Invoke(projection);
    }
}
}
