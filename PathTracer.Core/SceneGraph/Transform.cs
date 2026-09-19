using System.Numerics;

namespace PathTracerCore.SceneGraph;

public class Transform : Component
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;
}