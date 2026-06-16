using UnityEngine;

namespace CrescentWreath.Client.UI
{
public enum BattlefieldZoneLayoutType
{
    single,
    horizontalRow,
    verticalColumn,
    grid,
    stack,
    bounds,
}

[ExecuteAlways]
public sealed class BattlefieldZoneAnchor : MonoBehaviour
{
    [SerializeField]
    private string zoneViewKey = string.Empty;

    [SerializeField]
    private BattlefieldZoneLayoutType layoutType = BattlefieldZoneLayoutType.single;

    [SerializeField]
    [Min(1)]
    private int maxVisibleCards = 1;

    [SerializeField]
    private Vector2 cardSpacing = new(1.25f, 1.7f);

    [SerializeField]
    private Vector3 cardScale = new(0.72f, 0.02f, 1f);

    [SerializeField]
    private Vector3 localRotation = Vector3.zero;

    [SerializeField]
    private bool readOnly = true;

    public string ZoneViewKey => zoneViewKey;
    public BattlefieldZoneLayoutType LayoutType => layoutType;
    public int MaxVisibleCards => maxVisibleCards;
    public Vector2 CardSpacing => cardSpacing;
    public Vector3 CardScale => cardScale;
    public Vector3 LocalRotation => localRotation;
    public bool ReadOnly => readOnly;

    public void Configure(
        string newZoneViewKey,
        BattlefieldZoneLayoutType newLayoutType,
        int newMaxVisibleCards,
        Vector2 newCardSpacing,
        Vector3 newCardScale,
        Vector3 newLocalRotation,
        bool isReadOnly)
    {
        zoneViewKey = newZoneViewKey;
        layoutType = newLayoutType;
        maxVisibleCards = Mathf.Max(1, newMaxVisibleCards);
        cardSpacing = newCardSpacing;
        cardScale = newCardScale;
        localRotation = newLocalRotation;
        readOnly = isReadOnly;
    }

    private void OnDrawGizmos()
    {
        var previousMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = readOnly
            ? new Color(0.15f, 0.78f, 0.9f, 0.65f)
            : new Color(0.96f, 0.73f, 0.2f, 0.72f);
        Gizmos.DrawWireCube(Vector3.zero, calculateBounds());
        Gizmos.DrawLine(Vector3.zero, Vector3.forward * 0.55f);
        Gizmos.matrix = previousMatrix;

#if UNITY_EDITOR
        UnityEditor.Handles.color = Gizmos.color;
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.18f,
            string.IsNullOrWhiteSpace(zoneViewKey) ? gameObject.name : zoneViewKey);
#endif
    }

    private Vector3 calculateBounds()
    {
        var width = Mathf.Max(cardScale.x, 0.1f);
        var depth = Mathf.Max(cardScale.z, 0.1f);
        switch (layoutType)
        {
            case BattlefieldZoneLayoutType.horizontalRow:
                width += cardSpacing.x * Mathf.Max(0, maxVisibleCards - 1);
                break;
            case BattlefieldZoneLayoutType.verticalColumn:
                depth += cardSpacing.y * Mathf.Max(0, maxVisibleCards - 1);
                break;
            case BattlefieldZoneLayoutType.grid:
                var columns = Mathf.CeilToInt(Mathf.Sqrt(maxVisibleCards));
                var rows = Mathf.CeilToInt(maxVisibleCards / (float)columns);
                width += cardSpacing.x * Mathf.Max(0, columns - 1);
                depth += cardSpacing.y * Mathf.Max(0, rows - 1);
                break;
            case BattlefieldZoneLayoutType.bounds:
                width = Mathf.Max(width, cardSpacing.x);
                depth = Mathf.Max(depth, cardSpacing.y);
                break;
        }

        return new Vector3(width, 0.08f, depth);
    }
}
}
