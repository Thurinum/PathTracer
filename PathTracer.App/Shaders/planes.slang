struct Plane 
{
    float3 Origin;
    float2 Extents;
   
}

struct Camera
{
    float3 Origin;    
    float Aspect;
    float3 Up;        
    float TanHalfFov;
    float3 Right;     
    float Pad0;
    float3 Forward;   
    float Pad1;
};

struct Params
{
    uint Count;
}

RWTexture2D<float4> outputImage : register(u0);
StructuredBuffer<Plane> planes : register(t1);
ConstantBuffer<Params> params : register(b2);

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 dispatchThreadId : SV_DispatchThreadID)
{
    uint2 frameSize;
    outputImage.GetDimensions(frameSize.x, frameSize.y);
    
    uint2 pixel = dispatchThreadId.xy;
    if (any(pixel >= frameSize))
        return;
        
    float2 uv = float2(pixel) / float2(frameSize);
    
    for (uint i = 0; i < params.Count; ++i)
    {
        Plane plane = planes[i];
        
        
    }
    
    outputImage[pixel] = float4(, 1.0);
}
