using ImGuiNET;
using PathTracerApp.RenderPasses;
using PathTracerCore.Renderer.Graph;
using PathTracerCore.SceneGraph;

namespace PathTracerApp.Components.GUI;

public class RenderSettingsComponent(RenderGraph graph) : Component
{
    private TemporalAccumulationPass? _pass;

    public override void Awake()
    {
        _pass = graph.GetPass<TemporalAccumulationPass>();
    }

    public override void OnGUI()
    {
        if (_pass is null)
            return;

        ImGui.Begin("Renderer");

        bool enabled = _pass.Enabled;
        if (ImGui.Checkbox("Énerver Youssef", ref enabled))
            _pass.Enabled = enabled;

        ImGui.End();
    }
}
