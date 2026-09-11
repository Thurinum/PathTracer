RWTexture2D<float4> outputImage : register(u0);
cbuffer Params : register(b1)
{
    float Radius;
}

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 dispatchThreadId : SV_DispatchThreadID)
{
    uint width, height;
    outputImage.GetDimensions(width, height);
    
    uint2 pixel = dispatchThreadId.xy;
    float2 center = float2(width / 2, height / 2);

    float2 uv = float2(pixel) / float2(width, height);
    float3 color = float3(uv.x, uv.y, 0.5);
    float2 d = pixel - center;
    if (dot(d, d) <= Radius * Radius)
    {
        outputImage[pixel] = float4(color, 1.0);    
    }
    else
    {
        outputImage[pixel] = float4(0.0, 0.0, 0.0, 1.0);
    }    
}
