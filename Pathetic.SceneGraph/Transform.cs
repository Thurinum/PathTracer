using System.Numerics;

namespace Pathetic.SceneGraph;

public class Transform : Component
{
    public Vector3 Position;
    public Quaternion Rotation = Quaternion.Identity;
    public Vector3 Scale = Vector3.One;
}