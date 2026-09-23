module PathTracer;

import Primitives;
import BlinnPhong;

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
    uint PlaneCount;
    uint SphereCount;
}

RWTexture2D<float4> outputImage : register(u0);
Texture2D<float4> circles : register(t1);
SamplerState circlesSampler : register(s2);
ConstantBuffer<Camera> camera : register(b3);
StructuredBuffer<Plane> planes : register(t4);
StructuredBuffer<Sphere> spheres : register(t5);
ConstantBuffer<Params> params : register(b6);

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
    HitResult bestHit = HitResult.Miss();
    
    for (uint i = 0; i < params.PlaneCount; ++i)
    {
        HitResult h = IntersectPlane(origin, dir, planes[i], 1e-4, bestT);
        if (h.IsHit()) { bestT = h.T; bestHit = h; }
    }
    for (uint i = 0; i < params.SphereCount; ++i)
    {
        HitResult h = IntersectSphere(origin, dir, spheres[i].Center, spheres[i].Radius, 1e-4, bestT);
        if (h.IsHit()) { bestT = h.T; bestHit = h; }
    }

    float3 color = float3(0.0);
    if (bestHit.IsHit())
    {
        float3 lightCol = float3(0.56f, 0.7f, 0.1f);

        float3 Ng = bestHit.Normal; 
        float3 Nv = dot(Ng, dir) > 0.0 ? -Ng : Ng; 
        float3 L  = SunDir;
        float3 V  = -dir;

        float3 albedo = circles.SampleLevel(circlesSampler, bestHit.UV, 0.0).xyz;
        color = albedo * BlinnPhong(Ng, Nv, L, V, 0.8, 64.0, 0.2);
    }
    else 
    {
        color = 0;
    }

    outputImage[pixel] = float4(color, 1.0);
}
