using System.Runtime.CompilerServices;
using Pathetic.Renderer;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;

namespace Pathetic.App.Components.Camera;

public class CameraProvider(SceneRevision revision) : IResourceProvider
{
    private CameraData _data;
    public uint Version { get; private set; }

    public void Set(in CameraData data)
    {
        if (data.Equals(_data))
            return;

        _data = data;
        Version++;
        revision.Invalidate();
    }

    public IEnumerable<IResourceSpec> Resources { get; } =
    [
        new UniformBufferSpec("camera", (uint)Unsafe.SizeOf<CameraData>())    
    ];
    
    public void Update(RenderContext ctx)
    {
        ctx.Cmd.UpdateBuffer(ctx.Resources.GetBuffer("camera"), 0, _data);
    }
}