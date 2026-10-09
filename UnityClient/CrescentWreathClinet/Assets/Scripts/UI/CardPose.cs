using UnityEngine;

namespace CrescentWreath.Client.UI
{
public readonly struct CardPose
{
    public readonly Vector3 Position;
    public readonly Quaternion Rotation;
    public readonly Vector3 Scale;

    public CardPose(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        Position = position;
        Rotation = rotation;
        Scale = scale;
    }
}
}
