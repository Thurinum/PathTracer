using System.Numerics;
using PathTracerSceneGraph;

namespace PathTracerApp.Renderer;

public class Transform : Component
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;
}