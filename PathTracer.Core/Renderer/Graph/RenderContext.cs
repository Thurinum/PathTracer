using System.Numerics;
using NeoVeldrid;

namespace PathTracerCore.Renderer.Graph;

public sealed class RenderContext
{
    public required CommandList Cmd { get; init; }
    public required IResourceAccessor Resources { get; init; }
    public required uint Width { get; init; }
    public required uint Height { get; init; }
    public required uint FrameIndex { get; init; }
    public required float DeltaTime { get; init; }
    public required Vector2 Jitter { get; init; }
    public required uint SceneRevision { get; init; } 
}