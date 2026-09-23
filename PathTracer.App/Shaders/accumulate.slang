struct Params
{
    uint Reset;
    float HistoryWeight;
    uint Pad0;
    uint Pad1;
}

Texture2D<float4> sampleImage : register(t0);
Texture2D<float4> historyImage : register(t1);
Texture2D<float4> guideImage : register(t2);
Texture2D<float4> motionImage : register(t3);
Texture2D<float4> guideHistoryImage : register(t4);
RWTexture2D<float4> historyOutImage : register(u5);
RWTexture2D<float4> guideHistoryOutImage : register(u6);
ConstantBuffer<Params> params : register(b7);

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

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 dispatchThreadId : SV_DispatchThreadID)
{
    uint2 frameSize;
    historyOutImage.GetDimensions(frameSize.x, frameSize.y);
    uint2 pixel = dispatchThreadId.xy;
    if (any(pixel >= frameSize))
        return;

    float4 sample = sampleImage.Load(int3(pixel, 0));
    float4 currentGuide = guideImage.Load(int3(pixel, 0)); // oct normal, view depth, surface ID
    float4 motion = motionImage.Load(int3(pixel, 0));      // previous pixel, previous depth, surface ID
    int2 previousPixel = int2(round(motion.xy));
    bool inBounds = all(previousPixel >= int2(0, 0)) && all(previousPixel < int2(frameSize));
    bool validHistory = params.Reset == 0 && inBounds;

    float3 result = sample.rgb;
    float variance = max(sample.a, 0.0);
    // Mirror-like primary surfaces get only a short history: primary-surface
    // reprojection cannot fully track visibility changes later in the path.
    float roughness = frac(motion.w) * 10.0;
    float historyWeight = roughness < 0.1 ? 0.3 : params.HistoryWeight;
    if (validHistory && historyWeight > 0.0)
    {
        float4 previousGuide = guideHistoryImage.Load(int3(previousPixel, 0));
        float4 previousHistory = historyImage.Load(int3(previousPixel, 0));
        bool sameObject = abs(motion.w - previousGuide.w) < 0.25;
        bool isSky = abs(floor(motion.w + 0.25) - 9.0) < 0.25;
        bool sameNormal = isSky || dot(decodeOctahedralNormal(currentGuide.xy),
            decodeOctahedralNormal(previousGuide.xy)) > 0.95;
        float depthTolerance = max(0.01, motion.z * 0.015);
        bool sameDepth = isSky || abs(motion.z - previousGuide.z) < depthTolerance;

        if (sameObject && sameNormal && sameDepth)
        {
            float currentLuminance = luminance(sample.rgb);
            float previousLuminance = luminance(previousHistory.rgb);
            float weight = historyWeight;
            result = lerp(sample.rgb, previousHistory.rgb, weight);
            // Exponentially update luminance variance for the wavelet filter.
            variance = weight * previousHistory.a + (1.0 - weight) * variance
                + weight * (1.0 - weight) * (previousLuminance - currentLuminance) * (previousLuminance - currentLuminance);
        }
    }

    historyOutImage[pixel] = float4(result, variance);
    guideHistoryOutImage[pixel] = currentGuide;
}
