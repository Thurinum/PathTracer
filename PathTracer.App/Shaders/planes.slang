struct Plane 
{
    float3 Center;
    float3 U;
    float3 V;
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
ConstantBuffer<Camera> camera : register(b1);
StructuredBuffer<Plane> planes : register(t2);
ConstantBuffer<Params> params : register(b3);

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 dispatchThreadId : SV_DispatchThreadID)
{
    uint2 frameSize;
    outputImage.GetDimensions(frameSize.x, frameSize.y);
    
    uint2 pixel = dispatchThreadId.xy;
    if (any(pixel >= frameSize))
        return;
        
    float2 uv  = (float2(pixel) + 0.5) / float2(frameSize);
    float ndcX = (2.0 * uv.x - 1.0) * camera.Aspect * camera.TanHalfFov;
    float ndcY = (1.0 - 2.0 * uv.y) * camera.TanHalfFov;

    float3 origin = camera.Origin;
    float3 dir = normalize(camera.Forward + ndcX * camera.Right + ndcY * camera.Up);

    float bestT = 1e30;
    float3 hitN = 0.0;
    bool hit = false;

    for (uint i = 0; i < params.Count; ++i)
    {
        Plane p = planes[i];
        float3 n = normalize(cross(p.U, p.V));
        float denom = dot(n, dir);
        if (abs(denom) < 1e-6) continue;                      // parallel
    
        float t = dot(n, p.Center - origin) / denom;
        if (t <= 1e-4 || t >= bestT) continue;                // behind or farther
    
        float3 w = origin + t * dir - p.Center;
        float a = dot(w, p.U) / dot(p.U, p.U);                // U ⟂ V, so this is exact
        float b = dot(w, p.V) / dot(p.V, p.V);
        if (max(abs(a), abs(b)) > 1.0) continue;              // outside the rectangle
    
        bestT = t; hitN = n; hit = true;
    }

    float3 color = float3(0.0);
    if (hit)
    {
        if (dot(hitN, dir) > 0.0) hitN = -hitN;      // two-sided: face the ray
        color = 0.5 * (hitN + 1.0);                  // visualize the normal
    }
    outputImage[pixel] = float4(color, 1.0);
}
