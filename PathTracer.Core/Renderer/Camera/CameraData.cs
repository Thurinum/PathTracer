using System.Numerics;
using System.Runtime.InteropServices;

namespace PathTracerCore.Renderer.Camera;

[StructLayout(LayoutKind.Explicit, Size = 64)]
public record struct CameraData
{
    [FieldOffset(00)] public Vector3 Origin;
    [FieldOffset(12)] public float AspectRatio;
    [FieldOffset(16)] public Vector3 Up;
    [FieldOffset(28)] public float TanHalfFOV;
    [FieldOffset(32)] public Vector3 Right;
    [FieldOffset(48)] public Vector3 Forward;
}