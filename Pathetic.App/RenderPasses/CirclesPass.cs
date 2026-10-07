using NeoVeldrid;
using Pathetic.App.Components;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Primitives;
using Pathetic.Renderer.Resources;

namespace Pathetic.App.RenderPasses;

public sealed class CirclesPass(PrimitiveRegistry primitives) : RenderPass
{
    private struct Params
    {
        public uint Count;
    }

    private readonly PrimitiveBuffer<CirclePrimitive> _circles = primitives.Get<CirclePrimitive>();
    private readonly IResourceSpec[] _outputs =
    [
        new TextureSpec("circles", PixelFormat.R32_G32_B32_A32_Float, TextureUsage.Sampled | TextureUsage.Storage, new AutoSize()),
        new UniformBufferSpec("circlesParams", 16),
    ];
    private readonly ResourceRef[] _inputs =
    [
        new("circlePrimitives", ResourceKind.StructuredBufferReadOnly),
    ];

    public override string ShaderModule => "Circles";
    public override IReadOnlyList<IResourceSpec> Outputs => _outputs;
    public override IReadOnlyList<ResourceRef> Inputs => _inputs;

    protected override void Upload(RenderContext ctx)
    {
        Params @params = new() { Count = (uint)_circles.Count };
        ctx.Cmd.UpdateBuffer(ctx.Resources.GetBuffer("circlesParams"), 0, @params);
    }
}