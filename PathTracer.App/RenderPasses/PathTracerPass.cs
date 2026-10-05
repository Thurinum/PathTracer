using Microsoft.Extensions.Options;
using NeoVeldrid;
using PathTracerApp.Components;
using PathTracerCore;
using PathTracerCore.Renderer.Graph;
using PathTracerCore.Renderer.Primitives;
using PathTracerCore.Renderer.Resources;

namespace PathTracerApp.RenderPasses;

public sealed class PathTracerPass(IOptions<EngineOptions> options, PrimitiveRegistry primitives) : RenderPass
{
    private struct Params
    {
        public uint PlaneCount;
        public uint SphereCount;
        public uint Seed;
        public uint SamplesPerPixel;
        public uint MaxBounces;
    }

    private readonly PrimitiveBuffer<PlanePrimitive> _planes = primitives.Get<PlanePrimitive>();
    private readonly PrimitiveBuffer<SpherePrimitive> _spheres = primitives.Get<SpherePrimitive>();
    private readonly IResourceSpec[] _outputs =
    [
        new TextureSpec("pathTracerColor", PixelFormat.R32_G32_B32_A32_Float, TextureUsage.Sampled | TextureUsage.Storage, new AutoSize()),
        new UniformBufferSpec("pathTracerParams", 32),
    ];
    private readonly ResourceRef[] _inputs =
    [
        new("circles", ResourceKind.TextureReadOnly),
        new("camera", ResourceKind.UniformBuffer),
        new("planes", ResourceKind.StructuredBufferReadOnly),
        new("spheres", ResourceKind.StructuredBufferReadOnly),
    ];
    private readonly SamplerSpec[] _samplers =
    [
        new("circlesSampler", SamplerDescription.Linear),
    ];

    public override string ShaderModule => "PathTracer";
    public override IReadOnlyList<IResourceSpec> Outputs => _outputs;
    public override IReadOnlyList<ResourceRef> Inputs => _inputs;
    public override IReadOnlyList<SamplerSpec> Samplers => _samplers;

    protected override void Upload(RenderContext ctx)
    {
        Params @params = new()
        {
            PlaneCount = (uint)_planes.Count,
            SphereCount = (uint)_spheres.Count,
            Seed = ctx.FrameIndex,
            SamplesPerPixel = options.Value.SamplesPerPixel,
            MaxBounces = options.Value.MaxBounces,
        };
        ctx.Cmd.UpdateBuffer(ctx.Resources.GetBuffer("pathTracerParams"), 0, @params);
    }
}
