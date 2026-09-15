struct Circle 
{
    float2 Position01;
    float Radius;
}

RWTexture2D<float4> outputImage : register(u0);
StructuredBuffer<Circle> circles : register(t0);

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 dispatchThreadId : SV_DispatchThreadID)
{
    uint2 frameSize;
    outputImage.GetDimensions(frameSize.x, frameSize.y);
    
    uint2 pixel = dispatchThreadId.xy;
    float2 uv = pixel / frameSize;
    float3 color = float3(uv.x, uv.y, 0.5);
    
    uint circleCount, stride;
    circles.GetDimensions(circleCount, stride);
    for (uint i = 0; i < circleCount; ++i)
    {
        Circle circle = circles[i];
        float2 center = circle.Position01 * frameSize;
        float2 d = pixel - center;
        if (dot(d, d) <= circle.Radius * circle.Radius)
        {
            outputImage[pixel] = float4(color, 1.0);    
        }
        else
        {
            outputImage[pixel] = float4(0.0, 0.0, 0.0, 1.0);
        }    
    }
}
