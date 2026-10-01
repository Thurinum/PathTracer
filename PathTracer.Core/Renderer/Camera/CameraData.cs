using System.Numerics;
using System.Runtime.InteropServices;

namespace PathTracerCore.Renderer.Camera;

[StructLayout(LayoutKind.Sequential, Size = 64)]
public record struct CameraData
{
    public Vector3 Origin;
    public float AspectRatio;
    
    public Vector3 Up;
    public float TanHalfFOV;
    
    public Vector3 Right;
    public float FocusDistance;
    
    public Vector3 Forward;
    public float ApertureRadius;
}