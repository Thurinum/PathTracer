struct Params { 
    uint Reset; 
}

RWTexture2D<float4> outputImage : register(u0);
Texture2D<float4> sampleImage : register(t1);
RWTexture2D<float4> accumImage : register(u2);
ConstantBuffer<Params> params : register(b3);

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 dispatchThreadId : SV_DispatchThreadID)
{
    uint2 frameSize;
    outputImage.GetDimensions(frameSize.x, frameSize.y);
    uint2 pixel = dispatchThreadId.xy;
    if (any(pixel >= frameSize)) return;

    float3 estimate = sampleImage.Load(int3(pixel, 0)).rgb;

    float4 accum = (params.Reset != 0) ? float4(0.0) : accumImage[pixel];
    accum += float4(estimate, 1.0);
    accumImage[pixel] = accum;
    outputImage[pixel] = float4(accum.rgb / accum.a, 1.0);
}