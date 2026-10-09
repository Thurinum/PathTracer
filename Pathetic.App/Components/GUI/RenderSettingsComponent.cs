using ImGuiNET;
using Pathetic.App.RenderPasses;
using Pathetic.Renderer.Graph;
using Pathetic.SceneGraph;

namespace Pathetic.App.Components.GUI;

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

        bool enabled = _pass.Accumulate;
        if (ImGui.Checkbox("Énerver Youssef", ref enabled))
            _pass.Accumulate = enabled;

        ImGui.End();
    }
}
