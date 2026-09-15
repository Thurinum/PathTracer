using Microsoft.Extensions.Options;
using NeoVeldrid;
using NeoVeldrid.Sdl2;
using NeoVeldrid.StartupUtilities;

namespace PathTracerApp.Renderer;

public sealed class RenderBackend : IDisposable
{
    private readonly Sdl2Window _window;
    private readonly GraphicsDevice _device;
    private readonly CommandList _cmd;
    private readonly ImGuiRenderer _imgui;
    private readonly RenderPass _pass; // TODO: for now only one pass
    public bool Shown => _window.Exists;

    public RenderBackend(IOptions<RenderBackendOptions> options, RenderPass pass)
    {
        var config = options.Value;
        
        WindowCreateInfo windowDescription = new()
        {
            X = config.X,
            Y = config.Y,
            WindowWidth = (int)config.WindowWidth,
            WindowHeight = (int)config.WindowHeight,
            WindowTitle = config.AppName
        };

        GraphicsDeviceOptions deviceOptions = new()
        {
            PreferStandardClipSpaceYDirection = true,
            PreferDepthRangeZeroToOne = true,
        };

        NeoVeldridStartup.CreateWindowAndGraphicsDevice
        (
            windowDescription,
            deviceOptions,
            GraphicsBackend.Vulkan,
            out _window,
            out _device
        );

        _window.Resized += OnWindowResized;
        _cmd = _device.ResourceFactory.CreateCommandList();
        _imgui = new ImGuiRenderer(
            _device,
            _device.MainSwapchain.Framebuffer.OutputDescription,
            _window.Width,
            _window.Height);
        
        _pass = pass;
    }
    
    public InputSnapshot GetInput()
    {
        return _window.PumpEvents();
    }

    public void BeginFrame()
    {
        _cmd.Begin();
        _cmd.SetFramebuffer(_device.SwapchainFramebuffer);
    }

    public void Render()
    {
        _pass.Render();
    }

    public void EndFrame()
    {
        _cmd.End();
        _device.SubmitCommands(_cmd);
        _device.SwapBuffers(_device.MainSwapchain);
    }
    
    public void BeginGUI(float deltaTime, InputSnapshot input)
    {
        _imgui.Update(deltaTime, input);
    }

    public void EndGUI()
    {
        _imgui.Render(_device, _cmd);
    }

    private void OnWindowResized()
    {
        _device.ResizeMainWindow((uint)_window.Width, (uint)_window.Height);
        _imgui.WindowResized(_window.Width, _window.Height);
    }

    public void Dispose()
    {
        _device.WaitForIdle();
        _imgui.Dispose();
        _cmd.Dispose();
        _device.Dispose();
    }
}
