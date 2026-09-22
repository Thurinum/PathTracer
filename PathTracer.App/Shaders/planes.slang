#include "blinn_phong.slang" // module doesnt work for some rfuckign reason

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
Texture2D<float4> circles : register(t1);
SamplerState circlesSampler : register(s2);
ConstantBuffer<Camera> camera : register(b3);
StructuredBuffer<Plane> planes : register(t4);
ConstantBuffer<Params> params : register(b5);

static const float3 SunDir = float3(0.5081, 0.5679, 0.6476);

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
    float2 coords = 0.0;
    
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
        coords = float2(a, b) * 0.5 + 0.5;
    }

    float3 color = float3(0.0);
    if (hit)
    {
        float3 lightCol = float3(0.56f, 0.7f, 0.1f);

        float3 Ng = hitN;                               // geometric normal
        float3 Nv = dot(Ng, dir) > 0.0 ? -Ng : Ng;      // view-facing, for specular
        float3 L  = SunDir;
        float3 V  = -dir;

        float3 albedo = circles.SampleLevel(circlesSampler, coords, 0.0).xyz;
        color = albedo * BlinnPhong(Ng, Nv, L, V, 0.8, 64.0, 0.2);
       
    }

    outputImage[pixel] = float4(color, 1.0);
}
