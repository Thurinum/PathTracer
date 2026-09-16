using NeoVeldrid;

namespace PathTracerApp.Renderer.Pass;

public sealed class FrameContext(GraphicsDevice device, CommandList cmd, Texture target)
{
    public GraphicsDevice Device { get; } = device;
    public CommandList Cmd { get; } = cmd;
    public Texture Target { get; } = target;
}