using PathTracerCore.Renderer.Graph;

namespace PathTracerCore.Renderer.Resources;

public interface IResourceProvider
{
    IEnumerable<IResourceSpec> Resources { get; }
    void Update(RenderContext ctx);
}