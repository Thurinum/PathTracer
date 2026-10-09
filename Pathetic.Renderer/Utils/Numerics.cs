// ReSharper disable InconsistentNaming
namespace Pathetic.Renderer.Utils;

public readonly record struct uint2(uint x, uint y)
{
    public static implicit operator uint2((uint x, uint y) value) => new(value.x, value.y);
}

public readonly record struct uint3(uint x, uint y, uint z)
{
    public static implicit operator uint3((uint x, uint y, uint z) value) => new(value.x, value.y, value.z);
}