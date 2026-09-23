struct Params
{
    uint StepSize;
    uint FinalPass;
    uint Pad0;
    uint Pad1;
}

Texture2D<float4> sourceImage : register(t0);
Texture2D<float4> guideImage : register(t1);
RWTexture2D<float4> destinationImage : register(u2);
ConstantBuffer<Params> params : register(b3);

float3 decodeOctahedralNormal(float2 encoded)
{
    float2 f = encoded * 2.0 - 1.0;
    float3 normal = float3(f.x, f.y, 1.0 - abs(f.x) - abs(f.y));
    if (normal.z < 0.0)
        normal.xy = (1.0 - abs(normal.yx)) * sign(normal.xy);
    return normalize(normal);
}

float luminance(float3 color)
{
    return dot(color, float3(0.2126, 0.7152, 0.0722));
}

float kernelWeight(int offset)
{
    int distance = abs(offset);
    return distance == 0 ? 6.0 : (distance == 1 ? 4.0 : 1.0);
}

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 dispatchThreadId : SV_DispatchThreadID)
{
    uint2 frameSize;
    destinationImage.GetDimensions(frameSize.x, frameSize.y);
    uint2 pixel = dispatchThreadId.xy;
    if (any(pixel >= frameSize))
        return;

    float4 centerGuide = guideImage.Load(int3(pixel, 0)); // oct normal, view depth, surface ID
    float4 center = sourceImage.Load(int3(pixel, 0));
    float surfaceId = floor(centerGuide.w + 0.25);
    float roughness = frac(centerGuide.w) * 10.0;
    // Preserve sharp reflections on glossy surfaces; temporal reuse stays disabled there.
    if (roughness < 0.1)
    {
        float3 preserved = center.rgb;
        if (params.FinalPass != 0)
        {
            float3 mapped = max(preserved, 0.0) / (1.0 + max(preserved, 0.0));
            preserved = pow(mapped, 1.0 / 2.2);
        }
        destinationImage[pixel] = float4(preserved, center.a);
        return;
    }

    float3 centerNormal = decodeOctahedralNormal(centerGuide.xy);
    float centerLuminance = luminance(center.rgb);
    float3 sum = float3(0.0);
    float weightSum = 0.0;
    int stepSize = max(int(params.StepSize), 1);

    [unroll]
    for (int y = -2; y <= 2; y++)
    {
        [unroll]
        for (int x = -2; x <= 2; x++)
        {
            int2 q = int2(pixel) + int2(x, y) * stepSize;
            if (any(q < int2(0, 0)) || any(q >= int2(frameSize)))
                continue;

            float4 guide = guideImage.Load(int3(q, 0));
            if (abs(centerGuide.w - guide.w) > 0.25)
                continue;

            float normalWeight = 1.0;
            float depthWeight = 1.0;
            bool isSky = abs(surfaceId - 9.0) < 0.25;
            if (!isSky)
            {
                float normalAgreement = max(dot(centerNormal, decodeOctahedralNormal(guide.xy)), 0.0);
                normalWeight = pow(normalAgreement, 32.0);
                float depthSigma = max(0.01, centerGuide.z * 0.02 * float(stepSize));
                depthWeight = exp(-abs(centerGuide.z - guide.z) / depthSigma);
            }

            float4 neighbor = sourceImage.Load(int3(q, 0));
            float colorSigma = max(0.01, 2.5 * sqrt(max(center.a + neighbor.a, 0.0)));
            float colorWeight = exp(-abs(centerLuminance - luminance(neighbor.rgb)) / colorSigma);
            float waveletWeight = kernelWeight(x) * kernelWeight(y);
            float weight = normalWeight * depthWeight * colorWeight * waveletWeight;
            sum += neighbor.rgb * weight;
            weightSum += weight;
        }
    }

    float3 result = sum / max(weightSum, 1e-6);
    if (params.FinalPass != 0)
    {
        float3 mapped = max(result, 0.0) / (1.0 + max(result, 0.0));
        result = pow(mapped, 1.0 / 2.2);
    }
    destinationImage[pixel] = float4(result, center.a);
}
