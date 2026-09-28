// PCG hash-based random number generator.
// Source: "Hash Functions for GPU Rendering" (Jarzynski & Olano, JCGT 2020)
// https://jcgt.org/published/0009/03/02/
// https://www.reedbeta.com/blog/hash-functions-for-gpu-rendering/

module Random;

public uint PcgHash(uint v)
{
    uint state = v * 747796405u + 2891336453u;
    uint word = ((state >> ((state >> 28u) + 4u)) ^ state) * 277803737u;
    return (word >> 22u) ^ word;
}

public struct Rng
{
    public uint State;

    public __init(uint2 pixel, uint frame)
    {
        State = PcgHash(pixel.x + PcgHash(pixel.y + PcgHash(frame)));
    }

    [mutating] public float Next()
    {
        State = PcgHash(State);
        return float(State >> 8) * (1.0 / 16777216.0);
    }
}
