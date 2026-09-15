using NeoVeldrid;

namespace PathTracerApp.Renderer.Pass;

public class FrameContext
{
    public GraphicsDevice Device;
    public CommandList Cmd;
    public int Width;
    public int Height;
}