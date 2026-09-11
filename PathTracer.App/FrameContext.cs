using NeoVeldrid;
using NeoVeldrid.Sdl2;

namespace PathTracerApp;

public sealed class FrameContext
{
    internal FrameContext(
        GraphicsDevice device,
        Sdl2Window window,
        CommandList commandList,
        Framebuffer framebuffer)
    {
        Device = device;
        Window = window;
        CommandList = commandList;
        Framebuffer = framebuffer;
        SwapchainTarget = framebuffer.ColorTargets[0].Target;
    }

    public GraphicsDevice Device { get; }

    public Sdl2Window Window { get; }

    public CommandList CommandList { get; }

    public Framebuffer Framebuffer { get; }

    public Texture SwapchainTarget { get; }

    public uint Width => SwapchainTarget.Width;

    public uint Height => SwapchainTarget.Height;
}
