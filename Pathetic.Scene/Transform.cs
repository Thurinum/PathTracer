using System.Numerics;

namespace Pathetic.Scene;

public class Transform : Component
{
    public Vector3 Position;
    public Quaternion Rotation = Quaternion.Identity;
    public Vector3 Scale = Vector3.One;
}