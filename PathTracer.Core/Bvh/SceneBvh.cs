namespace PathTracerCore.Bvh;

using PathTracerCore.Renderer.Primitives;

public sealed class SceneBvh(PrimitiveRegistry primitives)
{
    public BvhNode? Root { get; private set; }
    
    private readonly Dictionary<IPrimitiveBuffer, uint> _sourceRevisions = [];

    public bool IsDirty =>
        primitives.All.Any(buffer =>
            !_sourceRevisions.TryGetValue(buffer, out var revision)
            || revision != buffer.Revision);

    public void Rebuild()
    {
        List<BvhPrimitiveRef> references = [];

        foreach (IPrimitiveBuffer buffer in primitives.All)
        {
            buffer.AppendBvhPrimitives(references);
            _sourceRevisions[buffer] = buffer.Revision;
        }

        Root = references.Count == 0
            ? null
            : new BvhBuilder().Build(references);
    }
}