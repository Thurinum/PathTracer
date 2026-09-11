using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeoVeldrid;
using NeoVeldrid.Sdl2;
using NeoVeldrid.StartupUtilities;

namespace PathTracerApp;

public class App : IDisposable
{
    private Sdl2Window _window = null!;
    private GraphicsDevice _device = null!;
    private CommandList _commandList = null!;
    private ImGuiRenderer _imGuiRenderer = null!;

    private InputSnapshot _inputSnapshot = null!;
    private long _lastTimestamp;
    private float _deltaTime;

    private readonly AppOptions _options;
    private readonly ILogger<App> _logger;

    public event Action? DrawGui;

    public App(IOptions<AppOptions> options, ILogger<App> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public void Run(Action<FrameContext> appLoop)
    {
        ArgumentNullException.ThrowIfNull(appLoop);

        CreateWindowAndDevice();

        _commandList = _device.ResourceFactory.CreateCommandList();
        _imGuiRenderer = new ImGuiRenderer(
            _device,
            _device.MainSwapchain.Framebuffer.OutputDescription,
            _window.Width,
            _window.Height);

        _window.Resized += OnWindowResized;
        _lastTimestamp = Stopwatch.GetTimestamp();

        while (_window.Exists)
        {
            _inputSnapshot = _window.PumpEvents();

            if (!_window.Exists)
            {
                break;
            }

            _deltaTime = (float)Stopwatch.GetElapsedTime(_lastTimestamp).TotalSeconds;
            _lastTimestamp = Stopwatch.GetTimestamp();

            FrameContext frame = new(
                _device,
                _window,
                _commandList,
                _device.SwapchainFramebuffer);

            _commandList.Begin();
            _commandList.SetFramebuffer(_device.SwapchainFramebuffer);

            appLoop(frame);
            OnUpdateGUI();

            _commandList.End();

            _device.SubmitCommands(_commandList);
            _device.SwapBuffers(_device.MainSwapchain);
        }
    }

    public void OnUpdateGUI()
    {
        _imGuiRenderer.Update(_deltaTime, _inputSnapshot);
        DrawGui?.Invoke();
        _imGuiRenderer.Render(_device, _commandList);
    }

    private void CreateWindowAndDevice()
    {
        WindowCreateInfo windowDescription = new()
        {
            X = 100,
            Y = 100,
            WindowWidth = (int)_options.WindowWidth,
            WindowHeight = (int)_options.WindowHeight,
            WindowTitle = _options.AppName
        };

        NeoVeldridStartup.CreateWindowAndGraphicsDevice(
            windowDescription,
            new GraphicsDeviceOptions
            {
                PreferStandardClipSpaceYDirection = true,
                PreferDepthRangeZeroToOne = true,
            },
            out Sdl2Window window,
            out GraphicsDevice device);

        _window = window;
        _device = device;
    }

    private void OnWindowResized()
    {
        _logger.LogInformation("Window resized to {Width}x{Height}", _window.Width, _window.Height);

        _device.ResizeMainWindow((uint)_window.Width, (uint)_window.Height);
        _imGuiRenderer.WindowResized(_window.Width, _window.Height);
    }

    public void Dispose()
    {
        _device.WaitForIdle();

        _imGuiRenderer.Dispose();
        _commandList.Dispose();
        _device.Dispose();
    }
}
