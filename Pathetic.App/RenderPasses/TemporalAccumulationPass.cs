using NeoVeldrid;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;

namespace Pathetic.App.RenderPasses;

public sealed class TemporalAccumulationPass : RenderPass
{
    private struct Params
    {
        public uint Reset;
    }

    private readonly IResourceSpec[] _outputs =
    [
        new TextureSpec("color", PixelFormat.B8_G8_R8_A8_UNorm,
            TextureUsage.Sampled | TextureUsage.Storage, new AutoSize()),
        new TextureSpec("accumImage", PixelFormat.R32_G32_B32_A32_Float,
            TextureUsage.Sampled | TextureUsage.Storage, new AutoSize()),
        new UniformBufferSpec("accumParams", 16),
    ];
    private readonly ResourceRef[] _inputs =
    [
        new("pathTracerColor", ResourceKind.TextureReadOnly),
    ];
    private uint _lastRevision = uint.MaxValue;

    public bool Accumulate { get; set; } = true;

    public override string ShaderModule => "TemporalAccumulation";
    public override IReadOnlyList<IResourceSpec> Outputs => _outputs;
    public override IReadOnlyList<ResourceRef> Inputs => _inputs;

    protected override void Upload(RenderContext ctx)
    {
        bool reset = !Accumulate || ctx.SceneRevision != _lastRevision;
        _lastRevision = ctx.SceneRevision;

        Params @params = new() { Reset = reset ? 1u : 0u };
        ctx.Cmd.UpdateBuffer(ctx.Resources.GetBuffer("accumParams"), 0, @params);
    }
}
